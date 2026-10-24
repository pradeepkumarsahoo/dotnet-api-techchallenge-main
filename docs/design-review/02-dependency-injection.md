# 2. Dependency injection bypassed in `ProductController` — Major

## Problem

```csharp
// before
var sortedProductQueryHandler =
    new GetSortedProductQueryHandler(new ProductHttpClient(), new ShopperHistoryHttpClient());
```

`Startup` registered the handler and both clients, but the controller ignored the container and `new`-ed everything on every request.

## Why it matters

- **You can't unit-test it.** The controller always hits the real WooliesX API, which is why the original controller tests were network tests.
- **The container's registrations were dead code**: misleading, because a reader assumes they are used.
- **Nobody manages lifetimes and configuration.** You can't swap in a cached client, add a decorator, or configure the `HttpClient`.
- **The controller depends on concrete infrastructure** instead of an abstraction (a Dependency Inversion Principle violation).

## Fix

- The controller takes `GetSortedProductsQueryHandler` through its constructor. The handler takes `IProductCatalogClient` and `IShopperHistoryClient`. Nothing calls `new` on a dependency. See [ProductsController.cs](../../ProductCatalog.Api/Controllers/ProductsController.cs) and [GetSortedProductsQuery.cs](../../ProductCatalog.Api/Application/Products/GetSortedProductsQuery.cs).
- The HTTP clients are **typed clients from `IHttpClientFactory`** (`AddHttpClient<T>`). That gives pooled handlers, DNS refresh, and a pipeline where token, logging and resilience handlers plug in. See [WooliesXServiceCollectionExtensions.cs](../../ProductCatalog.Api/Infrastructure/WooliesX/WooliesXServiceCollectionExtensions.cs).
- Because the graph is in the container, **caching becomes a decorator** registered in one line, without touching the handler (see [11-caching.md](11-caching.md)).
- The integration tests swap the upstream ports for stubs with `ConfigureTestServices` (see [ApiFactory.cs](../../ProductCatalog.Tests/Integration/ApiFactory.cs)). A composition test resolves the real graph to catch wiring mistakes ([CompositionTests.cs](../../ProductCatalog.Tests/Integration/CompositionTests.cs)).

**Lifetimes chosen:**

| Service | Lifetime | Why |
|---|---|---|
| `GetSortedProductsQueryHandler` | Scoped | Stateless; one per request is the natural fit and safe if it later takes a scoped dependency (e.g. a `DbContext`). |
| Typed clients | Transient (the factory's default) | The factory hands out cheap `HttpClient` wrappers; the expensive handler is pooled and recycled. |
| `IMemoryCache`, `RedactingHttpClientLogger` | Singleton | Shared state / stateless. |

## What to say in the interview

**30-second answer:**
> "The controller was newing up its dependencies even though Startup had registered them, so it couldn't be tested without the network and the registrations were dead code. I switched to constructor injection all the way down and moved the HTTP clients to `IHttpClientFactory` typed clients. The payoff showed up straight away: caching became a decorator, and the integration tests run the real pipeline with stubbed upstreams, no network."

**Likely follow-ups:**
- *"What's wrong with `new HttpClient()` per request?"* Socket exhaustion: sockets sit in TIME_WAIT. A single static `HttpClient` has the opposite problem: stale DNS. `IHttpClientFactory` pools and rotates handlers (2-minute default lifetime). Alternatively, use a long-lived `SocketsHttpHandler` with `PooledConnectionLifetime`.
- *"Captive dependency?"* A singleton holding a scoped or transient service keeps it alive forever. Example: injecting a typed client into a singleton pins its handler and loses DNS rotation. The container's `ValidateScopes` in Development catches this.
- *"Why not MediatR?"* For one query it's ceremony. A plain handler class is equally testable. I'd add it only once there are pipeline behaviours (validation, logging) worth sharing across many handlers.
