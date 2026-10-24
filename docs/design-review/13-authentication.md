# 13. New: authentication layer

## Problem

Every endpoint was anonymous, including `/user`, which returns the upstream credential ([01](01-hardcoded-token.md)), and `/products/sort` and `/trolleyTotal`, each of which spends upstream quota. Without caller identity there's also nothing to rate-limit *per client*.

## Fix

An **API-key authentication scheme** plugged into ASP.NET Core's normal authentication/authorization pipeline. See [ApiKeyAuthenticationHandler.cs](../../ProductCatalog.Api/Security/ApiKeyAuthenticationHandler.cs) and [Startup.cs](../../ProductCatalog.Api/Startup.cs):

- **The caller sends `X-Api-Key: <key>`.** The handler looks up the matching client and issues a `ClaimsPrincipal` (`NameIdentifier` and `Name` set to the client ID).
- **Keys are compared in constant time.** Both values are hashed with SHA-256 and compared with `CryptographicOperations.FixedTimeEquals`, so response timing doesn't reveal how much of a key matched.
- **Missing header → `NoResult`, wrong key → `Fail`.** Both end as 401 ProblemDetails. The response never says which.
- **Secure by default.** `AuthorizationOptions.FallbackPolicy = RequireAuthenticatedUser()`, so any new endpoint is protected unless it explicitly opts out. Only `/` and `/health` use `.AllowAnonymous()` (health probes must work without credentials). `/user` also carries an explicit `[Authorize]` for readability.
- **Keys live outside source control**, in `Authentication:ApiKey:Clients` from user-secrets, environment variables or Key Vault:
  ```bash
  dotnet user-secrets set "Authentication:ApiKey:Clients:0:ClientId" "local-dev"   --project ProductCatalog.Api
  dotnet user-secrets set "Authentication:ApiKey:Clients:0:Key"      "<random key>" --project ProductCatalog.Api
  ```
- **The client identity feeds the rate limiter** ([12](12-rate-limiting.md)).
- Tests: 401 for missing and wrong keys, 200 with a valid key, anonymous root and health. See [ProductsEndpointTests.cs](../../ProductCatalog.Tests/Integration/ProductsEndpointTests.cs) and [UserEndpointTests.cs](../../ProductCatalog.Tests/Integration/UserEndpointTests.cs).

## Why API keys, and when I'd choose something else

API keys are a reasonable fit for **service-to-service** callers of a catalog API: simple to issue, simple to send, and fully built here in ~60 lines with no external identity provider.

They have real limitations. They're long-lived bearer secrets, carry no user identity, have no standard expiry, and revocation means editing config. For **user-facing** or broader scenarios I'd use **OAuth 2.0 / OpenID Connect JWT bearer tokens** (Entra ID, Auth0, Cognito). That's `AddJwtBearer` with authority and audience, which gives short-lived tokens, scopes for authorization (`products:read`), and central revocation. Because everything goes through the standard `[Authorize]`/policy pipeline, switching schemes doesn't touch the controllers. Supporting both during a migration is just two schemes plus a policy.

Hardening for production:

- Store **hashes** of keys, not the keys themselves, and support **two active keys per client** for zero-downtime rotation.
- Add **scopes or roles** per client and use policy-based authorization (`[Authorize(Policy = "TrolleyWrite")]`), not just "is authenticated".
- **Audit logging** of client ID per request (it's already on the principal), and alerts on bursts of 401s.
- Require HTTPS everywhere (`UseHttpsRedirection` plus HSTS at the edge). An API key over plain HTTP is a leaked key.

## What to say in the interview

**30-second answer:**
> "Nothing was authenticated, including the endpoint that returns a credential. I added an API-key scheme as a proper ASP.NET Core authentication handler, with constant-time comparison, keys from the secret store, and a fallback policy so every endpoint is protected by default and only health and root opt out. It also gives the rate limiter a real client identity. For user-facing clients I'd use OAuth/OIDC JWT bearer instead, and because it's all standard `[Authorize]` plumbing, swapping the scheme doesn't touch the controllers."

**Likely follow-ups:**
- *"Authentication vs. authorization?"* Authentication answers *who are you* (the handler produces a principal). Authorization answers *what may you do* (policies evaluate the principal). Here, authorization is just "must be authenticated". Scopes would be the next step.
- *"Why a fallback policy rather than `[Authorize]` on each controller?"* Secure by default. Forgetting an attribute shouldn't create a public endpoint, and opting out is explicit and easy to review.
- *"Why constant-time comparison?"* A normal string comparison returns early at the first mismatched character, so response time leaks how many leading characters were right. That enables a character-by-character brute force.
- *"API key in a header vs. the query string?"* Header. Query strings end up in logs, browser history and proxies, which is exactly the upstream-token problem from [06](06-error-handling-and-logging.md).
