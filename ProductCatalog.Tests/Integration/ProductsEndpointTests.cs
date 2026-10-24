using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using ProductCatalog.Api.Application.Abstractions;
using ProductCatalog.Api.Contracts;
using ProductCatalog.Tests.DataHelpers;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Xunit;

namespace ProductCatalog.Tests.Integration
{
    public class ProductsEndpointTests
    {
        private static List<ProductResponse> AsResponses(IEnumerable<Api.Domain.Products.Product> products) =>
            products.Select(ProductResponse.FromDomain).ToList();

        [Theory]
        [InlineData("/products/sort?sortOption=High")]
        [InlineData("/products/sort?sortOption=high")]
        [InlineData("/sort?sortOption=HIGH")]
        public async Task ShouldReturnSortedProducts(string url)
        {
            using var factory = new ApiFactory();
            factory.Products.Products = ProductData.Unsorted;

            var response = await factory.CreateAuthenticatedClient().GetAsync(url);

            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var products = await response.Content.ReadFromJsonAsync<List<ProductResponse>>();
            products.Should().Equal(AsResponses(ProductData.SortedHighToLow));
        }

        [Fact]
        public async Task ShouldReturnRecommendedProducts()
        {
            using var factory = new ApiFactory();
            factory.Products.Products = ProductData.Unsorted;
            factory.History.History = ShopperHistoryData.History;

            var products = await factory.CreateAuthenticatedClient()
                .GetFromJsonAsync<List<ProductResponse>>("/products/sort?sortOption=Recommended");

            products.Should().Equal(AsResponses(ProductData.SortedRecommended));
        }

        [Theory]
        [InlineData("/products/sort")]
        [InlineData("/products/sort?sortOption=")]
        [InlineData("/products/sort?sortOption=Lowest")]
        [InlineData("/products/sort?sortOption=1")]
        public async Task ShouldRejectInvalidSortOption(string url)
        {
            using var factory = new ApiFactory();

            var response = await factory.CreateAuthenticatedClient().GetAsync(url);

            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
            var problem = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>();
            problem!.Errors.Should().ContainKey("sortOption");
            factory.Products.Calls.Should().Be(0);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("wrong-key")]
        public async Task ShouldRequireValidApiKey(string? apiKey)
        {
            using var factory = new ApiFactory();
            var client = factory.CreateClient();
            if (apiKey is not null)
                client.DefaultRequestHeaders.Add("X-Api-Key", apiKey);

            var response = await client.GetAsync("/products/sort?sortOption=Low");

            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }

        [Fact]
        public async Task ShouldReturnBadGatewayProblemWhenUpstreamFails()
        {
            using var factory = new ApiFactory();
            factory.Products.Failure = new UpstreamServiceException("api/resource/products", HttpStatusCode.InternalServerError);

            var response = await factory.CreateAuthenticatedClient().GetAsync("/products/sort?sortOption=Low");

            response.StatusCode.Should().Be(HttpStatusCode.BadGateway);
            response.Content.Headers.ContentType!.MediaType.Should().Be("application/problem+json");
            var body = await response.Content.ReadAsStringAsync();
            body.Should().NotContain(ApiFactory.UpstreamToken).And.NotContain("api/resource");
        }

        [Theory]
        [InlineData("/")]
        [InlineData("/health")]
        public async Task ShouldAllowAnonymousAccessToRootAndHealth(string url)
        {
            using var factory = new ApiFactory();

            var response = await factory.CreateClient().GetAsync(url);

            response.StatusCode.Should().Be(HttpStatusCode.OK);
        }
    }
}
