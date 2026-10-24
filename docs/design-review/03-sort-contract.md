# 3. String-typed sort contract that fails silently with HTTP 200 — Major

## Problem

```csharp
// before
[Route("/products")]
public class ProductController : ControllerBase
{
    [HttpGet("/sort")]                       // leading "/" ignores the controller route → real URL is /sort
    public async Task<...> Sort([FromQuery] string sortOption)
...
if (getSortedProductQuery.SortOption == "Low") ...      // case-sensitive magic strings
else if (...)
else
    return new GetSortedProductQueryResponse(productList); // unknown → unsorted, HTTP 200
```

- `?sortOption=low`, a typo, or a missing parameter returned **200 with unsorted data**. The caller never learns they're wrong.
- The valid values exist only as magic strings deep in the handler. The compiler can't help, and every new option means editing an `if/else` chain.
- **The routes were misleading.** `[Route("/products")]` plus `[HttpGet("/sort")]` maps to `/sort`, not `/products/sort`.
- **No `[ApiController]`**, so no automatic 400s and no `ProblemDetails`.

## Fix

- A `SortOption` **enum** plus `SortOptions.TryParse`: case-insensitive, trimmed, name-only. See [SortOption.cs](../../ProductCatalog.Api/Domain/Products/SortOption.cs).
- **Invalid or missing input → 400 `ValidationProblemDetails`** listing the allowed values. See [ProductsController.cs](../../ProductCatalog.Api/Controllers/ProductsController.cs).
  ```json
  { "status": 400, "errors": { "sortOption": ["Must be one of: Low, High, Ascending, Descending, Recommended."] } }
  ```
- The handler uses a **`switch` expression** over the enum. The `default` branch throws `ArgumentOutOfRangeException`, so a new enum member can't silently fall through. See [GetSortedProductsQuery.cs](../../ProductCatalog.Api/Application/Products/GetSortedProductsQuery.cs).
- **Routes:** the canonical route is `GET /products/sort`. The original `GET /sort` is kept as a second route so existing consumers don't break. `[ApiController]` is on every controller.
- Name sorting uses `StringComparer.OrdinalIgnoreCase`, so the result doesn't depend on the server's culture.

### Why not bind straight to the enum?

ASP.NET Core binds `?sortOption=low` to an enum case-insensitively, but `Enum.Parse` also accepts **`"1"`** (any number, even undefined ones) and **`"Low,High"`** (a flags combination). With these values `Low|High == 3 == Ascending`, so `"Low,High"` would quietly become `Ascending`. A name-only lookup avoids that. Both cases are covered in [SortOptionsTests.cs](../../ProductCatalog.Tests/SortOptionsTests.cs).

## Trade-offs

- Rejecting unknown values with 400 is a **behaviour change**. A client that relied on "unknown means unsorted" will now get errors. In a real system I'd check the logs for invalid values before shipping, and announce the change.
- With five options a `switch` is the simplest correct thing. If options multiplied, or became pluggable, I'd move to a strategy dictionary (`IReadOnlyDictionary<SortOption, ISortStrategy>`) to keep the Open/Closed Principle.

## What to say in the interview

**30-second answer:**
> "The sort endpoint took a free-form string and compared it case-sensitively, so `low` or a typo returned 200 with unsorted data. That's the worst kind of failure: silent. I made it an enum with a strict, case-insensitive parser and return a 400 ProblemDetails listing the valid values. I also fixed the route, which was really `/sort` because of a leading slash, and kept `/sort` as an alias so nothing breaks."

**Likely follow-ups:**
- *"400 or default to unsorted?"* Fail loudly. A default hides client bugs. If product wanted a default, it should be explicit and documented (e.g. `Ascending`), not an accident.
- *"How do you version a breaking change like this?"* Use URL or header versioning (`Asp.Versioning.Mvc`), keep v1 behaviour for a deprecation window, and send a `Sunset` header.
- *"Why 400 and not 422?"* 400 is the conventional status for malformed query parameters. 422 fits a well-formed body that fails business rules.
