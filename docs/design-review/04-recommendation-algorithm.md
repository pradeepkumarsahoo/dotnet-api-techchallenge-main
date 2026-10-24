# 4. Recommendation algorithm throws on duplicate names and returns nulls — Critical

## Problem

```csharp
// before
from shoppingHistory in shopperHistory
from order in shoppingHistory.Products
group order by order.Name into ordersGroupedByName
let p = new {
    NumberOfOrders = ordersGroupedByName.Sum(product => product.Quantity),
    Product = products.SingleOrDefault(product => product.Name == ordersGroupedByName.Key)   // (1)(2)(3)
}
orderby p.NumberOfOrders descending
select p.Product;
...
var productsThatWereNotOrdered = products.Except(orderedProducts);                        // (4)
```

1. **`SingleOrDefault` throws** `InvalidOperationException` if the catalog has two products with the same name. That's a 500 for the whole endpoint.
2. **It returns `null`** for any ordered item that's no longer in the catalog (discontinued). The response then contains `null` entries.
3. **O(n × m)**: a linear scan of the catalog for every distinct ordered name.
4. **`Except` relies on value equality** of `Product` (name + price + quantity), using a hand-rolled `ValueObject`. It also *deduplicates*, so two identical catalog rows would collapse into one.
5. The products and history calls ran **sequentially**, even though they are independent.

## Fix

The ranking is a pure domain function, [PopularityRanking.cs](../../ProductCatalog.Api/Domain/Products/PopularityRanking.cs):

```csharp
var unitsOrderedByName = histories
    .SelectMany(history => history.Products ?? Enumerable.Empty<OrderedProduct>())
    .GroupBy(line => line.Name, StringComparer.Ordinal)
    .ToDictionary(g => g.Key, g => g.Sum(line => line.Quantity), StringComparer.Ordinal);

return products
    .OrderByDescending(product => unitsOrderedByName.GetValueOrDefault(product.Name))
    .ToList();
```

- **It ranks the catalog, not the history.** Every catalog product appears exactly once, there are no nulls, and history items not in the catalog are ignored.
- **Duplicate catalog names don't throw.** Both rows get the same popularity.
- **O(n + m)** with a dictionary lookup.
- **Deterministic ties.** `OrderByDescending` is a *stable* sort, so equal-popularity products keep catalog order, and never-ordered products stay in catalog order at the end.
- **Products and history are fetched concurrently** with `Task.WhenAll` ([GetSortedProductsQuery.cs](../../ProductCatalog.Api/Application/Products/GetSortedProductsQuery.cs)). Latency becomes max(a, b) instead of a + b.
- Edge cases have dedicated tests: duplicates, discontinued items, empty history, and "units, not order count". See [PopularityRankingTests.cs](../../ProductCatalog.Tests/PopularityRankingTests.cs).

## Trade-offs / product questions to raise

- **What does "popular" mean?** I kept the original definition: **total units ordered**. Alternatives include number of distinct customers (resists one bulk buyer skewing results), number of orders, or recency-weighted. Each is a one-line change. This is a product decision, not a coding one.
- **Match on name** because that's all the upstream gives us. A real system would join on a product ID/SKU.
- **Shopper history grows without bound.** At scale you'd precompute popularity offline or in a stream, not on every request. Caching it ([11-caching.md](11-caching.md)) is the cheap first step.

## What to say in the interview

**30-second answer:**
> "The recommended sort had real bugs. `SingleOrDefault` throws if two catalog products share a name, and it returns null for anything ordered but no longer in the catalog. I rewrote it as a pure function: build a dictionary of units ordered by name, then sort the catalog by it. That's O(n + m), it can't produce nulls, and the stable sort makes ties deterministic. I also made the two upstream calls concurrent, and wrote tests for each edge case."

**Likely follow-ups:**
- *"Why is it in Domain and not the handler?"* It's pure business logic with no I/O. It's trivially unit-testable and reusable.
- *"Is `OrderBy` really stable?"* Yes, LINQ-to-Objects `OrderBy`/`OrderByDescending` are documented as stable. `List.Sort` is **not**.
- *"Why `Task.WhenAll` and not just awaiting both?"* Starting both tasks before awaiting is what gives the concurrency. `WhenAll` makes the intent explicit and surfaces the first failure once both complete.
