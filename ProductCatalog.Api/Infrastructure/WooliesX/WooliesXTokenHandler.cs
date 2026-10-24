using Microsoft.Extensions.Options;
using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace ProductCatalog.Api.Infrastructure.WooliesX
{
    /// <summary>
    /// Adds the WooliesX token to every outgoing request, so the secret lives in one place.
    /// Registered outside the resilience handler so retries reuse the already-signed request instead of appending the token twice.
    /// </summary>
    public sealed class WooliesXTokenHandler : DelegatingHandler
    {
        private readonly IOptionsMonitor<WooliesXOptions> _options;

        public WooliesXTokenHandler(IOptionsMonitor<WooliesXOptions> options)
        {
            _options = options;
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var uri = request.RequestUri ?? throw new InvalidOperationException("Request URI is required.");
            var builder = new UriBuilder(uri);
            var tokenParameter = "token=" + Uri.EscapeDataString(_options.CurrentValue.Token);
            builder.Query = string.IsNullOrEmpty(builder.Query) ? tokenParameter : builder.Query.TrimStart('?') + "&" + tokenParameter;
            request.RequestUri = builder.Uri;

            return base.SendAsync(request, cancellationToken);
        }
    }
}
