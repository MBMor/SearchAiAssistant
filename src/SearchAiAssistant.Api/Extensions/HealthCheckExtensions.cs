using System.Text.Json;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using SearchAiAssistant.Infrastructure.HealthChecks;

namespace SearchAiAssistant.Api.Extensions;

public static class HealthCheckExtensions
{
    private static readonly JsonSerializerOptions JsonSerializerOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true
    };

    public static IServiceCollection AddApplicationHealthChecks(
        this IServiceCollection services,
        string postgresConnectionString)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(postgresConnectionString);

        services
            .AddHealthChecks()
            .AddCheck("api", () => HealthCheckResult.Healthy("API is running."))
            .AddCheck(
                "postgresql",
                new PostgreSqlHealthCheck(postgresConnectionString),
                failureStatus: HealthStatus.Unhealthy,
                tags: ["database", "postgresql"])
            .AddCheck<OpenSearchHealthCheck>(
                "opensearch",
                failureStatus: HealthStatus.Unhealthy,
                tags: ["search", "opensearch"]);

        return services;
    }

    public static WebApplication MapApplicationHealthChecks(this WebApplication app)
    {
        app.MapHealthChecks("/health", new HealthCheckOptions
        {
            ResponseWriter = WriteHealthCheckResponseAsync
        })
        .AllowAnonymous();

        return app;
    }

    private static async Task WriteHealthCheckResponseAsync(
        HttpContext context,
        HealthReport report)
    {
        context.Response.ContentType = "application/json";

        var response = new
        {
            status = report.Status.ToString(),
            totalDurationMilliseconds = report.TotalDuration.TotalMilliseconds,
            checks = report.Entries.Select(entry => new
            {
                name = entry.Key,
                status = entry.Value.Status.ToString(),
                description = entry.Value.Description,
                durationMilliseconds = entry.Value.Duration.TotalMilliseconds,
                error = entry.Value.Exception?.Message,
                data = entry.Value.Data
            })
        };

        await context.Response.WriteAsync(
            JsonSerializer.Serialize(response, JsonSerializerOptions));
    }
}