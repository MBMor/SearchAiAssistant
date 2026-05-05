using SearchAiAssistant.Domain.Entities;
using DocumentEntity = SearchAiAssistant.Domain.Entities.Document;

namespace SearchAiAssistant.Application.Abstractions.Indexing;

public interface IIndexingService
{
    Task IndexEmployeeAsync(
        Employee employee,
        CancellationToken cancellationToken = default);

    Task IndexDocumentAsync(
        DocumentEntity document,
        CancellationToken cancellationToken = default);

    Task RemoveEmployeeAsync(
        Guid employeeId,
        CancellationToken cancellationToken = default);

    Task RemoveDocumentAsync(
        Guid documentId,
        CancellationToken cancellationToken = default);
}