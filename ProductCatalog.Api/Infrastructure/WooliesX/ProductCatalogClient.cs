using ProductCatalog.Api.Application.Abstractions;
using ProductCatalog.Api.Domain.Products;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace ProductCatalog.Api.Infrastructure.WooliesX
{
    public sealed class ProductCatalogClient : IProductCatalogClient
    {
        private const string Resource = "api/resource/products";
        private readonly HttpClient _httpClient;

        public ProductCatalogClient(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public Task<IReadOnlyList<Product>> GetProductsAsync(CancellationToken cancellationToken) =>
            _httpClient.GetFromWooliesXAsync<IReadOnlyList<Product>>(Resource, cancellationToken);
    }
}
