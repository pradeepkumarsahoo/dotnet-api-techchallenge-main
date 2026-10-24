using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ProductCatalog.Api.Application.Products;
using ProductCatalog.Api.Controllers;
using ProductCatalog.Api.ErrorHandling;
using ProductCatalog.Api.Infrastructure.WooliesX;
using ProductCatalog.Api.RateLimiting;
using ProductCatalog.Api.Security;

namespace ProductCatalog.Api
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        public void ConfigureServices(IServiceCollection services)
        {
            services.AddControllers();
            services.AddProblemDetails();
            services.AddExceptionHandler<GlobalExceptionHandler>();
            services.AddHealthChecks();

            services.AddOptions<UserOptions>()
                .Bind(Configuration.GetSection(UserOptions.SectionName))
                .ValidateDataAnnotations()
                .ValidateOnStart();

            services.AddScoped<GetSortedProductsQueryHandler>();
            services.AddWooliesXClients(Configuration);

            services.AddAuthentication(ApiKeyDefaults.AuthenticationScheme)
                .AddScheme<ApiKeyAuthenticationOptions, ApiKeyAuthenticationHandler>(
                    ApiKeyDefaults.AuthenticationScheme,
                    options => Configuration.GetSection("Authentication:ApiKey").Bind(options));

            // Secure by default: every endpoint requires an authenticated caller unless it opts out with AllowAnonymous.
            services.AddAuthorization(options =>
                options.FallbackPolicy = new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build());

            services.AddClientRateLimiting(Configuration);
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            app.UseExceptionHandler();
            app.UseStatusCodePages();

            app.UseHttpsRedirection();

            app.UseRouting();

            app.UseAuthentication();
            app.UseRateLimiter();
            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapGet("/", () => "Welcome to Product Catalog API").AllowAnonymous();
                endpoints.MapHealthChecks("/health").AllowAnonymous();
                endpoints.MapControllers();
            });
        }
    }
}
