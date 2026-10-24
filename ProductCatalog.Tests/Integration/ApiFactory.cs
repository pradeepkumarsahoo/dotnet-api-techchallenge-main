using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ProductCatalog.Api;
using ProductCatalog.Api.Application.Abstractions;
using ProductCatalog.Tests.Stubs;
using System.Collections.Generic;
using System.Globalization;
using System.Net.Http;

namespace ProductCatalog.Tests.Integration
{
    /// <summary>Hosts the real pipeline in memory with every upstream dependency replaced by a stub.</summary>
    public sealed class ApiFactory : WebApplicationFactory<Startup>
    {
        public const string ApiKey = "test-api-key";
        public const string UpstreamToken = "test-upstream-token";

        public StubProductCatalogClient Products { get; } = new();

        public StubShopperHistoryClient History { get; } = new();

        public StubTrolleyCalculatorClient Trolley { get; } = new();

        public int PermitLimit { get; init; } = 1000;

        public string UpstreamTokenSetting { get; init; } = UpstreamToken;

        public bool UseStubs { get; init; } = true;

        public HttpClient CreateAuthenticatedClient()
        {
            var client = CreateClient();
            client.DefaultRequestHeaders.Add("X-Api-Key", ApiKey);
            return client;
        }

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Testing");
            builder.ConfigureAppConfiguration((_, configuration) => configuration.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["WooliesX:Token"] = UpstreamTokenSetting,
                ["Authentication:ApiKey:Clients:0:ClientId"] = "tests",
                ["Authentication:ApiKey:Clients:0:Key"] = ApiKey,
                ["RateLimiting:PermitLimit"] = PermitLimit.ToString(CultureInfo.InvariantCulture),
            }));
            if (!UseStubs)
                return;

            builder.ConfigureTestServices(services =>
            {
                services.AddSingleton<IProductCatalogClient>(Products);
                services.AddSingleton<IShopperHistoryClient>(History);
                services.AddSingleton<ITrolleyCalculatorClient>(Trolley);
            });
        }
    }
}
