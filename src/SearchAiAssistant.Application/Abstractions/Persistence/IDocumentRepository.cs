using SearchAiAssistant.Application.Common.Pagination;
using SearchAiAssistant.Application.Documents;
using DocumentEntity = SearchAiAssistant.Domain.Entities.Document;

namespace SearchAiAssistant.Application.Abstractions.Persistence;

public interface IDocumentRepository
{
    Task<DocumentEntity?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<PagedResult<DocumentEntity>> ListAsync(
        DocumentListRequest request,
        CancellationToken cancellationToken = default);

    Task AddAsync(DocumentEntity document, CancellationToken cancellationToken = default);

    void Remove(DocumentEntity document);
}