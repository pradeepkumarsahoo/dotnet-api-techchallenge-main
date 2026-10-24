using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using System;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Threading.RateLimiting;
using System.Threading.Tasks;

namespace ProductCatalog.Api.RateLimiting
{
    public sealed class RateLimitingOptions
    {
        public const string SectionName = "RateLimiting";

        [Range(1, int.MaxValue)]
        public int PermitLimit { get; set; } = 60;

        public TimeSpan Window { get; set; } = TimeSpan.FromMinutes(1);
    }

    public static class RateLimitingServiceCollectionExtensions
    {
        /// <summary>
        /// Fixed-window limit per authenticated client (or per IP address for anonymous callers).
        /// Runs after authentication so the partition key is the client id rather than a shared proxy IP.
        /// </summary>
        public static IServiceCollection AddClientRateLimiting(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddOptions<RateLimitingOptions>()
                .Bind(configuration.GetSection(RateLimitingOptions.SectionName))
                .ValidateDataAnnotations()
                .ValidateOnStart();

            services.AddRateLimiter(options =>
            {
                options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
                options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
                {
                    var settings = context.RequestServices.GetRequiredService<IOptions<RateLimitingOptions>>().Value;
                    return RateLimitPartition.GetFixedWindowLimiter(PartitionKey(context), _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = settings.PermitLimit,
                        Window = settings.Window,
                        QueueLimit = 0
                    });
                });
                options.OnRejected = (context, _) =>
                {
                    if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter))
                        context.HttpContext.Response.Headers.RetryAfter = ((int)retryAfter.TotalSeconds).ToString(CultureInfo.InvariantCulture);
                    return ValueTask.CompletedTask;
                };
            });

            return services;
        }

        private static string PartitionKey(HttpContext context) =>
            context.User.Identity is { IsAuthenticated: true, Name: { } clientId }
                ? $"client:{clientId}"
                : $"ip:{context.Connection.RemoteIpAddress}";
    }
}
