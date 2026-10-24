using ProductCatalog.Api.Domain.Products;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace ProductCatalog.Api.Application.Abstractions
{
    public interface IShopperHistoryClient
    {
        Task<IReadOnlyList<ShopperHistory>> GetShopperHistoryAsync(CancellationToken cancellationToken);
    }
}
