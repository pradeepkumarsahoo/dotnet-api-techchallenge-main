# 8. `double` for money, `Product` reused for order lines, broken equality — Major

## Problem

```csharp
// before
public class Product : ValueObject
{
    [JsonProperty] public double Price { get; }
    [JsonProperty] public double Quantity { get; }
    ...
}
public class ShopperHistory : ValueObject
{
    public List<Domain.Product.Product> Products { get; private set; }   // order lines reuse the catalog type
    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return CustomerId;
        yield return Products;   // List<T> compares by reference
    }
}
```

1. **`double` for money.** Binary floating point can't represent 0.1 or 99.99 exactly, so `0.1 + 0.2 != 0.3`, and totals and equality checks drift by fractions of a cent.
2. **One type, two meanings.** `Product.Quantity` means *stock on hand* in the catalog but *units bought* in shopper history. That invites bugs like summing stock as if it were sales.
3. **Broken equality in the hand-rolled `ValueObject`:**
   - `ShopperHistory` equality compared the `List<Product>` **by reference**, so two identical histories were never equal.
   - `GetHashCode` used `Aggregate` with no seed, which throws on an object with no components, and XOR combines collide easily (`a ^ a == 0`).
   - `ShopperHistory` is an upstream DTO. Nothing needs value semantics for it, so `ValueObject` was the wrong tool.
4. **Misleading `[JsonProperty]` attributes.** They're Newtonsoft attributes on a domain type, but ASP.NET Core serialises with `System.Text.Json`, so they did nothing.

## Fix

- **`decimal` for every price, total and quantity.** The JSON numbers on the wire are unchanged.
- **C# `record`s** replace `ValueObject`. The compiler generates correct `Equals`/`GetHashCode` with no hand-written code to get wrong. `ValueObject.cs` is deleted.
- **Separate types for separate concepts.** See [Product.cs](../../ProductCatalog.Api/Domain/Products/Product.cs), [OrderedProduct.cs](../../ProductCatalog.Api/Domain/Products/OrderedProduct.cs) and [ShopperHistory.cs](../../ProductCatalog.Api/Domain/Products/ShopperHistory.cs).
  ```csharp
  public sealed record Product(string Name, decimal Price, decimal Quantity);         // catalog: stock quantity
  public sealed record OrderedProduct(string Name, decimal Price, decimal Quantity);  // order line: units bought
  public sealed record ShopperHistory(long CustomerId, IReadOnlyList<OrderedProduct> Products);
  ```
- **Collections are `IReadOnlyList<T>`**, so callers, and the cache, can't mutate shared data.
- **The API response is its own contract**, `ProductResponse`, mapped from the domain. The domain can change without breaking the public JSON. See [ProductResponse.cs](../../ProductCatalog.Api/Contracts/ProductResponse.cs).
- **No JSON attributes in the domain.** `System.Text.Json` web defaults (camelCase, case-insensitive, numbers-from-strings) handle the upstream shape. The `CustomerId` is a JSON number upstream and that's covered by a test. See [WooliesXClientTests.cs](../../ProductCatalog.Tests/WooliesXClientTests.cs).
- Newtonsoft.Json and Flurl are removed from the API project.

## Trade-offs

- `decimal` arithmetic is slower than `double` because it's done in software, not by the FPU. That's irrelevant at this scale, and correctness wins for money. Some systems use integer minor units (`long` cents) instead. That's also fine, as long as it's consistent.
- `OrderedProduct.Quantity` is `decimal`, not `int`, because the upstream sometimes sends `3.0`. Sold-by-weight items (1.5 kg) make that legitimate anyway.
- Record equality on `ShopperHistory` still compares the list by reference. That's fine, because it's never compared. If it were, I'd implement `Equals` with `SequenceEqual` explicitly.
- A full **`Money` value object** (amount + currency) is the next step if multi-currency ever appears. Today it's YAGNI.

## What to say in the interview

**30-second answer:**
> "Prices were `double`, which can't represent cents exactly. I switched every money value to `decimal`. The catalog `Product` was also reused for order lines, where `Quantity` meant something completely different, so I split it into `Product` and `OrderedProduct`. And I replaced the hand-rolled `ValueObject`, whose equality was broken for lists and whose hash code could throw, with C# records. That means less code and correct equality by construction. The API now returns its own response DTO instead of the domain type."

**Likely follow-ups:**
- *"Demonstrate the double problem."* `0.1 + 0.2 == 0.3` is `false` with `double` (the sum is `0.30000000000000004`) and `true` with `decimal`, which is base-10.
- *"Why separate DTOs from domain models? Isn't it duplication?"* It decouples the public contract from internal refactoring. Rename a domain property and the API doesn't change. It also stops you accidentally exposing internal fields. The mapping is one line.
- *"When would you use a class over a record?"* For an entity with identity and mutable lifecycle, like an `Order` aggregate. Records suit immutable values and DTOs.
