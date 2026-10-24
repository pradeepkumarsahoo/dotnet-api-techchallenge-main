using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Polly.CircuitBreaker;
using Polly.Timeout;
using ProductCatalog.Api.Application.Abstractions;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace ProductCatalog.Api.ErrorHandling
{
    /// <summary>
    /// Single place that turns exceptions into RFC 7807 problem responses. Upstream failures map to 502/503/504 so callers
    /// can tell "our bug" from "a dependency is down". Exception details are logged, never returned.
    /// </summary>
    public sealed class GlobalExceptionHandler : IExceptionHandler
    {
        private const int ClientClosedRequest = 499;
        private readonly IProblemDetailsService _problemDetailsService;
        private readonly ILogger<GlobalExceptionHandler> _logger;

        public GlobalExceptionHandler(IProblemDetailsService problemDetailsService, ILogger<GlobalExceptionHandler> logger)
        {
            _problemDetailsService = problemDetailsService;
            _logger = logger;
        }

        public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
        {
            if (exception is OperationCanceledException && httpContext.RequestAborted.IsCancellationRequested)
            {
                _logger.LogInformation("Request was cancelled by the client.");
                httpContext.Response.StatusCode = ClientClosedRequest;
                return true;
            }

            var (status, title) = exception switch
            {
                UpstreamServiceException => (StatusCodes.Status502BadGateway, "An upstream service returned an error."),
                BrokenCircuitException => (StatusCodes.Status503ServiceUnavailable, "An upstream service is temporarily unavailable."),
                TimeoutRejectedException => (StatusCodes.Status504GatewayTimeout, "An upstream service timed out."),
                _ => (StatusCodes.Status500InternalServerError, "An unexpected error occurred.")
            };

            if (status == StatusCodes.Status500InternalServerError)
                _logger.LogError(exception, "Unhandled exception");
            else
                _logger.LogWarning(exception, "Upstream dependency failure ({Status})", status);

            httpContext.Response.StatusCode = status;
            return await _problemDetailsService.TryWriteAsync(new ProblemDetailsContext
            {
                HttpContext = httpContext,
                Exception = exception,
                ProblemDetails = new ProblemDetails { Status = status, Title = title }
            });
        }
    }
}
