# 1. Hard-coded upstream token, returned by an anonymous `/user` endpoint — Critical

## Problem

The WooliesX token `25a4f06f-…` was a string literal in **four** places: `UserController`, `ProductHttpClient`,
`ShopperHistoryHttpClient` and `WooliesXProxy`.

```csharp
// before — repeated in four files
.SetQueryParam("token", "25a4f06f-8fd5-49b3-a711-c013c156f8c8")
```

`GET /user` had no authentication and returned that token to anyone who called it.

## Why it matters

- **The secret is in source control.** Anyone with repo access (or a leaked clone) has it, and it stays in git history even after deletion.
- **It can't be rotated without a rebuild and redeploy**, and it can't differ between dev, test and prod.
- **The four copies can drift apart**: rotate three, forget one, and you get a partial outage.
- **An anonymous endpoint hands the credential to anyone** who can reach the API.

## Fix

| What | Where |
|---|---|
| A single strongly-typed options class with `[Required] Token`. It is validated **at startup** (`ValidateOnStart`), so a missing token fails the deploy instead of failing the first request | [WooliesXOptions.cs](../../ProductCatalog.Api/Infrastructure/WooliesX/WooliesXOptions.cs), [WooliesXServiceCollectionExtensions.cs](../../ProductCatalog.Api/Infrastructure/WooliesX/WooliesXServiceCollectionExtensions.cs) |
| `appsettings.json` holds an empty `Token`. The real value comes from **user-secrets** (dev) or **environment variables / Key Vault** (deployed) | [appsettings.json](../../ProductCatalog.Api/appsettings.json) |
| The token is attached in exactly one place, a `DelegatingHandler` on every WooliesX `HttpClient` | [WooliesXTokenHandler.cs](../../ProductCatalog.Api/Infrastructure/WooliesX/WooliesXTokenHandler.cs) |
| `/user` reads the name and token from configuration and is protected by `[Authorize]` (see [13-authentication.md](13-authentication.md)) | [UserController.cs](../../ProductCatalog.Api/Controllers/UserController.cs) |
| A test proves the app refuses to start without a token | [CompositionTests.cs](../../ProductCatalog.Tests/Integration/CompositionTests.cs) |

```bash
dotnet user-secrets set "WooliesX:Token" "<token>" --project ProductCatalog.Api
# deployed: environment variable  WooliesX__Token=<token>   (or Azure Key Vault config provider)
```

## Trade-offs and what is still open

- **The old token is still in git history.** Removing it from code is not enough: **rotate it**. Rewriting history (`git filter-repo`) is optional once the token has been rotated.
- `/user` still returns the token, because the challenge contract requires it. It is now behind authentication. In a real system I would question why an API returns a credential at all.
- The upstream only accepts the token as a **query-string parameter**, so it ends up in URLs. Problem 6 covers keeping it out of logs. If the upstream were ours, I'd move it to an `Authorization` header.

## What to say in the interview

**30-second answer:**
> "The most serious issue was the upstream token committed in four places and handed out by an anonymous endpoint. I moved it into one validated options class sourced from user-secrets or Key Vault, attached it in a single `DelegatingHandler`, and made the app fail at startup if it's missing. I'd also rotate the token, because it's in git history forever. Deleting it from code doesn't un-leak it."

**Likely follow-ups:**
- *"Why not just put it in appsettings.Production.json?"* That file is still in source control. Config files hold non-secret settings; secrets go in a secret store (Key Vault, AWS Secrets Manager), or in environment variables injected by the platform.
- *"Why validate at startup?"* Fail fast. A bad deploy should fail its health check and roll back, not serve 500s.
- *"How would you rotate without downtime?"* Use `IOptionsMonitor` (the handler already does) plus Key Vault reload. Better still, run old and new tokens in parallel if the upstream supports it.
- *"Why a DelegatingHandler and not a parameter on each call?"* It's a cross-cutting concern. One place to change, and clients can't forget it.
