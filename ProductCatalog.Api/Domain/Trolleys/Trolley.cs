using System.Collections.Generic;

namespace ProductCatalog.Api.Domain.Trolleys
{
    public sealed record Trolley(
        IReadOnlyList<TrolleyProduct> Products,
        IReadOnlyList<TrolleySpecial> Specials,
        IReadOnlyList<TrolleyQuantity> Quantities);

    public sealed record TrolleyProduct(string Name, decimal Price);

    public sealed record TrolleySpecial(IReadOnlyList<TrolleyQuantity> Quantities, decimal Total);

    public sealed record TrolleyQuantity(string Name, decimal Quantity);
}
