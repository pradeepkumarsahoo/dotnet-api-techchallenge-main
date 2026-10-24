using System;
using System.Collections.Generic;
using System.Linq;

namespace ProductCatalog.Api.Domain.Products
{
    public static class PopularityRanking
    {
        /// <summary>
        /// Orders the catalog by total units ordered across all shopper histories, most popular first.
        /// Every catalog product is returned exactly once; products never ordered keep their catalog order at the end,
        /// and ordered items that are not in the catalog are ignored.
        /// </summary>
        public static IReadOnlyList<Product> Rank(IEnumerable<Product> products, IEnumerable<ShopperHistory> histories)
        {
            var unitsOrderedByName = histories
                .SelectMany(history => history.Products ?? Enumerable.Empty<OrderedProduct>())
                .GroupBy(line => line.Name, StringComparer.Ordinal)
                .ToDictionary(group => group.Key, group => group.Sum(line => line.Quantity), StringComparer.Ordinal);

            // OrderByDescending is a stable sort, so ties keep the order the catalog returned them in.
            return products
                .OrderByDescending(product => unitsOrderedByName.GetValueOrDefault(product.Name))
                .ToList();
        }
    }
}
