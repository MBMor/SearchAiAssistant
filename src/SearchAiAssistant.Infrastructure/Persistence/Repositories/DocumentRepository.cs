using Microsoft.EntityFrameworkCore;
using SearchAiAssistant.Application.Abstractions.Persistence;
using SearchAiAssistant.Application.Common.Pagination;
using SearchAiAssistant.Application.Documents;
using DocumentEntity = SearchAiAssistant.Domain.Entities.Document;

namespace SearchAiAssistant.Infrastructure.Persistence.Repositories;

public sealed class DocumentRepository(SearchAiAssistantDbContext dbContext) : IDocumentRepository
{
    private readonly SearchAiAssistantDbContext _dbContext = dbContext;

    public Task<DocumentEntity?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        return _dbContext.Documents
            .FirstOrDefaultAsync(document => document.Id == id, cancellationToken);
    }

    public async Task<IReadOnlyList<DocumentEntity>> ListAllAsync(
    CancellationToken cancellationToken = default)
    {
        return await _dbContext.Documents
            .AsNoTracking()
            .OrderBy(document => document.Title)
            .ThenBy(document => document.Id)
            .ToListAsync(cancellationToken);
    }

    public async Task<PagedResult<DocumentEntity>> ListAsync(
        DocumentListRequest request,
        CancellationToken cancellationToken = default)
    {
        var query = _dbContext.Documents
            .AsNoTracking()
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(request.Category))
        {
            var category = request.Category.Trim().ToLowerInvariant();

            query = query.Where(document =>
                document.Category.Equals(category, StringComparison.CurrentCultureIgnoreCase));
        }

        if (!string.IsNullOrWhiteSpace(request.Tag))
        {
            var tag = request.Tag.Trim();

            query = query.Where(document =>
                document.Tags.Contains(tag));
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderBy(document => document.Title)
            .ThenBy(document => document.Id)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<DocumentEntity>(
            Items: items,
            Page: request.Page,
            PageSize: request.PageSize,
            TotalCount: totalCount);
    }

    public async Task AddAsync(
        DocumentEntity document,
        CancellationToken cancellationToken = default)
    {
        await _dbContext.Documents.AddAsync(document, cancellationToken);
    }

    public void Remove(DocumentEntity document)
    {
        _dbContext.Documents.Remove(document);
    }
}