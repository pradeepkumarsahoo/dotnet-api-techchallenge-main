# 11. New: caching layer

## Problem

Every request to `/products/sort` made a fresh call to WooliesX, and `Recommended` made two. The product list and shopper history change slowly, so most of those calls fetch the same data. That means higher latency, more load on the upstream, and an outage the moment it hiccups.

## Fix

**Decorators** over the ports, using `IMemoryCache`. See [CachingClients.cs](../../ProductCatalog.Api/Infrastructure/WooliesX/CachingClients.cs):

```csharp
services.AddTransient<IProductCatalogClient>(provider => new CachingProductCatalogClient(
    provider.GetRequiredService<ProductCatalogClient>(),   // the real typed HttpClient
    provider.GetRequiredService<IMemoryCache>(),
    provider.GetRequiredService<IOptionsMonitor<WooliesXOptions>>()));
```

- **The handler and controller are unchanged.** They don't know caching exists (Open/Closed Principle, Decorator pattern). That's a direct payoff of fixing DI ([02](02-dependency-injection.md)).
- **We cache the upstream data, not the HTTP response.** One cached product list serves all five sort options. An output cache keyed by URL would store five copies and need varying by `sortOption` (and by API key).
- **TTLs are configurable** per resource: products 1 min, shopper history 5 min. `0` disables caching. See [WooliesXOptions.cs](../../ProductCatalog.Api/Infrastructure/WooliesX/WooliesXOptions.cs).
- **Failures are never cached.** An exception leaves the cache empty, so the next request retries.
- **Cached lists are `IReadOnlyList<T>`.** The sort creates new lists and never mutates the shared cached instance.
- Tests: [CachingClientTests.cs](../../ProductCatalog.Tests/CachingClientTests.cs) covers hits, failures not cached, and TTL 0 bypassing the cache.

## Trade-offs and next steps

- **Staleness.** A price change can take up to the TTL to show. Is one minute acceptable for prices? That's a product question. If the upstream published change events, I'd invalidate on event instead.
- **Cache stampede.** When the entry expires under load, many concurrent requests can all miss at once and all call upstream. `IMemoryCache` doesn't coalesce them. Fixes: .NET 9 **`HybridCache`**, which has built-in stampede protection, or a `SemaphoreSlim`/`Lazy<Task<T>>` per key.
- **Per-instance cache.** With N pods, there are N caches and N times the upstream calls, and they may disagree briefly. For scale-out, use a distributed L2 cache (Redis via `IDistributedCache`, or `HybridCache` with L1 memory plus L2 Redis).
- **Stale-if-error.** Keep the last good value past its TTL and serve it if the upstream fails. That turns an outage into slightly stale data. It's a natural extension of this decorator.
- **Memory bounds.** Set `SizeLimit` on the cache if more keys are ever added.
- **HTTP caching for callers.** Add `Cache-Control`/`ETag` headers on `/products/sort` so clients and CDNs can cache too. That's complementary to this.

## What to say in the interview

**30-second answer:**
> "I added caching as a decorator around the upstream ports, so the use case doesn't change and it's one line of DI to turn on. I cache the upstream data, not the HTTP response, so one cached catalog serves every sort option. TTLs are configurable, failures are never cached, and cached data is read-only. For multiple instances I'd move to HybridCache with Redis as L2, which also solves cache stampede."

**Likely follow-ups:**
- *"What's a cache stampede, and how do you prevent it?"* A hot key expires and all concurrent requests hit the backend at once. Prevent it with request coalescing (single-flight), early probabilistic refresh, or a background refresh before expiry.
- *"Output caching vs. data caching?"* Output caching (`AddOutputCache`) stores whole responses. It's great for identical requests, but it varies by every parameter and must be careful with authenticated responses. Data caching sits closer to the source and is shared across endpoints.
- *"Absolute or sliding expiration?"* Absolute, because the data's freshness depends on time since fetch, not on how often it's read. Sliding expiry can keep stale data alive forever on a hot key.
