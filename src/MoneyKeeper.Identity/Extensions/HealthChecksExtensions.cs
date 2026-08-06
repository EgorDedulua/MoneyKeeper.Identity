using HealthChecks.UI.Client;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using MoneyKeeper.Identity.Infrastructure.Data;

namespace MoneyKeeper.Identity.Extensions
{
    public static class HealthChecksExtensions
    {
        public static IServiceCollection AddAppHealthChecks(this IServiceCollection services)
        {
            services.AddHealthChecks()
                .AddDbContextCheck<IdentityDbContext>("database", tags: new[] { "ready" })
                .AddCheck("self", () => HealthCheckResult.Healthy(), new[] { "live" });

            return services;
        }

        public static WebApplication MapAppHealthChecks(this WebApplication app)
        {
            app.MapHealthChecks("health/live", new HealthCheckOptions
            {
                Predicate = check => check.Tags.Contains("live"),
                ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
            });

            app.MapHealthChecks("health/ready", new HealthCheckOptions
            {
                Predicate = check => check.Tags.Contains("ready"),
                ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
            });

            return app;
        }
    }
}
