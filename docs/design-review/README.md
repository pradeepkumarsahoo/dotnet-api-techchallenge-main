# Design review: Product Catalog API

This folder documents every problem found in the original code (see [DesignProblems.txt](../../DesignProblems.txt)), how it was fixed, the trade-offs, and **what to say about it in an interview**.

Each document follows the same structure: **Problem → Why it matters → Fix (with file links) → Trade-offs → What to say in the interview** (a 30-second answer plus likely follow-up questions).

## Index

| # | Severity | Problem | Doc |
|---|---|---|---|
| 1 | Critical | Token hard-coded in four places and returned by an anonymous `/user` | [01-hardcoded-token.md](01-hardcoded-token.md) |
| 2 | Major | DI bypassed in `ProductController` | [02-dependency-injection.md](02-dependency-injection.md) |
| 3 | Major | String-typed sort contract that fails silently with HTTP 200; misleading route | [03-sort-contract.md](03-sort-contract.md) |
| 4 | Critical | Recommendation algorithm throws on duplicate names and returns nulls | [04-recommendation-algorithm.md](04-recommendation-algorithm.md) |
| 5 | Critical | No timeout, cancellation or resilience on upstream calls | [05-resilience.md](05-resilience.md) |
| 6 | Major | Log-and-throw leaks the token; no structured error handling | [06-error-handling-and-logging.md](06-error-handling-and-logging.md) |
| 7 | Major | Static `/trolleyTotal` pass-through for all HTTP verbs | [07-trolley-total-proxy.md](07-trolley-total-proxy.md) |
| 8 | Major | `double` for money, `Product` reused for order lines, broken equality | [08-money-and-domain-model.md](08-money-and-domain-model.md) |
| 9 | Major | Layering and namespace problems | [09-layering-and-namespaces.md](09-layering-and-namespaces.md) |
| 10 | Critical | No real tests, CI or quality gates | [10-tests-and-ci.md](10-tests-and-ci.md) |
| 11 | New | Caching layer | [11-caching.md](11-caching.md) |
| 12 | New | Rate limiting | [12-rate-limiting.md](12-rate-limiting.md) |
| 13 | New | Authentication | [13-authentication.md](13-authentication.md) |
| 14 | Minor | Smaller code smells | [14-code-smells.md](14-code-smells.md) |

## Request flow after the changes

```
Client ──X-Api-Key──▶ ExceptionHandler ─▶ Authentication ─▶ RateLimiter ─▶ Authorization ─▶ ProductsController
                       (ProblemDetails)    (API key → principal) (per client, 429)  (fallback: authenticated)      │ parse SortOption (400 if invalid)
                                                                                                                  ▼
                                                                                         GetSortedProductsQueryHandler
                                                                                                  │ Task.WhenAll for Recommended
                                                         ┌────────────────────────────────────────┴───────────┐
                                                         ▼                                                    ▼
                                           CachingProductCatalogClient                          CachingShopperHistoryClient
                                                         ▼                                                    ▼
                                    ProductCatalogClient (typed HttpClient)               ShopperHistoryClient (typed HttpClient)
                                    └─ WooliesXTokenHandler → Resilience (timeouts, retry, circuit breaker) → RedactingLogger → WooliesX
```

## Interview game plan

### Opening: 2-minute summary

> "I treated this like a code review of a service going to production. I grouped what I found into four themes.
>
> **1. Security.** The upstream token was hard-coded in four files and handed out by an anonymous endpoint, and it leaked into logs through exception messages. I moved it to validated configuration backed by a secret store, attached it in one `DelegatingHandler`, redacted it from logs, and put authentication in front of the API with a secure-by-default fallback policy. I'd also rotate it, because it's in git history.
>
> **2. Correctness.** The recommended sort threw on duplicate names and returned nulls. Prices were `double`. An unknown sort option silently returned unsorted data with a 200. I rewrote the ranking as a pure O(n + m) function, moved money to `decimal`, and made bad input a 400 ProblemDetails.
>
> **3. Resilience and operability.** Upstream calls had a 100-second timeout and no cancellation, retry or circuit breaker. I moved to `IHttpClientFactory` typed clients with the standard resilience handler, made cancellation flow end to end, mapped failures to 502/503/504, and added caching and per-client rate limiting.
>
> **4. Design and testability.** DI was bypassed, the domain contained HTTP clients, and the tests hit the real third-party API. I fixed the layering, injected everything, and built a 56-test suite that runs in about a second with no network, plus CI with warnings-as-errors and a vulnerability scan, which caught a real CVE on its first run."

### How to prioritise if asked "what would you fix first?"

1. **Rotate the leaked token**, and stop it leaking (logs, `/user`). Security incidents first.
2. **Fix the recommendation crash and the money type.** These are correctness bugs that customers see.
3. **Add timeouts and cancellation.** One slow dependency can take the whole service down.
4. **Fix DI and the tests**, so every later change is safe to make.
5. Everything else: caching, rate limiting, layering, smells.

### Things to say proactively (they show seniority)

- **Behaviour changes are deliberate and listed.** Invalid sort → 400 instead of 200, `/trolleyTotal` is POST-only, and every endpoint except `/` and `/health` requires an API key. The legacy `/sort` route is kept. In a real team I'd check logs for affected callers and announce the changes, or version the API.
- **What I deliberately *didn't* do, and why.** No MediatR, no repository layer, no separate projects, no Redis. Each is a reasonable next step at a different scale, and none is justified for this one yet.
- **The next steps I'd propose:** HybridCache/Redis with stampede protection and stale-if-error; JWT bearer (OIDC) for user-facing clients; gateway-level rate limits; OpenAPI/Swagger; contract tests against the upstream; containerisation and a deployment stage; OpenTelemetry tracing and metrics (upstream latency, cache hit ratio, 429 counts, circuit-breaker state).

### Questions you can ask the interviewer

- "Who calls this API: internal services or end-user apps? That decides API keys vs. OAuth."
- "How fresh do prices need to be? That sets the cache TTL."
- "Is 'recommended' meant to be units sold, distinct buyers or recency-weighted?"
- "Do we own the trolley pricing logic, or must the upstream stay the source of truth?"

## Running locally

```bash
dotnet user-secrets set "WooliesX:Token" "<upstream token>"                    --project ProductCatalog.Api
dotnet user-secrets set "Authentication:ApiKey:Clients:0:ClientId" "local-dev" --project ProductCatalog.Api
dotnet user-secrets set "Authentication:ApiKey:Clients:0:Key" "<any random key>" --project ProductCatalog.Api
dotnet run --project ProductCatalog.Api

curl -H "X-Api-Key: <key>" "https://localhost:5001/products/sort?sortOption=Recommended"
dotnet test     # 56 tests, no network access needed
```
