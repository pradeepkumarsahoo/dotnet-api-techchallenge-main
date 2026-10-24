# 12. New: rate limiting

## Problem

There were no limits on inbound traffic. One misbehaving client could saturate the service and, because every request fans out to WooliesX, push the upstream into throttling or an outage. That would hit *every* client, and could exhaust our upstream quota.

## Fix

ASP.NET Core's built-in rate limiter (`Microsoft.AspNetCore.RateLimiting`, .NET 7+). See [RateLimitingServiceCollectionExtensions.cs](../../ProductCatalog.Api/RateLimiting/RateLimitingServiceCollectionExtensions.cs):

- **A global fixed-window limiter, partitioned per caller.** The key is `client:{clientId}` for authenticated callers and `ip:{address}` for anonymous ones. Each client gets its own budget, so one noisy client can't starve the others.
- **Defaults: 60 requests per minute**, configurable under `RateLimiting` in [appsettings.json](../../ProductCatalog.Api/appsettings.json) and validated at startup.
- **Rejected requests get `429 Too Many Requests` with a `Retry-After` header**, so well-behaved clients know when to come back. `QueueLimit = 0` means rejections are immediate, not queued.
- **Middleware order:** `UseAuthentication()` → `UseRateLimiter()` → `UseAuthorization()` in [Startup.cs](../../ProductCatalog.Api/Startup.cs). The limiter runs after authentication so it can partition by client ID, and before authorization and the endpoints so rejected traffic costs almost nothing.
- Test: [RateLimitingTests.cs](../../ProductCatalog.Tests/Integration/RateLimitingTests.cs) checks that the third request with a limit of 2 returns 429 with `Retry-After`.

## Trade-offs and next steps

- **Fixed window vs. alternatives.** Fixed windows allow a burst of up to 2× the limit around a window boundary. **Sliding window** smooths that. **Token bucket** allows controlled bursts with a steady refill. **Concurrency limiter** caps in-flight requests, which is better for protecting expensive endpoints. Swapping is one line.
- **Per-instance counters.** With N instances the effective limit is N × 60. For a strict global limit, enforce it at the gateway (Azure API Management, AWS API Gateway, Envoy) or use a Redis-backed limiter. In practice I'd put **coarse limits at the edge and fine-grained per-client limits in the app**.
- **IP partitioning behind a proxy.** Without `UseForwardedHeaders` (with known proxies), every anonymous caller shares the proxy's IP. Anonymous traffic is limited to `/` and `/health` today, but it matters if that changes.
- **Tiered limits.** Partners could get different limits by adding a `Tier` to `ApiKeyClient` and reading it in the partition factory.
- **Outbound limits too.** The standard resilience handler ([05](05-resilience.md)) caps concurrent *outbound* calls, which protects the upstream from us.

## What to say in the interview

**30-second answer:**
> "I added ASP.NET Core's built-in rate limiter as a global, per-client fixed window, partitioned by API-key client ID and falling back to IP. It runs after authentication so the partition is the real caller, returns 429 with Retry-After, and is configurable and tested. Because instance-local counters don't give a global limit, in production I'd also put a coarse limit at the API gateway and keep per-client fairness in the app."

**Likely follow-ups:**
- *"Fixed window vs. sliding window vs. token bucket?"* Fixed is simplest but bursty at window boundaries. Sliding tracks segments to smooth that. Token bucket allows bursts up to the bucket size with a steady refill, which is often the best match for API quotas.
- *"Where should rate limiting live?"* At both layers: the edge stops volumetric abuse cheaply, the app enforces business-aware per-client quotas.
- *"Why 429 and not 503?"* 429 means *you* are sending too much. 503 means *we* are unavailable. Different meanings, different client behaviour.
