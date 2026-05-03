using SearchAiAssistant.Domain.Entities;
using SearchAiAssistant.Domain.Enums;

namespace SearchAiAssistant.Application.Abstractions.Persistence;

public interface ISearchDocumentRepository
{
    Task<SearchDocument?> GetBySourceAsync(
        SearchDocumentSourceType sourceType,
        Guid sourceId,
        CancellationToken cancellationToken = default);

    Task AddAsync(SearchDocument searchDocument, CancellationToken cancellationToken = default);

    void Remove(SearchDocument searchDocument);
}