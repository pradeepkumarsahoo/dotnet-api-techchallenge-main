using Microsoft.Extensions.Http.Logging;
using Microsoft.Extensions.Logging;
using System;
using System.Net.Http;

namespace ProductCatalog.Api.Infrastructure.WooliesX
{
    /// <summary>
    /// Replaces the default HttpClientFactory loggers, which write the full request URI (including the token query
    /// parameter) at Information level. Only the scheme, host and path are logged.
    /// </summary>
    public sealed class RedactingHttpClientLogger : IHttpClientLogger
    {
        private readonly ILogger<RedactingHttpClientLogger> _logger;

        public RedactingHttpClientLogger(ILogger<RedactingHttpClientLogger> logger)
        {
            _logger = logger;
        }

        public object? LogRequestStart(HttpRequestMessage request) => null;

        public void LogRequestStop(object? context, HttpRequestMessage request, HttpResponseMessage response, TimeSpan elapsed) =>
            _logger.LogInformation(
                "Upstream {Method} {Path} responded {StatusCode} in {ElapsedMilliseconds:0}ms",
                request.Method, Redact(request.RequestUri), (int)response.StatusCode, elapsed.TotalMilliseconds);

        public void LogRequestFailed(object? context, HttpRequestMessage request, HttpResponseMessage? response, Exception exception, TimeSpan elapsed) =>
            _logger.LogWarning(
                "Upstream {Method} {Path} failed after {ElapsedMilliseconds:0}ms with {ExceptionType}",
                request.Method, Redact(request.RequestUri), elapsed.TotalMilliseconds, exception.GetType().Name);

        private static string? Redact(Uri? uri) => uri?.GetLeftPart(UriPartial.Path);
    }
}
