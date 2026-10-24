namespace ProductCatalog.Api.Domain.Products
{
    /// <summary>A line on a past order. <see cref="Quantity"/> is the number of units the shopper bought.</summary>
    public sealed record OrderedProduct(string Name, decimal Price, decimal Quantity);
}
