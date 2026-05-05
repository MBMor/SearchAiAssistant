using System.Net;
using System.Text.Json;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using SearchAiAssistant.Infrastructure.Search.OpenSearch;

namespace SearchAiAssistant.Infrastructure.HealthChecks;

public sealed class OpenSearchHealthCheck(
    HttpClient httpClient,
    IOptions<OpenSearchOptions> openSearchOptions) : IHealthCheck
{
    private readonly HttpClient _httpClient = httpClient;
    private readonly OpenSearchOptions _openSearchOptions = openSearchOptions.Value;

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            using var response = await _httpClient.GetAsync(
                "_cluster/health",
                cancellationToken);

            var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                return HealthCheckResult.Unhealthy(
                    $"OpenSearch health endpoint returned HTTP {(int)response.StatusCode}.");
            }

            var status = ReadClusterStatus(responseBody);

            var data = new Dictionary<string, object>
            {
                ["clusterStatus"] = status ?? "unknown",
                ["indexName"] = _openSearchOptions.IndexName
            };

            return status switch
            {
                "green" => HealthCheckResult.Healthy("OpenSearch cluster is healthy.", data: data),
                "yellow" => HealthCheckResult.Degraded("OpenSearch cluster is available but degraded.", data: data),
                "red" => HealthCheckResult.Unhealthy("OpenSearch cluster is unhealthy.", data: data),
                _ => HealthCheckResult.Degraded("OpenSearch cluster returned an unknown status.", data: data)
            };
        }
        catch (Exception exception)
        {
            return HealthCheckResult.Unhealthy(
                "OpenSearch is not reachable.",
                exception);
        }
    }

    private static string? ReadClusterStatus(string responseBody)
    {
        if (string.IsNullOrWhiteSpace(responseBody))
        {
            return null;
        }

        using var jsonDocument = JsonDocument.Parse(responseBody);

        if (!jsonDocument.RootElement.TryGetProperty("status", out var statusElement))
        {
            return null;
        }

        return statusElement.GetString()?.Trim().ToLowerInvariant();
    }
}