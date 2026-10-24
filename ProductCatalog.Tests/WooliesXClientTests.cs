using FluentAssertions;
using ProductCatalog.Api.Application.Abstractions;
using ProductCatalog.Api.Domain.Products;
using ProductCatalog.Api.Infrastructure.WooliesX;
using ProductCatalog.Tests.Stubs;
using System;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace ProductCatalog.Tests
{
    public class WooliesXClientTests
    {
        private static readonly Uri BaseAddress = new("https://upstream.test/");

        [Fact]
        public async Task ProductClientShouldReadUpstreamJson()
        {
            var handler = new RecordingHandler(HttpStatusCode.OK, """[{"name":"Test Product A","price":99.99,"quantity":0.0}]""");
            var client = new ProductCatalogClient(new HttpClient(handler) { BaseAddress = BaseAddress });

            var products = await client.GetProductsAsync(CancellationToken.None);

            products.Should().Equal(new Product("Test Product A", 99.99m, 0m));
            handler.LastRequestUri.Should().Be(new Uri(BaseAddress, "api/resource/products"));
        }

        [Fact]
        public async Task ShopperHistoryClientShouldAcceptNumericCustomerIds()
        {
            var handler = new RecordingHandler(HttpStatusCode.OK,
                """[{"customerId":123,"products":[{"name":"Test Product A","price":99.99,"quantity":3.0}]}]""");
            var client = new ShopperHistoryClient(new HttpClient(handler) { BaseAddress = BaseAddress });

            var history = await client.GetShopperHistoryAsync(CancellationToken.None);

            history.Should().ContainSingle().Which.CustomerId.Should().Be(123);
            history[0].Products.Should().Equal(new OrderedProduct("Test Product A", 99.99m, 3m));
        }

        [Theory]
        [InlineData(HttpStatusCode.InternalServerError, "{}")]
        [InlineData(HttpStatusCode.OK, "not json")]
        public async Task ShouldWrapUpstreamFailures(HttpStatusCode status, string body)
        {
            var client = new ProductCatalogClient(new HttpClient(new RecordingHandler(status, body)) { BaseAddress = BaseAddress });

            var act = () => client.GetProductsAsync(CancellationToken.None);

            await act.Should().ThrowAsync<UpstreamServiceException>();
        }

        [Theory]
        [InlineData("https://upstream.test/api/resource/products", "https://upstream.test/api/resource/products?token=secret%20value")]
        [InlineData("https://upstream.test/api/resource/products?a=1", "https://upstream.test/api/resource/products?a=1&token=secret%20value")]
        public async Task TokenHandlerShouldAppendTokenOnce(string requestUri, string expected)
        {
            var inner = new RecordingHandler(HttpStatusCode.OK, "[]");
            var handler = new WooliesXTokenHandler(new StaticOptionsMonitor<WooliesXOptions>(new WooliesXOptions { Token = "secret value" }))
            {
                InnerHandler = inner
            };

            using var invoker = new HttpMessageInvoker(handler);
            await invoker.SendAsync(new HttpRequestMessage(HttpMethod.Get, requestUri), CancellationToken.None);

            inner.LastRequestUri!.AbsoluteUri.Should().Be(expected);
        }

        private sealed class RecordingHandler : HttpMessageHandler
        {
            private readonly HttpStatusCode _status;
            private readonly string _body;

            public RecordingHandler(HttpStatusCode status, string body)
            {
                _status = status;
                _body = body;
            }

            public Uri? LastRequestUri { get; private set; }

            protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            {
                LastRequestUri = request.RequestUri;
                return Task.FromResult(new HttpResponseMessage(_status)
                {
                    Content = new StringContent(_body, Encoding.UTF8, "application/json")
                });
            }
        }
    }
}
