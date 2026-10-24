using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using ProductCatalog.Api.Application.Abstractions;
using ProductCatalog.Api.Infrastructure.WooliesX;
using System;
using Xunit;

namespace ProductCatalog.Tests.Integration
{
    /// <summary>Checks the production service graph (no stubs) without making any network calls.</summary>
    public class CompositionTests
    {
        [Fact]
        public void ShouldResolveCachedTypedClients()
        {
            using var factory = new ApiFactory { UseStubs = false };
            using var scope = factory.Services.CreateScope();

            scope.ServiceProvider.GetRequiredService<IProductCatalogClient>().Should().BeOfType<CachingProductCatalogClient>();
            scope.ServiceProvider.GetRequiredService<IShopperHistoryClient>().Should().BeOfType<CachingShopperHistoryClient>();
            scope.ServiceProvider.GetRequiredService<ITrolleyCalculatorClient>().Should().BeOfType<TrolleyCalculatorClient>();
        }

        [Fact]
        public void ShouldFailFastWhenUpstreamTokenIsMissing()
        {
            using var factory = new ApiFactory { UpstreamTokenSetting = "" };

            var act = () => factory.Services;

            act.Should().Throw<OptionsValidationException>().WithMessage("*Token*");
        }
    }
}
