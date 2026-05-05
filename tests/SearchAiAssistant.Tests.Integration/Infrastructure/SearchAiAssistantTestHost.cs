using System.Net;
using Testcontainers.OpenSearch;
using Testcontainers.PostgreSql;

namespace SearchAiAssistant.Tests.Integration.Infrastructure;

public sealed class SearchAiAssistantIntegrationFixture : IAsyncLifetime
{
    private const string PostgreSqlImage = "postgres:16.2";
    private const string OpenSearchImage = "opensearchproject/opensearch:3.6.0";

    private readonly PostgreSqlContainer _postgresContainer;
    private readonly OpenSearchContainer _openSearchContainer;

    private CustomSearchAiAssistantWebApplicationFactory? _factory;
    private string? _openSearchUri;

    public SearchAiAssistantIntegrationFixture()
    {
        SearchIndexName = $"search-ai-assistant-tests-{Guid.NewGuid():N}";

        _postgresContainer = new PostgreSqlBuilder(PostgreSqlImage)
            .WithDatabase("search_ai_assistant_tests")
            .WithUsername("postgres")
            .WithPassword("postgres")
            .WithCleanUp(true)
            .Build();

        _openSearchContainer = new OpenSearchBuilder(OpenSearchImage)
            .WithSecurityEnabled(false)
            .WithCleanUp(true)
            .Build();
    }

    public string SearchIndexName { get; }

    public IServiceProvider Services =>
        _factory?.Services
        ?? throw new InvalidOperationException("The test factory has not been initialized.");

    public async Task InitializeAsync()
    {
        await _postgresContainer.StartAsync();
        await _openSearchContainer.StartAsync();

        _openSearchUri = _openSearchContainer.GetConnectionString();

        _factory = new CustomSearchAiAssistantWebApplicationFactory(
            postgresConnectionString: _postgresContainer.GetConnectionString(),
            openSearchUri: _openSearchUri,
            searchIndexName: SearchIndexName);

        await IntegrationDatabaseHelper.MigrateAsync(_factory.Services);
        await ResetAsync();
    }

    public HttpClient CreateClient()
    {
        if (_factory is null)
        {
            throw new InvalidOperationException("The test factory has not been initialized.");
        }

        return _factory.CreateClient();
    }

    public async Task ResetAsync()
    {
        if (_factory is null)
        {
            throw new InvalidOperationException("The test factory has not been initialized.");
        }

        await IntegrationDatabaseHelper.ResetAsync(_factory.Services);
        await DeleteOpenSearchIndexIfExistsAsync();
    }

    public async Task DisposeAsync()
    {
        _factory?.Dispose();

        await _openSearchContainer.DisposeAsync();
        await _postgresContainer.DisposeAsync();
    }

    private async Task DeleteOpenSearchIndexIfExistsAsync()
    {
        if (string.IsNullOrWhiteSpace(_openSearchUri))
        {
            throw new InvalidOperationException("OpenSearch URI has not been initialized.");
        }

        using var httpClient = new HttpClient
        {
            BaseAddress = new Uri(_openSearchUri.TrimEnd('/') + "/"),
            Timeout = TimeSpan.FromSeconds(30)
        };

        using var response = await httpClient.DeleteAsync(
            Uri.EscapeDataString(SearchIndexName));

        if (response.IsSuccessStatusCode || response.StatusCode == HttpStatusCode.NotFound)
        {
            return;
        }

        var responseBody = await response.Content.ReadAsStringAsync();

        throw new InvalidOperationException(
            $"Could not delete OpenSearch test index '{SearchIndexName}'. Status code: {(int)response.StatusCode}. Response: {responseBody}");
    }
}