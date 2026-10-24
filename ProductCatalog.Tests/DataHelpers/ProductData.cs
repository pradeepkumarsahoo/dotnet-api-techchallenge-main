using ProductCatalog.Api.Domain.Products;
using System.Collections.Generic;

namespace ProductCatalog.Tests.DataHelpers
{
    public static class ProductData
    {
        public static readonly List<Product> Unsorted =
        [
            new Product("Test Product B", 101.99m, 0),
            new Product("Test Product A", 99.99m, 0),
            new Product("Test Product C", 10.99m, 0),
            new Product("Test Product D", 5m, 0),
            new Product("Test Product F", 999999999999m, 0),
        ];

        public static readonly List<Product> SortedLowToHigh =
        [
            new Product("Test Product D", 5m, 0),
            new Product("Test Product C", 10.99m, 0),
            new Product("Test Product A", 99.99m, 0),
            new Product("Test Product B", 101.99m, 0),
            new Product("Test Product F", 999999999999m, 0),
        ];

        public static readonly List<Product> SortedHighToLow =
        [
            new Product("Test Product F", 999999999999m, 0),
            new Product("Test Product B", 101.99m, 0),
            new Product("Test Product A", 99.99m, 0),
            new Product("Test Product C", 10.99m, 0),
            new Product("Test Product D", 5m, 0),
        ];

        public static readonly List<Product> SortedAscending =
        [
            new Product("Test Product A", 99.99m, 0),
            new Product("Test Product B", 101.99m, 0),
            new Product("Test Product C", 10.99m, 0),
            new Product("Test Product D", 5m, 0),
            new Product("Test Product F", 999999999999m, 0),
        ];

        public static readonly List<Product> SortedDescending =
        [
            new Product("Test Product F", 999999999999m, 0),
            new Product("Test Product D", 5m, 0),
            new Product("Test Product C", 10.99m, 0),
            new Product("Test Product B", 101.99m, 0),
            new Product("Test Product A", 99.99m, 0),
        ];

        // Units ordered: A = 7, B = 4, F = 4, C = 3, D = 0. B and F tie, so they keep catalog order (B first).
        public static readonly List<Product> SortedRecommended =
        [
            new Product("Test Product A", 99.99m, 0),
            new Product("Test Product B", 101.99m, 0),
            new Product("Test Product F", 999999999999m, 0),
            new Product("Test Product C", 10.99m, 0),
            new Product("Test Product D", 5m, 0),
        ];
    }

    public static class ShopperHistoryData
    {
        public static readonly List<ShopperHistory> History =
        [
            new ShopperHistory(123,
            [
                new OrderedProduct("Test Product A", 99.99m, 3),
                new OrderedProduct("Test Product B", 101.99m, 1),
                new OrderedProduct("Test Product F", 999999999999m, 1),
            ]),
            new ShopperHistory(23,
            [
                new OrderedProduct("Test Product A", 99.99m, 2),
                new OrderedProduct("Test Product B", 101.99m, 3),
                new OrderedProduct("Test Product F", 999999999999m, 1),
            ]),
            new ShopperHistory(23,
            [
                new OrderedProduct("Test Product C", 10.99m, 2),
                new OrderedProduct("Test Product F", 999999999999m, 2),
            ]),
            new ShopperHistory(23,
            [
                new OrderedProduct("Test Product A", 99.99m, 1),
                new OrderedProduct("Test Product A", 101.99m, 1),
                new OrderedProduct("Test Product C", 10.99m, 1),
            ]),
        ];
    }
}
