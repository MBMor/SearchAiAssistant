using Microsoft.Extensions.Options;
using SearchAiAssistant.Application.Abstractions.Indexing;
using SearchAiAssistant.Application.Abstractions.Persistence;
using SearchAiAssistant.Application.Common.Abstractions;
using SearchAiAssistant.Application.Common.Options;
using SearchAiAssistant.Application.Common.Pagination;
using DocumentEntity = SearchAiAssistant.Domain.Entities.Document;

namespace SearchAiAssistant.Application.Documents;

public sealed class DocumentService(
    IDocumentRepository documentRepository,
    IDateTimeProvider dateTimeProvider,
    IUnitOfWork unitOfWork,
    IIndexingService indexingService,
    IOptions<PaginationOptions> paginationOptions) : IDocumentService
{
    private readonly IDocumentRepository _documentRepository = documentRepository;
    private readonly IDateTimeProvider _dateTimeProvider = dateTimeProvider;
    private readonly IUnitOfWork _unitOfWork = unitOfWork;
    private readonly IIndexingService _indexingService = indexingService;
    private readonly PaginationOptions _paginationOptions = paginationOptions.Value;

    public async Task<DocumentResponse> CreateAsync(
        CreateDocumentRequest request,
        CancellationToken cancellationToken = default)
    {
        var now = _dateTimeProvider.UtcNow;

        var document = new DocumentEntity(
            id: Guid.NewGuid(),
            title: request.Title,
            content: request.Content,
            category: request.Category,
            tags: request.Tags,
            createdAt: now);

        await _documentRepository.AddAsync(document, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        await _indexingService.IndexDocumentAsync(document, cancellationToken);

        return ToResponse(document);
    }

    public async Task<DocumentResponse?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var document = await _documentRepository.GetByIdAsync(id, cancellationToken);

        return document is null
            ? null
            : ToResponse(document);
    }

    public async Task<PagedResult<DocumentResponse>> ListAsync(
        DocumentListRequest request,
        CancellationToken cancellationToken = default)
    {
        var normalizedRequest = NormalizeListRequest(request);

        var result = await _documentRepository.ListAsync(
            normalizedRequest,
            cancellationToken);

        var items = result.Items
            .Select(ToResponse)
            .ToList();

        return new PagedResult<DocumentResponse>(
            Items: items,
            Page: result.Page,
            PageSize: result.PageSize,
            TotalCount: result.TotalCount);
    }

    public async Task<DocumentResponse?> UpdateAsync(
        Guid id,
        UpdateDocumentRequest request,
        CancellationToken cancellationToken = default)
    {
        var document = await _documentRepository.GetByIdAsync(id, cancellationToken);

        if (document is null)
        {
            return null;
        }

        document.Update(
            title: request.Title,
            content: request.Content,
            category: request.Category,
            tags: request.Tags,
            updatedAt: _dateTimeProvider.UtcNow);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        await _indexingService.IndexDocumentAsync(document, cancellationToken);

        return ToResponse(document);
    }

    public async Task<bool> DeleteAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var document = await _documentRepository.GetByIdAsync(id, cancellationToken);

        if (document is null)
        {
            return false;
        }

        _documentRepository.Remove(document);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        await _indexingService.RemoveDocumentAsync(id, cancellationToken);

        return true;
    }

    private DocumentListRequest NormalizeListRequest(DocumentListRequest request)
    {
        var page = request.Page < 1
            ? 1
            : request.Page;

        var pageSize = request.PageSize < 1
            ? _paginationOptions.DefaultPageSize
            : request.PageSize;

        pageSize = Math.Min(pageSize, _paginationOptions.MaxPageSize);

        return request with
        {
            Category = NormalizeOptionalFilter(request.Category),
            Tag = NormalizeOptionalFilter(request.Tag),
            Page = page,
            PageSize = pageSize
        };
    }

    private static string? NormalizeOptionalFilter(string? value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? null
            : value.Trim();
    }

    private static DocumentResponse ToResponse(DocumentEntity document)
    {
        return new DocumentResponse(
            Id: document.Id,
            Title: document.Title,
            Content: document.Content,
            Category: document.Category,
            Tags: document.Tags,
            CreatedAt: document.CreatedAt,
            UpdatedAt: document.UpdatedAt);
    }
}