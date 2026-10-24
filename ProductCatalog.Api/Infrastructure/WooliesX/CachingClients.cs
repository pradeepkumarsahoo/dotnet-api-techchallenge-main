using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using ProductCatalog.Api.Application.Abstractions;
using ProductCatalog.Api.Domain.Products;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace ProductCatalog.Api.Infrastructure.WooliesX
{
    /// <summary>Decorates <see cref="IProductCatalogClient"/> with a short-lived in-memory cache.</summary>
    public sealed class CachingProductCatalogClient : IProductCatalogClient
    {
        private const string CacheKey = "wooliesx:products";
        private readonly IProductCatalogClient _inner;
        private readonly IMemoryCache _cache;
        private readonly IOptionsMonitor<WooliesXOptions> _options;

        public CachingProductCatalogClient(IProductCatalogClient inner, IMemoryCache cache, IOptionsMonitor<WooliesXOptions> options)
        {
            _inner = inner;
            _cache = cache;
            _options = options;
        }

        public Task<IReadOnlyList<Product>> GetProductsAsync(CancellationToken cancellationToken) =>
            _cache.GetOrFetchAsync(CacheKey, _options.CurrentValue.ProductsCacheDuration, () => _inner.GetProductsAsync(cancellationToken));
    }

    /// <summary>Decorates <see cref="IShopperHistoryClient"/> with a short-lived in-memory cache.</summary>
    public sealed class CachingShopperHistoryClient : IShopperHistoryClient
    {
        private const string CacheKey = "wooliesx:shopper-history";
        private readonly IShopperHistoryClient _inner;
        private readonly IMemoryCache _cache;
        private readonly IOptionsMonitor<WooliesXOptions> _options;

        public CachingShopperHistoryClient(IShopperHistoryClient inner, IMemoryCache cache, IOptionsMonitor<WooliesXOptions> options)
        {
            _inner = inner;
            _cache = cache;
            _options = options;
        }

        public Task<IReadOnlyList<ShopperHistory>> GetShopperHistoryAsync(CancellationToken cancellationToken) =>
            _cache.GetOrFetchAsync(CacheKey, _options.CurrentValue.ShopperHistoryCacheDuration, () => _inner.GetShopperHistoryAsync(cancellationToken));
    }

    internal static class MemoryCacheExtensions
    {
        /// <summary>Failures are not cached: an exception from <paramref name="fetch"/> leaves the cache empty.</summary>
        public static async Task<T> GetOrFetchAsync<T>(this IMemoryCache cache, string key, TimeSpan duration, Func<Task<T>> fetch)
        {
            if (duration <= TimeSpan.Zero)
                return await fetch();

            if (cache.TryGetValue(key, out T? cached) && cached is not null)
                return cached;

            var value = await fetch();
            cache.Set(key, value, duration);
            return value;
        }
    }
}
