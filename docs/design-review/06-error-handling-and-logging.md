# 6. Log-and-throw leaks the token, with no structured error handling — Major

## Problem

```csharp
// before — in each client
catch (Exception e)
{
    Console.WriteLine(e);   // Flurl's exception message contains the full URL — including ?token=…
    throw;
}
```

- **The token leaks into logs.** `FlurlHttpException.Message` includes the request URL, query string and all. Anyone with log access gets the secret.
- **`Console.WriteLine` is unstructured.** It skips `ILogger`, log levels, correlation IDs and sinks (App Insights, Seq…).
- **Log-and-rethrow logs every failure twice**: once here, once wherever it's finally handled. That's noise.
- `catch (Exception)` treats a cancelled request, a 404 and a bug the same way.
- **No global handler.** Unhandled exceptions became an HTML developer page in Development, or a bare 500 elsewhere.
- **A second, less obvious leak:** `IHttpClientFactory`'s built-in logging writes `Sending HTTP request GET https://…?token=…` at **Information** level.

## Fix

- **The clients don't log.** They translate failures into one meaningful exception, `UpstreamServiceException`. Its message names the *resource path only*, never the URL with its query. See [UpstreamServiceException.cs](../../ProductCatalog.Api/Application/Abstractions/UpstreamServiceException.cs) and [WooliesXHttpClientExtensions.cs](../../ProductCatalog.Api/Infrastructure/WooliesX/WooliesXHttpClientExtensions.cs).
- **Logging and the HTTP response are decided in one place.** A .NET 8 `IExceptionHandler` maps exceptions to RFC 7807 `ProblemDetails` (502/503/504/500, or 499 for client disconnects), logs once with `ILogger` structured templates, and never returns exception details to the caller. See [GlobalExceptionHandler.cs](../../ProductCatalog.Api/ErrorHandling/GlobalExceptionHandler.cs).
- **The HttpClientFactory loggers are replaced** with a custom `IHttpClientLogger` that logs method, path, status and duration, with the query string stripped. As a belt-and-braces measure, `System.Net.Http.HttpClient` is set to `Warning` in `appsettings.json`. See [RedactingHttpClientLogger.cs](../../ProductCatalog.Api/Infrastructure/WooliesX/RedactingHttpClientLogger.cs).
- `UseStatusCodePages()` plus `AddProblemDetails()` give 401, 404 and 405 the same JSON shape.
- A test asserts that the 502 body contains neither the token nor the upstream path. See [ProductsEndpointTests.cs](../../ProductCatalog.Tests/Integration/ProductsEndpointTests.cs).

```json
// GET /products/sort?sortOption=Low while upstream returns 500
HTTP/1.1 502 Bad Gateway
Content-Type: application/problem+json
{ "type": "https://tools.ietf.org/html/rfc9110#section-15.6.3", "title": "An upstream service returned an error.", "status": 502, "traceId": "00-…" }
```

## Trade-offs

- Exception details are hidden in every environment. Developers use the logs, correlated by `traceId`. You could expose details in Development via `ProblemDetailsOptions.CustomizeProblemDetails`. I chose consistency.
- For defence in depth in production, I'd add `Microsoft.Extensions.Http.Diagnostics` (`AddExtendedHttpClientLogging` with data classification and redaction) and a log-pipeline scrubber for anything that looks like a token.

## What to say in the interview

**30-second answer:**
> "The clients caught every exception, wrote it to the console and rethrew. Flurl's exception message contains the URL, which contains the token, so we were writing the secret to logs. I removed logging from the clients. They now throw one domain exception that never includes the URL. A single .NET 8 `IExceptionHandler` decides the status code, logs once with structured logging, and returns ProblemDetails. I also found HttpClientFactory's default logging writes the full URI, so I replaced it with a redacting logger."

**Likely follow-ups:**
- *"Why is log-and-throw an anti-pattern?"* The same failure gets logged at several layers, and the layer that logs has no context on whether it was handled. Log where you *handle* the exception.
- *"Exception middleware vs. exception filters?"* Filters only see MVC exceptions. Middleware or `IExceptionHandler` also catches routing, auth and minimal-API failures.
- *"How do you correlate a 502 with logs?"* Each response carries a W3C `traceId` that matches the `TraceId` on every log line and on the outgoing `traceparent` header to the upstream.
