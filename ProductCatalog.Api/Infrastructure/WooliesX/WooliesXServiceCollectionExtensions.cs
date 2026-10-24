using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using ProductCatalog.Api.Application.Abstractions;
using System.Net.Http.Headers;

namespace ProductCatalog.Api.Infrastructure.WooliesX
{
    public static class WooliesXServiceCollectionExtensions
    {
        public static IServiceCollection AddWooliesXClients(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddOptions<WooliesXOptions>()
                .Bind(configuration.GetSection(WooliesXOptions.SectionName))
                .ValidateDataAnnotations()
                .ValidateOnStart();

            services.AddMemoryCache();
            services.AddTransient<WooliesXTokenHandler>();
            services.AddSingleton<RedactingHttpClientLogger>();

            var resilience = configuration.GetSection($"{WooliesXOptions.SectionName}:Resilience");
            services.AddWooliesXHttpClient<ProductCatalogClient>(resilience);
            services.AddWooliesXHttpClient<ShopperHistoryClient>(resilience);
            services.AddWooliesXHttpClient<TrolleyCalculatorClient>(resilience);

            services.AddTransient<IProductCatalogClient>(provider => new CachingProductCatalogClient(
                provider.GetRequiredService<ProductCatalogClient>(),
                provider.GetRequiredService<IMemoryCache>(),
                provider.GetRequiredService<IOptionsMonitor<WooliesXOptions>>()));
            services.AddTransient<IShopperHistoryClient>(provider => new CachingShopperHistoryClient(
                provider.GetRequiredService<ShopperHistoryClient>(),
                provider.GetRequiredService<IMemoryCache>(),
                provider.GetRequiredService<IOptionsMonitor<WooliesXOptions>>()));
            services.AddTransient<ITrolleyCalculatorClient>(provider => provider.GetRequiredService<TrolleyCalculatorClient>());

            return services;
        }

        private static void AddWooliesXHttpClient<TClient>(this IServiceCollection services, IConfigurationSection resilience)
            where TClient : class
        {
            // Handler order matters: the token handler is outermost so a retry does not append the token again,
            // and the logger is innermost so it records each attempt (with the token redacted).
            services.AddHttpClient<TClient>((provider, client) =>
                {
                    client.BaseAddress = provider.GetRequiredService<IOptions<WooliesXOptions>>().Value.BaseAddress;
                    client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                })
                .AddHttpMessageHandler<WooliesXTokenHandler>()
                .RemoveAllLoggers()
                .AddLogger<RedactingHttpClientLogger>()
                .AddStandardResilienceHandler(resilience);
        }
    }
}
