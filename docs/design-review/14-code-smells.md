# 14. Smaller code smells fixed along the way

These items aren't headline design problems, but they're worth mentioning briefly if the interviewer asks "what else did you notice?"

| Smell | Before | After |
|---|---|---|
| Missing `[ApiController]` | No automatic 400 on binding errors, no ProblemDetails | On every controller |
| Untyped results | `IActionResult` | `ActionResult<T>` with `[ProducesResponseType]`, which documents the contract and helps OpenAPI |
| Dead `[JsonProperty]` attributes | Newtonsoft attributes, but the app serialises with System.Text.Json | Removed. Newtonsoft dropped from the API project |
| Two HTTP stacks | Flurl and `HttpClient` concepts mixed; Flurl 2.4 is old | Only `IHttpClientFactory` typed clients |
| Interface and class in one file, misnamed | `IProductHttpClient.cs` holds `ProductHttpClient` too | One type per file, named after the type |
| `Product.Product` namespace clash | `Domain.Product.Product`, `Product.Product[]` | `Domain.Products.Product` |
| Unused usings and the copy-pasted comment `// This method gets called by the runtime…` inside an action | | Removed |
| Mixed declared/actual types | `GetJsonAsync<Product[]>` returned as `IEnumerable`, then `.ToList()` again | `IReadOnlyList<T>` end to end |
| Culture-sensitive name sort | Default comparer depends on server culture | `StringComparer.OrdinalIgnoreCase`, deterministic |
| `GetHashCode` via unseeded `Aggregate` with XOR | Throws on zero components; XOR collides | Compiler-generated record equality |
| Test data naming typos | `ANotSortedProductsFormLowToHigh` | `ProductData.Unsorted`, `SortedLowToHigh`, … |
| `WebApplicationFactory` not disposed | `new WebApplicationFactory<Startup>().Server.CreateClient()` | `using var factory = new ApiFactory()` |
| Test stubs returning `null` lists | `WithNoHistory()` left `ShopperHistories` null; `AsEnumerable()` on null would throw | Stubs default to empty lists |
| `launchUrl` pointing at an auth-protected route | `sort?sortOption=Low` | `health` (anonymous) |
| No nullable reference types | `string` could be null anywhere | `<Nullable>enable</Nullable>`, with warnings as errors |
| Vulnerable transitive package | `System.Text.Json 8.0.4` in tests (GHSA-8g4q-xg66-9fp4) | Pinned to 8.0.5; CI now scans for this |

## What to say in the interview

> "Beyond the big items there was a tail of hygiene issues: dead Newtonsoft attributes that did nothing because the app uses System.Text.Json, interface and implementation sharing a file, a namespace clashing with its type, culture-sensitive sorting, and undisposed test factories. Individually they're small. Together they're why I added nullable checks, warnings-as-errors and CI, so the next one gets caught by the build instead of a reviewer."
