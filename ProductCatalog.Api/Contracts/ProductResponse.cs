using ProductCatalog.Api.Domain.Products;

namespace ProductCatalog.Api.Contracts
{
    /// <summary>Public response shape for a product. Kept separate from the domain type so either can change independently.</summary>
    public sealed record ProductResponse(string Name, decimal Price, decimal Quantity)
    {
        public static ProductResponse FromDomain(Product product) => new(product.Name, product.Price, product.Quantity);
    }
}
