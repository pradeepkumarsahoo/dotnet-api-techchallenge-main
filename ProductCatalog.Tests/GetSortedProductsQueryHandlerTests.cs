using FluentAssertions;
using ProductCatalog.Api.Application.Products;
using ProductCatalog.Api.Domain.Products;
using ProductCatalog.Tests.DataHelpers;
using ProductCatalog.Tests.Stubs;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace ProductCatalog.Tests
{
    public class GetSortedProductsQueryHandlerTests
    {
        private readonly StubProductCatalogClient _products = new() { Products = ProductData.Unsorted };
        private readonly StubShopperHistoryClient _history = new() { History = ShopperHistoryData.History };

        private Task<IReadOnlyList<Product>> Handle(SortOption option) =>
            new GetSortedProductsQueryHandler(_products, _history).Handle(new GetSortedProductsQuery(option), CancellationToken.None);

        public static TheoryData<SortOption, List<Product>> Sorts => new()
        {
            { SortOption.Low, ProductData.SortedLowToHigh },
            { SortOption.High, ProductData.SortedHighToLow },
            { SortOption.Ascending, ProductData.SortedAscending },
            { SortOption.Descending, ProductData.SortedDescending },
            { SortOption.Recommended, ProductData.SortedRecommended },
        };

        [Theory]
        [MemberData(nameof(Sorts))]
        public async Task ShouldSortProducts(SortOption option, List<Product> expected)
        {
            var products = await Handle(option);

            products.Should().Equal(expected);
        }

        [Fact]
        public async Task ShouldReturnEmptyListWhenCatalogIsEmpty()
        {
            _products.Products = [];

            var products = await Handle(SortOption.Low);

            products.Should().BeEmpty();
        }

        [Theory]
        [InlineData(SortOption.Low)]
        [InlineData(SortOption.High)]
        [InlineData(SortOption.Ascending)]
        [InlineData(SortOption.Descending)]
        public async Task ShouldNotCallShopperHistoryUnlessRecommended(SortOption option)
        {
            await Handle(option);

            _history.Calls.Should().Be(0);
        }

        [Fact]
        public async Task ShouldRejectUnknownSortOption()
        {
            var act = () => Handle((SortOption)999);

            await act.Should().ThrowAsync<ArgumentOutOfRangeException>();
        }
    }
}
