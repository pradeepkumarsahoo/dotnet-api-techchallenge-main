using ProductCatalog.Api.Application.Abstractions;
using ProductCatalog.Api.Domain.Products;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace ProductCatalog.Api.Infrastructure.WooliesX
{
    public sealed class ShopperHistoryClient : IShopperHistoryClient
    {
        private const string Resource = "api/resource/shopperHistory";
        private readonly HttpClient _httpClient;

        public ShopperHistoryClient(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public Task<IReadOnlyList<ShopperHistory>> GetShopperHistoryAsync(CancellationToken cancellationToken) =>
            _httpClient.GetFromWooliesXAsync<IReadOnlyList<ShopperHistory>>(Resource, cancellationToken);
    }
}
