using FluentAssertions;
using ProductCatalog.Api.Domain.Products;
using System.Collections.Generic;
using Xunit;

namespace ProductCatalog.Tests
{
    public class PopularityRankingTests
    {
        [Fact]
        public void ShouldNotThrowWhenCatalogContainsDuplicateNames()
        {
            List<Product> catalog = [new("A", 1m, 0), new("A", 2m, 0), new("B", 3m, 0)];
            List<ShopperHistory> history = [new(1, [new OrderedProduct("B", 3m, 1)])];

            var ranked = PopularityRanking.Rank(catalog, history);

            ranked.Should().Equal(new Product("B", 3m, 0), new Product("A", 1m, 0), new Product("A", 2m, 0));
        }

        [Fact]
        public void ShouldIgnoreOrderedProductsMissingFromCatalog()
        {
            List<Product> catalog = [new("A", 1m, 0)];
            List<ShopperHistory> history = [new(1, [new OrderedProduct("Discontinued", 9m, 50)])];

            var ranked = PopularityRanking.Rank(catalog, history);

            ranked.Should().Equal(new Product("A", 1m, 0));
            ranked.Should().NotContainNulls();
        }

        [Fact]
        public void ShouldKeepCatalogOrderWhenNothingWasOrdered()
        {
            List<Product> catalog = [new("C", 1m, 0), new("A", 2m, 0), new("B", 3m, 0)];

            var ranked = PopularityRanking.Rank(catalog, []);

            ranked.Should().Equal(catalog);
        }

        [Fact]
        public void ShouldRankByUnitsOrderedNotByNumberOfOrders()
        {
            List<Product> catalog = [new("A", 1m, 0), new("B", 1m, 0)];
            List<ShopperHistory> history =
            [
                new(1, [new OrderedProduct("A", 1m, 1)]),
                new(2, [new OrderedProduct("A", 1m, 1)]),
                new(3, [new OrderedProduct("B", 1m, 5)]),
            ];

            var ranked = PopularityRanking.Rank(catalog, history);

            ranked.Should().Equal(new Product("B", 1m, 0), new Product("A", 1m, 0));
        }
    }
}
