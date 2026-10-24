using ProductCatalog.Api.Application.Abstractions;
using ProductCatalog.Api.Domain.Products;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace ProductCatalog.Api.Application.Products
{
    public sealed record GetSortedProductsQuery(SortOption SortOption);

    public sealed class GetSortedProductsQueryHandler
    {
        private readonly IProductCatalogClient _productCatalogClient;
        private readonly IShopperHistoryClient _shopperHistoryClient;

        public GetSortedProductsQueryHandler(
            IProductCatalogClient productCatalogClient,
            IShopperHistoryClient shopperHistoryClient)
        {
            _productCatalogClient = productCatalogClient;
            _shopperHistoryClient = shopperHistoryClient;
        }

        public async Task<IReadOnlyList<Product>> Handle(GetSortedProductsQuery query, CancellationToken cancellationToken)
        {
            if (query.SortOption == SortOption.Recommended)
                return await Recommend(cancellationToken);

            var products = await _productCatalogClient.GetProductsAsync(cancellationToken);

            return query.SortOption switch
            {
                SortOption.Low => products.OrderBy(product => product.Price).ToList(),
                SortOption.High => products.OrderByDescending(product => product.Price).ToList(),
                SortOption.Ascending => products.OrderBy(product => product.Name, StringComparer.OrdinalIgnoreCase).ToList(),
                SortOption.Descending => products.OrderByDescending(product => product.Name, StringComparer.OrdinalIgnoreCase).ToList(),
                _ => throw new ArgumentOutOfRangeException(nameof(query), query.SortOption, "Unsupported sort option.")
            };
        }

        private async Task<IReadOnlyList<Product>> Recommend(CancellationToken cancellationToken)
        {
            // The two upstream calls are independent, so run them concurrently.
            var productsTask = _productCatalogClient.GetProductsAsync(cancellationToken);
            var historyTask = _shopperHistoryClient.GetShopperHistoryAsync(cancellationToken);
            await Task.WhenAll(productsTask, historyTask);

            return PopularityRanking.Rank(await productsTask, await historyTask);
        }
    }
}
