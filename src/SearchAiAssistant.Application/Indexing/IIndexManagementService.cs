namespace SearchAiAssistant.Application.Indexing;

public interface IIndexManagementService
{
    Task<RebuildIndexResponse> RebuildAsync(
        CancellationToken cancellationToken = default);

    Task<IndexItemResponse> IndexEmployeeAsync(
        Guid employeeId,
        CancellationToken cancellationToken = default);

    Task<IndexItemResponse> IndexDocumentAsync(
        Guid documentId,
        CancellationToken cancellationToken = default);
}