using SearchAiAssistant.Application.Common.Pagination;

namespace SearchAiAssistant.Application.Documents;

public interface IDocumentService
{
    Task<DocumentResponse> CreateAsync(
        CreateDocumentRequest request,
        CancellationToken cancellationToken = default);

    Task<DocumentResponse?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    Task<PagedResult<DocumentResponse>> ListAsync(
        DocumentListRequest request,
        CancellationToken cancellationToken = default);

    Task<DocumentResponse?> UpdateAsync(
        Guid id,
        UpdateDocumentRequest request,
        CancellationToken cancellationToken = default);

    Task<bool> DeleteAsync(
        Guid id,
        CancellationToken cancellationToken = default);
}