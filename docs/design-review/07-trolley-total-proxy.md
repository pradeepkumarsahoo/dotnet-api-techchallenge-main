# 7. Static `/trolleyTotal` pass-through for all HTTP verbs — Major

## Problem

```csharp
// before — Startup.cs
endpoints.Map("/trolleyTotal", async context => { await WooliesXProxy.TrolleyCalculator(context); });

// WooliesXProxy.cs
public static async Task TrolleyCalculator(HttpContext context)
{
    var postRequestContent = await new StreamReader(context.Request.Body).ReadToEndAsync();
    var response = await proxyUrl.SetQueryParam("token", "25a4…")
        .PostAsync(new StringContent(postRequestContent));
    await context.Response.WriteAsync(await response.Content.ReadAsStringAsync());
}
```

- **`endpoints.Map` matches *every* verb.** `GET`, `DELETE` or `PUT /trolleyTotal` all forwarded an empty POST upstream.
- **No validation.** Any body, including empty or non-JSON, went upstream. We spent an upstream call to find out it was bad.
- **Upstream errors were swallowed.** If WooliesX returned 400 or 500, we copied the body back **with status 200**. No content type was set either.
- **A static class with a hard-coded URL and token** can't be injected, configured or tested. It also bypasses everything a controller gets: model binding, validation, auth, ProblemDetails, OpenAPI.
- **Reads the whole body into a string** with no size limit.

## Fix

- A normal `[ApiController]` action, **`POST` only**, with a typed request model and data-annotation validation (required lists, non-negative prices and quantities). See [TrolleyController.cs](../../ProductCatalog.Api/Controllers/TrolleyController.cs) and [TrolleyRequest.cs](../../ProductCatalog.Api/Contracts/TrolleyRequest.cs).
- The contract maps to **domain records** (`Trolley`, `TrolleyProduct`, `TrolleySpecial`, `TrolleyQuantity`), which are sent through an injected `ITrolleyCalculatorClient` typed client. That client has the same token handler, resilience and error mapping as the others. See [Trolley.cs](../../ProductCatalog.Api/Domain/Trolleys/Trolley.cs) and [TrolleyCalculatorClient.cs](../../ProductCatalog.Api/Infrastructure/WooliesX/TrolleyCalculatorClient.cs).
- Responses: **200** with the total as a JSON number, **400** ProblemDetails for invalid input (upstream never called), **405** for other verbs, and **502/503/504** for upstream failures.
- The total is a `decimal` end to end. `decimal` preserves scale, so `150.0` from upstream is still `150.0` on our wire.
- Tests: [TrolleyEndpointTests.cs](../../ProductCatalog.Tests/Integration/TrolleyEndpointTests.cs).

## Trade-offs

- **The behaviour changed:** non-POST verbs now get 405, and a non-JSON content type gets 415. Both are correct HTTP, but they could break a sloppy client.
- **A deliberate design choice: proxy vs. own the logic.** I kept delegating to the upstream calculator because it's the source of truth for pricing and specials. If we owned pricing, I'd implement the "cheapest combination of specials" calculation in the domain. It's a bounded knapsack, a good DP or search question. That would remove a network hop.
- If you genuinely need a transparent reverse proxy, use **YARP**, not a hand-rolled stream copy.

## What to say in the interview

**30-second answer:**
> "`/trolleyTotal` was a static pass-through mapped for every HTTP verb, with no validation, and it returned 200 even when the upstream failed. I made it a proper POST action with a typed, validated request model and an injected typed client, so it gets auth, resilience, ProblemDetails and tests like everything else. Bad input is rejected before we spend an upstream call."

**Likely follow-ups:**
- *"Why not keep a raw passthrough? It's less code."* It's less code but more risk: unvalidated input, wrong status codes, no auth, no tests. If we need a real proxy, YARP does it properly.
- *"How would you compute the trolley total yourself?"* Price everything individually, then search the combinations of specials that fit inside the quantities and keep the minimum. Memoise on remaining quantities (DP over a multiset). Prune specials that cost more than buying the items individually.
