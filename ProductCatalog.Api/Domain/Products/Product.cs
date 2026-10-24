namespace ProductCatalog.Api.Domain.Products
{
    /// <summary>A product in the catalog. <see cref="Quantity"/> is the stock quantity reported by the catalog.</summary>
    public sealed record Product(string Name, decimal Price, decimal Quantity);
}
