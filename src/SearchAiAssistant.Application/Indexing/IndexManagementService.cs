using SearchAiAssistant.Application.Abstractions.Indexing;
using SearchAiAssistant.Application.Abstractions.Persistence;
using SearchAiAssistant.Application.Search;

namespace SearchAiAssistant.Application.Indexing;

public sealed class IndexManagementService(
    IEmployeeRepository employeeRepository,
    IDocumentRepository documentRepository,
    IIndexingService indexingService) : IIndexManagementService
{
    private readonly IEmployeeRepository _employeeRepository = employeeRepository;
    private readonly IDocumentRepository _documentRepository = documentRepository;
    private readonly IIndexingService _indexingService = indexingService;

    public async Task<RebuildIndexResponse> RebuildAsync(
        CancellationToken cancellationToken = default)
    {
        await _indexingService.RecreateIndexAsync(cancellationToken);

        var employees = await _employeeRepository.ListAllAsync(cancellationToken);
        var documents = await _documentRepository.ListAllAsync(cancellationToken);

        foreach (var employee in employees)
        {
            await _indexingService.IndexEmployeeAsync(employee, cancellationToken);
        }

        foreach (var document in documents)
        {
            await _indexingService.IndexDocumentAsync(document, cancellationToken);
        }

        return new RebuildIndexResponse(
            EmployeesIndexed: employees.Count,
            DocumentsIndexed: documents.Count);
    }

    public async Task<IndexItemResponse> IndexEmployeeAsync(
        Guid employeeId,
        CancellationToken cancellationToken = default)
    {
        var employee = await _employeeRepository.GetByIdAsync(
            employeeId,
            cancellationToken);

        if (employee is null)
        {
            return new IndexItemResponse(
                SourceId: employeeId,
                SourceType: SearchSourceTypes.Employee,
                Indexed: false,
                Message: $"Employee with id '{employeeId}' was not found.");
        }

        await _indexingService.IndexEmployeeAsync(employee, cancellationToken);

        return new IndexItemResponse(
            SourceId: employee.Id,
            SourceType: SearchSourceTypes.Employee,
            Indexed: true,
            Message: "Employee was indexed.");
    }

    public async Task<IndexItemResponse> IndexDocumentAsync(
        Guid documentId,
        CancellationToken cancellationToken = default)
    {
        var document = await _documentRepository.GetByIdAsync(
            documentId,
            cancellationToken);

        if (document is null)
        {
            return new IndexItemResponse(
                SourceId: documentId,
                SourceType: SearchSourceTypes.Document,
                Indexed: false,
                Message: $"Document with id '{documentId}' was not found.");
        }

        await _indexingService.IndexDocumentAsync(document, cancellationToken);

        return new IndexItemResponse(
            SourceId: document.Id,
            SourceType: SearchSourceTypes.Document,
            Indexed: true,
            Message: "Document was indexed.");
    }
}