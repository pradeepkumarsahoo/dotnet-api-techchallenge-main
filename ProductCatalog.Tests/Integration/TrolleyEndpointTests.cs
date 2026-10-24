using FluentAssertions;
using ProductCatalog.Api.Domain.Trolleys;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace ProductCatalog.Tests.Integration
{
    public class TrolleyEndpointTests
    {
        private const string ValidTrolley = """
            {
              "products": [{ "name": "test", "price": 100.0 }],
              "specials": [
                { "quantities": [{ "name": "test", "quantity": 2 }], "total": 150 }
              ],
              "quantities": [{ "name": "test", "quantity": 2 }]
            }
            """;

        private static StringContent Json(string body) => new(body, Encoding.UTF8, "application/json");

        [Fact]
        public async Task ShouldReturnTotalFromCalculator()
        {
            using var factory = new ApiFactory();
            factory.Trolley.Total = 150.0m;

            var response = await factory.CreateAuthenticatedClient().PostAsync("/trolleyTotal", Json(ValidTrolley));

            response.StatusCode.Should().Be(HttpStatusCode.OK);
            (await response.Content.ReadAsStringAsync()).Should().Be("150.0");
            factory.Trolley.ReceivedTrolley!.Products.Should().Equal(new TrolleyProduct("test", 100.0m));
            factory.Trolley.ReceivedTrolley.Specials.Should().ContainSingle().Which.Total.Should().Be(150m);
        }

        [Fact]
        public async Task ShouldRejectInvalidBody()
        {
            using var factory = new ApiFactory();

            var response = await factory.CreateAuthenticatedClient().PostAsync("/trolleyTotal", Json("""{ "specials": [] }"""));

            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
            factory.Trolley.ReceivedTrolley.Should().BeNull();
        }

        [Fact]
        public async Task ShouldOnlyAcceptPost()
        {
            using var factory = new ApiFactory();

            var response = await factory.CreateAuthenticatedClient().GetAsync("/trolleyTotal");

            response.StatusCode.Should().Be(HttpStatusCode.MethodNotAllowed);
        }
    }
}
