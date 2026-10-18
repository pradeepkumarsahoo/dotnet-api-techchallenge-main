using Newtonsoft.Json;
using System.Collections.Generic;

namespace ProductCatalog.Api.Domain.Product
{
    public class Product : ValueObject
    {
        [JsonProperty] public string Name { get; }

        [JsonProperty] public double Price { get; }

        [JsonProperty] public double Quantity { get; }

        public Product(string name, double price, double quantity)
        {
            Name = name;
            Price = price;
            Quantity = quantity;
        }

        protected override IEnumerable<object> GetEqualityComponents()
        {
            yield return Name;
            yield return Price;
            yield return Quantity;
        }
    }
}

//double for money 
//Binary floating point cannot represent values like 0.1 exactly; equality and totals can be off by tiny amounts.
//decimal Price. JSON numbers stay the same on the wire.

//Serialisation attributes inside the domain type.
//Misleading, and ties the domain model to one JSON library.

//Property attributes removed. A single [JsonConstructor] states how upstream JSON is read.

