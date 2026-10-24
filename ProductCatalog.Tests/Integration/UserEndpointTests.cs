using FluentAssertions;
using ProductCatalog.Api.Contracts;
using System.Net;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Xunit;

namespace ProductCatalog.Tests.Integration
{
    public class UserEndpointTests
    {
        [Fact]
        public async Task ShouldReturnConfiguredUserForAuthenticatedCaller()
        {
            using var factory = new ApiFactory();

            var user = await factory.CreateAuthenticatedClient().GetFromJsonAsync<UserResponse>("/user");

            user.Should().Be(new UserResponse("John Smith", ApiFactory.UpstreamToken));
        }

        [Fact]
        public async Task ShouldRejectAnonymousCaller()
        {
            using var factory = new ApiFactory();

            var response = await factory.CreateClient().GetAsync("/user");

            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }
    }
}
