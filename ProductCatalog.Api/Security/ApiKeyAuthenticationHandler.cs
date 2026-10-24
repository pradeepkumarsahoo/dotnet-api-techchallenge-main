using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Encodings.Web;
using System.Threading.Tasks;

namespace ProductCatalog.Api.Security
{
    public static class ApiKeyDefaults
    {
        public const string AuthenticationScheme = "ApiKey";
    }

    public sealed class ApiKeyAuthenticationOptions : AuthenticationSchemeOptions
    {
        public string HeaderName { get; set; } = "X-Api-Key";

        /// <summary>Known callers. Keys are supplied via user-secrets, environment variables or a key vault.</summary>
        public List<ApiKeyClient> Clients { get; set; } = [];
    }

    public sealed class ApiKeyClient
    {
        public string ClientId { get; set; } = string.Empty;

        public string Key { get; set; } = string.Empty;
    }

    /// <summary>
    /// Authenticates callers by a shared key in a request header. Suitable for service-to-service calls;
    /// user-facing clients should use OAuth 2.0 / OpenID Connect bearer tokens instead.
    /// </summary>
    public sealed class ApiKeyAuthenticationHandler : AuthenticationHandler<ApiKeyAuthenticationOptions>
    {
        public ApiKeyAuthenticationHandler(IOptionsMonitor<ApiKeyAuthenticationOptions> options, ILoggerFactory logger, UrlEncoder encoder)
            : base(options, logger, encoder)
        {
        }

        protected override Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            var presentedKey = Request.Headers[Options.HeaderName].ToString();
            if (string.IsNullOrEmpty(presentedKey))
                return Task.FromResult(AuthenticateResult.NoResult());

            var client = Options.Clients.FirstOrDefault(candidate => KeysMatch(presentedKey, candidate.Key));
            if (client is null)
                return Task.FromResult(AuthenticateResult.Fail("Invalid API key."));

            var identity = new ClaimsIdentity(
                [new Claim(ClaimTypes.NameIdentifier, client.ClientId), new Claim(ClaimTypes.Name, client.ClientId)],
                Scheme.Name);
            return Task.FromResult(AuthenticateResult.Success(new AuthenticationTicket(new ClaimsPrincipal(identity), Scheme.Name)));
        }

        // Compare fixed-length hashes in constant time so response timing does not reveal how much of a key matched.
        private static bool KeysMatch(string presented, string expected) =>
            !string.IsNullOrEmpty(expected) &&
            CryptographicOperations.FixedTimeEquals(
                SHA256.HashData(Encoding.UTF8.GetBytes(presented)),
                SHA256.HashData(Encoding.UTF8.GetBytes(expected)));
    }
}
