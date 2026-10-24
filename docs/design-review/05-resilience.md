# 5. Upstream calls have no timeout, cancellation or resilience — Critical

## Problem

- **Flurl's default timeout is 100 seconds.** A slow upstream ties up a request thread and a connection for that long. Under load that's thread-pool starvation and a cascading failure.
- **No `CancellationToken`.** If the caller disconnects, we keep waiting on WooliesX for nothing.
- **No retries for transient faults** (a 503 or a dropped connection), and **no circuit breaker**: when WooliesX is down we keep hammering it, and every caller waits the full timeout.

## Fix

**Standard resilience pipeline** (`Microsoft.Extensions.Http.Resilience`, built on Polly v8) on every typed client. See [WooliesXServiceCollectionExtensions.cs](../../ProductCatalog.Api/Infrastructure/WooliesX/WooliesXServiceCollectionExtensions.cs):

```csharp
services.AddHttpClient<TClient>(...)
    .AddHttpMessageHandler<WooliesXTokenHandler>()
    .RemoveAllLoggers().AddLogger<RedactingHttpClientLogger>()
    .AddStandardResilienceHandler(configuration.GetSection("WooliesX:Resilience"));
```

The pipeline, outer to inner:

| Strategy | Setting ([appsettings.json](../../ProductCatalog.Api/appsettings.json)) | Effect |
|---|---|---|
| Rate limiter | default | Caps concurrent outbound requests. |
| **Total timeout** | 15 s | Upper bound for the whole call, including retries. |
| **Retry** | 2 retries, exponential back-off + jitter | Only on transient errors: 5xx, 408, 429, `HttpRequestException`, timeouts. |
| **Circuit breaker** | default (opens at a 10% failure ratio over 30 s) | Fails fast while the upstream is unhealthy, and gives it room to recover. |
| **Attempt timeout** | 5 s | Bound for a single try. |

- **Cancellation flows end to end.** The action takes a `CancellationToken`, which MVC binds to `HttpContext.RequestAborted`, and it's passed to the handler, the clients and `HttpClient`. A client disconnect cancels the upstream call.
- **Each typed client has its own pipeline**, so a broken shopper-history service opens only *its* circuit, and `Low`/`High` sorts keep working.
- Failures map to meaningful status codes in [GlobalExceptionHandler.cs](../../ProductCatalog.Api/ErrorHandling/GlobalExceptionHandler.cs): upstream error → **502**, circuit open → **503**, timeout → **504**, client disconnect → **499** with no body.

### A subtle ordering bug avoided

Handler order matters. The token handler is registered **before** (outside) the resilience handler. If it sat inside, every retry would re-run it and append `&token=…` again to the same request. The logger is innermost, so it records each attempt. Both orders are documented in code comments.

## Trade-offs

- **Retrying a POST** (`/trolleyTotal`). The standard handler retries every method. Here that's safe because the trolley calculator is a pure calculation, so it's idempotent. For a non-idempotent POST (placing an order) I'd disable retries for unsafe methods, or use idempotency keys.
- **Retries multiply load** on a struggling upstream. That's why there are only 2 retries with jittered back-off, plus the circuit breaker and a total timeout.
- The numbers are starting points. In production I'd tune them from the upstream's p99 latency and its SLA.

## What to say in the interview

**30-second answer:**
> "Every upstream call could hang for 100 seconds with no cancellation, retry or circuit breaker. That's how one slow dependency takes down a service. I moved to `IHttpClientFactory` typed clients with the standard resilience handler: per-attempt and total timeouts, jittered retries for transient errors only, and a circuit breaker per dependency. The request's cancellation token flows all the way down. Failures surface as 502, 503 or 504 so callers and dashboards can tell 'we're broken' from 'a dependency is broken'."

**Likely follow-ups:**
- *"What's a retry storm, and how do you prevent it?"* Many clients retry at once and multiply the load on a struggling service. Prevent it with exponential back-off, jitter, low retry counts, retry budgets, and circuit breakers.
- *"Why 504 and not 500?"* 500 means *our* bug. 502/503/504 tell the caller, and the alerting, that a dependency is at fault. That changes who gets paged.
- *"What about fallbacks?"* For products, serving **stale cache** on failure is a good fallback (*stale-if-error*). It's the next step on top of [11-caching.md](11-caching.md).
- *"How do you test this?"* Unit-test the mapping in the exception handler. For the pipeline itself, use fault injection (Polly chaos strategies, or a stub handler that returns 503s) in an integration test.
