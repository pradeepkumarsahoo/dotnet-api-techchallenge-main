# 9. Layering and namespace problems — Major

## Problem

- **HTTP clients (infrastructure) lived in the `Domain.HttpClients` namespace.** The domain depended on Flurl, URLs and tokens.
- **Interface and implementation in one file**, named after the interface (`IProductHttpClient.cs` contained `ProductHttpClient` too).
- **Port names leaked the transport.** `IProductHttpClient` tells the use case *how* products are fetched. Its job is to say *what* it needs.
- **A namespace and a type with the same name**, `Domain.Product.Product`, forced awkward `Product.Product` and `Domain.Product.Product` references.
- **`WooliesXProxy`** sat at the project root, outside any layer.
- **The use case (`GetSortedProductQueryHandler`) lived inside `Domain`**, mixed with entities.

## Fix

Inside one project, folders and namespaces now enforce a clean (ports-and-adapters) dependency direction: **Web → Application → Domain ← Infrastructure**.

```
ProductCatalog.Api/
├── Domain/                     pure C#: no ASP.NET, no HTTP, no JSON attributes
│   ├── Products/               Product, OrderedProduct, ShopperHistory, SortOption, PopularityRanking
│   └── Trolleys/               Trolley records
├── Application/
│   ├── Abstractions/           ports: IProductCatalogClient, IShopperHistoryClient, ITrolleyCalculatorClient, UpstreamServiceException
│   └── Products/               GetSortedProductsQuery + handler (use case)
├── Infrastructure/WooliesX/    adapters: typed HttpClients, token handler, caching decorators, redacting logger, DI wiring
├── Contracts/                  public request/response DTOs
├── Controllers/                thin HTTP layer: parse → call use case → map
├── Security/                   API-key authentication
├── RateLimiting/               rate limiter registration
├── ErrorHandling/              global exception → ProblemDetails
└── Startup.cs                  composition root
```

- **Ports are named by capability** (`IProductCatalogClient`) and live in Application. Adapters implement them in Infrastructure. That's the Dependency Inversion Principle: the use case owns the interface it needs.
- **One public type per file**, file named after the type. The small exceptions are tightly related records or options grouped together, such as `Trolley.cs` and `CachingClients.cs`.
- `Domain.Products` (plural namespace) avoids the `Product.Product` clash.
- Each feature registers itself through an extension method (`AddWooliesXClients`, `AddClientRateLimiting`), so `Startup` reads like a table of contents. See [Startup.cs](../../ProductCatalog.Api/Startup.cs).

## Trade-offs

- **One project vs. four** (`Domain`, `Application`, `Infrastructure`, `Api`). Separate projects let the *compiler* enforce the dependency direction. For a service this size, folders and namespaces are enough. When the team or the codebase grows, I'd split the projects, or add an architecture test (NetArchTest/ArchUnitNET) that fails the build if `Domain` references `Infrastructure`.
- **Vertical slices** (a `Features/Products/Sort/` folder holding controller, handler and DTO together) are an equally valid alternative. They optimise for change locality over layer purity.
- I kept the `Startup` class instead of migrating to minimal-hosting `Program.cs`. It still works fine on .NET 8, and the test factory targets it. Migrating is a mechanical follow-up.

## What to say in the interview

**30-second answer:**
> "The 'Domain' namespace contained HTTP clients with URLs and tokens, so the dependency arrows pointed the wrong way. I reorganised into Domain, Application, Infrastructure and a thin web layer. The use case owns its ports, named by capability rather than transport, and the WooliesX adapters implement them. The domain has no framework dependencies at all, which is why the ranking logic is trivially unit-testable. With a bigger team I'd split into projects so the compiler enforces it."

**Likely follow-ups:**
- *"Isn't this over-engineering for a small API?"* The folders cost nothing. The real value is that the domain is pure and the infrastructure is swappable, which already paid off with caching decorators and stubbed tests. I deliberately didn't add separate projects, MediatR or a repository layer.
- *"Clean Architecture vs. vertical slices?"* Layers make dependency rules obvious. Slices make features easy to change and delete. Many teams use slices *on top of* a pure domain core.
