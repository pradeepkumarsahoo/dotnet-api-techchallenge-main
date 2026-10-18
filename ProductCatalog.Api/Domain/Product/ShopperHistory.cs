using System.Collections.Generic;

namespace ProductCatalog.Api.Domain.Product
{
    public class ShopperHistory : ValueObject
    {
        public ShopperHistory(string customerId, List<Product> products)
        {
            CustomerId = customerId;
            Products = products;
        }

        public string CustomerId { get; private set; }

        public List<Domain.Product.Product> Products { get; private set; }

        protected override IEnumerable<object> GetEqualityComponents()
        {
            yield return CustomerId;
            yield return Products;
        }
    }
}

//A DTO modelled as a value object.
//It represents data from an upstream API; nothing in the system compares histories by value.
//Plain DTO without ValueObject.

//Reuses Product for order lines.
//Quantity means "units ordered" here but "stock" on the catalog.
//Changing SortOption, Price and the history item type changes public signatures, so the controller, the HTTP clients and any tests must use the new types
//Not added: validation of upstream data, a response DTO separate from the domain Product.