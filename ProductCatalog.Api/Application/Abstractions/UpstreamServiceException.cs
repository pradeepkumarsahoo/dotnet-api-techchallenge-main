using System;
using System.Net;

namespace ProductCatalog.Api.Application.Abstractions
{
    /// <summary>
    /// An upstream dependency failed or returned something unusable.
    /// The message names the resource only; it never contains the request URL or its query string.
    /// </summary>
    public sealed class UpstreamServiceException : Exception
    {
        public UpstreamServiceException(string resource, HttpStatusCode statusCode)
            : base($"Upstream resource '{resource}' returned {(int)statusCode} ({statusCode}).")
        {
            Resource = resource;
            StatusCode = statusCode;
        }

        public UpstreamServiceException(string resource, string reason, Exception? innerException = null)
            : base($"Upstream resource '{resource}' failed: {reason}.", innerException)
        {
            Resource = resource;
        }

        public string Resource { get; }

        public HttpStatusCode? StatusCode { get; }
    }
}
