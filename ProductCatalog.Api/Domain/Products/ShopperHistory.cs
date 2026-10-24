using System.Collections.Generic;

namespace ProductCatalog.Api.Domain.Products
{
    /// <summary>One past order placed by a shopper.</summary>
    public sealed record ShopperHistory(long CustomerId, IReadOnlyList<OrderedProduct> Products);
}
