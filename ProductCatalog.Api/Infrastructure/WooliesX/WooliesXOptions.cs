using System;
using System.ComponentModel.DataAnnotations;

namespace ProductCatalog.Api.Infrastructure.WooliesX
{
    public sealed class WooliesXOptions
    {
        public const string SectionName = "WooliesX";

        [Required]
        public Uri? BaseAddress { get; set; }

        /// <summary>Upstream API token. Supply it via user-secrets, an environment variable or a key vault, never appsettings.</summary>
        [Required]
        public string Token { get; set; } = string.Empty;

        /// <summary>How long the product list is cached. Zero disables caching.</summary>
        public TimeSpan ProductsCacheDuration { get; set; } = TimeSpan.FromMinutes(1);

        /// <summary>How long shopper history is cached. Zero disables caching.</summary>
        public TimeSpan ShopperHistoryCacheDuration { get; set; } = TimeSpan.FromMinutes(5);
    }
}
