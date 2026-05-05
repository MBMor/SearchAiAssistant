using System.Net;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SearchAiAssistant.Application.Abstractions.Indexing;
using SearchAiAssistant.Application.Common.Abstractions;
using SearchAiAssistant.Domain.Entities;
using SearchAiAssistant.Infrastructure.Search.OpenSearch.Models;
using DocumentEntity = SearchAiAssistant.Domain.Entities.Document;

namespace SearchAiAssistant.Infrastructure.Search.OpenSearch;

public sealed class OpenSearchIndexingService(
    HttpClient httpClient,
    IOptions<OpenSearchOptions> openSearchOptions,
    IDateTimeProvider dateTimeProvider,
    ILogger<OpenSearchIndexingService> logger) : IIndexingService
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly HttpClient _httpClient = httpClient;
    private readonly OpenSearchOptions _openSearchOptions = openSearchOptions.Value;
    private readonly IDateTimeProvider _dateTimeProvider = dateTimeProvider;
    private readonly ILogger<OpenSearchIndexingService> _logger = logger;

    public async Task RecreateIndexAsync(
    CancellationToken cancellationToken = default)
    {
        await DeleteIndexIfExistsAsync(cancellationToken);

        var indexExists = await EnsureIndexExistsAsync(cancellationToken);

        if (!indexExists)
        {
            throw new InvalidOperationException(
                $"OpenSearch index '{_openSearchOptions.IndexName}' could not be created.");
        }
    }
    public Task IndexEmployeeAsync(
        Employee employee,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(employee);

        return ExecuteSafelyAsync(
            operationName: "index employee",
            operation: async () =>
            {
                var indexDocument = SearchIndexDocumentFactory.FromEmployee(
                    employee,
                    _dateTimeProvider.UtcNow);

                await IndexDocumentAsync(indexDocument, cancellationToken);
            });
    }

    public Task IndexDocumentAsync(
        DocumentEntity document,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(document);

        return ExecuteSafelyAsync(
            operationName: "index document",
            operation: async () =>
            {
                var indexDocument = SearchIndexDocumentFactory.FromDocument(
                    document,
                    _dateTimeProvider.UtcNow);

                await IndexDocumentAsync(indexDocument, cancellationToken);
            });
    }

    public Task RemoveEmployeeAsync(
        Guid employeeId,
        CancellationToken cancellationToken = default)
    {
        var documentId = SearchIndexDocumentFactory.CreateDocumentId(
            SearchIndexDocumentTypes.Employee,
            employeeId);

        return ExecuteSafelyAsync(
            operationName: "remove employee from index",
            operation: () => DeleteDocumentAsync(documentId, cancellationToken));
    }

    public Task RemoveDocumentAsync(
        Guid documentId,
        CancellationToken cancellationToken = default)
    {
        var searchDocumentId = SearchIndexDocumentFactory.CreateDocumentId(
            SearchIndexDocumentTypes.Document,
            documentId);

        return ExecuteSafelyAsync(
            operationName: "remove document from index",
            operation: () => DeleteDocumentAsync(searchDocumentId, cancellationToken));
    }

    private async Task IndexDocumentAsync(
        SearchIndexDocument document,
        CancellationToken cancellationToken)
    {
        var indexExists = await EnsureIndexExistsAsync(cancellationToken);

        if (!indexExists)
        {
            _logger.LogWarning(
                "OpenSearch index '{IndexName}' is not available. Document '{DocumentId}' was not indexed.",
                _openSearchOptions.IndexName,
                document.Id);

            return;
        }

        var json = JsonSerializer.Serialize(document, JsonOptions);

        using var content = new StringContent(json, Encoding.UTF8, "application/json");

        var requestUri =
            $"{Escape(_openSearchOptions.IndexName)}/_doc/{Escape(document.Id)}?refresh=true";

        using var response = await _httpClient.PutAsync(
            requestUri,
            content,
            cancellationToken);

        var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError(
                "Failed to index OpenSearch document. StatusCode: {StatusCode}, DocumentId: {DocumentId}, Response: {ResponseBody}",
                response.StatusCode,
                document.Id,
                responseBody);

            return;
        }

        _logger.LogInformation(
            "Indexed OpenSearch document. Index: {IndexName}, DocumentId: {DocumentId}, SourceType: {SourceType}, SourceId: {SourceId}",
            _openSearchOptions.IndexName,
            document.Id,
            document.SourceType,
            document.SourceId);
    }

    private async Task DeleteDocumentAsync(
        string documentId,
        CancellationToken cancellationToken)
    {
        var requestUri =
            $"{Escape(_openSearchOptions.IndexName)}/_doc/{Escape(documentId)}?refresh=true";

        using var response = await _httpClient.DeleteAsync(
            requestUri,
            cancellationToken);

        var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);

        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            _logger.LogInformation(
                "OpenSearch document was already missing. Index: {IndexName}, DocumentId: {DocumentId}",
                _openSearchOptions.IndexName,
                documentId);

            return;
        }

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError(
                "Failed to delete OpenSearch document. StatusCode: {StatusCode}, DocumentId: {DocumentId}, Response: {ResponseBody}",
                response.StatusCode,
                documentId,
                responseBody);

            return;
        }

        _logger.LogInformation(
            "Deleted OpenSearch document. Index: {IndexName}, DocumentId: {DocumentId}",
            _openSearchOptions.IndexName,
            documentId);
    }

    private async Task DeleteIndexIfExistsAsync(
       CancellationToken cancellationToken)
    {
        using var response = await _httpClient.DeleteAsync(
            Escape(_openSearchOptions.IndexName),
            cancellationToken);

        var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);

        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            _logger.LogInformation(
                "OpenSearch index '{IndexName}' did not exist before rebuild.",
                _openSearchOptions.IndexName);

            return;
        }

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError(
                "Failed to delete OpenSearch index before rebuild. StatusCode: {StatusCode}, Index: {IndexName}, Response: {ResponseBody}",
                response.StatusCode,
                _openSearchOptions.IndexName,
                responseBody);

            throw new InvalidOperationException(
                $"OpenSearch index '{_openSearchOptions.IndexName}' could not be deleted.");
        }

        _logger.LogInformation(
            "Deleted OpenSearch index '{IndexName}' before rebuild.",
            _openSearchOptions.IndexName);
    }

    private async Task<bool> EnsureIndexExistsAsync(CancellationToken cancellationToken)
    {
        using var headRequest = new HttpRequestMessage(
            HttpMethod.Head,
            Escape(_openSearchOptions.IndexName));

        using var headResponse = await _httpClient.SendAsync(
            headRequest,
            cancellationToken);

        if (headResponse.IsSuccessStatusCode)
        {
            return true;
        }

        if (headResponse.StatusCode != HttpStatusCode.NotFound)
        {
            _logger.LogError(
                "Failed to check OpenSearch index existence. StatusCode: {StatusCode}, Index: {IndexName}",
                headResponse.StatusCode,
                _openSearchOptions.IndexName);

            return false;
        }

        var requestBody = OpenSearchIndexMappings.CreateIndexRequestBody();
        var json = JsonSerializer.Serialize(requestBody, JsonOptions);

        using var content = new StringContent(json, Encoding.UTF8, "application/json");

        using var createResponse = await _httpClient.PutAsync(
            Escape(_openSearchOptions.IndexName),
            content,
            cancellationToken);

        var createResponseBody = await createResponse.Content.ReadAsStringAsync(cancellationToken);

        if (createResponse.IsSuccessStatusCode)
        {
            _logger.LogInformation(
                "Created OpenSearch index '{IndexName}'.",
                _openSearchOptions.IndexName);

            return true;
        }

        if (createResponse.StatusCode == HttpStatusCode.BadRequest &&
            createResponseBody.Contains("resource_already_exists_exception", StringComparison.OrdinalIgnoreCase))
        {
            _logger.LogInformation(
                "OpenSearch index '{IndexName}' already exists.",
                _openSearchOptions.IndexName);

            return true;
        }

        _logger.LogError(
            "Failed to create OpenSearch index. StatusCode: {StatusCode}, Index: {IndexName}, Response: {ResponseBody}",
            createResponse.StatusCode,
            _openSearchOptions.IndexName,
            createResponseBody);

        return false;
    }

    private async Task ExecuteSafelyAsync(
        string operationName,
        Func<Task> operation)
    {
        try
        {
            await operation();
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception exception)
        {
            _logger.LogError(
                exception,
                "OpenSearch indexing operation failed: {OperationName}",
                operationName);
        }
    }

    private static string Escape(string value)
    {
        return Uri.EscapeDataString(value);
    }
}