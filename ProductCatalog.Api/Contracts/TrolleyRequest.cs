using ProductCatalog.Api.Domain.Trolleys;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;

namespace ProductCatalog.Api.Contracts
{
    public sealed class TrolleyRequest
    {
        [Required]
        public List<TrolleyProductRequest>? Products { get; init; }

        public List<TrolleySpecialRequest> Specials { get; init; } = [];

        [Required]
        public List<TrolleyQuantityRequest>? Quantities { get; init; }

        public Trolley ToDomain() => new(
            (Products ?? []).Select(product => new TrolleyProduct(product.Name!, product.Price)).ToList(),
            Specials.Select(special => new TrolleySpecial(ToDomain(special.Quantities), special.Total)).ToList(),
            ToDomain(Quantities));

        private static List<TrolleyQuantity> ToDomain(IEnumerable<TrolleyQuantityRequest>? quantities) =>
            (quantities ?? []).Select(quantity => new TrolleyQuantity(quantity.Name!, quantity.Quantity)).ToList();
    }

    public sealed class TrolleyProductRequest
    {
        [Required]
        public string? Name { get; init; }

        [Range(0, double.MaxValue)]
        public decimal Price { get; init; }
    }

    public sealed class TrolleySpecialRequest
    {
        [Required]
        public List<TrolleyQuantityRequest>? Quantities { get; init; }

        [Range(0, double.MaxValue)]
        public decimal Total { get; init; }
    }

    public sealed class TrolleyQuantityRequest
    {
        [Required]
        public string? Name { get; init; }

        [Range(0, double.MaxValue)]
        public decimal Quantity { get; init; }
    }
}
