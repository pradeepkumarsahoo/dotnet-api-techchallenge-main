using FluentAssertions;
using Microsoft.Extensions.Caching.Memory;
using ProductCatalog.Api.Infrastructure.WooliesX;
using ProductCatalog.Tests.DataHelpers;
using ProductCatalog.Tests.Stubs;
using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace ProductCatalog.Tests
{
    public class CachingClientTests : IDisposable
    {
        private readonly MemoryCache _cache = new(new MemoryCacheOptions());
        private readonly StubProductCatalogClient _inner = new() { Products = ProductData.Unsorted };

        private CachingProductCatalogClient CreateClient(TimeSpan duration) =>
            new(_inner, _cache, new StaticOptionsMonitor<WooliesXOptions>(new WooliesXOptions { ProductsCacheDuration = duration }));

        [Fact]
        public async Task ShouldServeRepeatCallsFromCache()
        {
            var client = CreateClient(TimeSpan.FromMinutes(1));

            await client.GetProductsAsync(CancellationToken.None);
            var products = await client.GetProductsAsync(CancellationToken.None);

            products.Should().Equal(ProductData.Unsorted);
            _inner.Calls.Should().Be(1);
        }

        [Fact]
        public async Task ShouldNotCacheFailures()
        {
            var client = CreateClient(TimeSpan.FromMinutes(1));
            _inner.Failure = new HttpRequestException("boom");
            await client.Invoking(c => c.GetProductsAsync(CancellationToken.None)).Should().ThrowAsync<HttpRequestException>();

            _inner.Failure = null;
            var products = await client.GetProductsAsync(CancellationToken.None);

            products.Should().Equal(ProductData.Unsorted);
            _inner.Calls.Should().Be(2);
        }

        [Fact]
        public async Task ShouldBypassCacheWhenDurationIsZero()
        {
            var client = CreateClient(TimeSpan.Zero);

            await client.GetProductsAsync(CancellationToken.None);
            await client.GetProductsAsync(CancellationToken.None);

            _inner.Calls.Should().Be(2);
        }

        public void Dispose()
        {
            _cache.Dispose();
            GC.SuppressFinalize(this);
        }
    }
}
