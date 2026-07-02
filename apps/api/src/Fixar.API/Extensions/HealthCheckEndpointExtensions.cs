using System.Text.Json;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Fixar.API.Extensions;

public static class HealthCheckEndpointExtensions
{
    public static IServiceCollection AddApiHealthChecks(this IServiceCollection services)
    {
        services.AddHealthChecks()
            .AddCheck("self", () => HealthCheckResult.Healthy(), tags: new[] { "live" });

        return services;
    }

    /// <summary>
    /// Maps /health/live (process is up), /health/ready (dependencies such
    /// as PostgreSQL are reachable) and /health (everything).
    /// </summary>
    public static WebApplication MapHealthCheckEndpoints(this WebApplication app)
    {
        var jsonWriter = static async (HttpContext context, HealthReport report) =>
        {
            context.Response.ContentType = "application/json";

            var payload = new
            {
                status = report.Status.ToString(),
                totalDurationMs = report.TotalDuration.TotalMilliseconds,
                checks = report.Entries.Select(e => new
                {
                    name = e.Key,
                    status = e.Value.Status.ToString(),
                    description = e.Value.Description,
                    durationMs = e.Value.Duration.TotalMilliseconds
                })
            };

            await context.Response.WriteAsync(JsonSerializer.Serialize(payload));
        };

        app.MapHealthChecks("/health/live", new HealthCheckOptions
        {
            Predicate = check => check.Tags.Contains("live"),
            ResponseWriter = jsonWriter
        });

        app.MapHealthChecks("/health/ready", new HealthCheckOptions
        {
            Predicate = check => check.Tags.Contains("ready"),
            ResponseWriter = jsonWriter
        });

        app.MapHealthChecks("/health", new HealthCheckOptions
        {
            ResponseWriter = jsonWriter
        });

        return app;
    }
}
