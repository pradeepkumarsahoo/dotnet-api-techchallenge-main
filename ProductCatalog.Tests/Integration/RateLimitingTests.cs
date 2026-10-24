using FluentAssertions;
using System.Net;
using System.Threading.Tasks;
using Xunit;

namespace ProductCatalog.Tests.Integration
{
    public class RateLimitingTests
    {
        [Fact]
        public async Task ShouldRejectRequestsOverTheLimit()
        {
            using var factory = new ApiFactory { PermitLimit = 2 };
            var client = factory.CreateAuthenticatedClient();

            (await client.GetAsync("/user")).StatusCode.Should().Be(HttpStatusCode.OK);
            (await client.GetAsync("/user")).StatusCode.Should().Be(HttpStatusCode.OK);
            var rejected = await client.GetAsync("/user");

            rejected.StatusCode.Should().Be(HttpStatusCode.TooManyRequests);
            rejected.Headers.Contains("Retry-After").Should().BeTrue();
        }
    }
}
