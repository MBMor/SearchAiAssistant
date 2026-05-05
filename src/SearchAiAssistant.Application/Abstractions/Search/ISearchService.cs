using SearchAiAssistant.Application.Common.Pagination;
using SearchAiAssistant.Application.Search;

namespace SearchAiAssistant.Application.Abstractions.Search;

public interface ISearchService
{
    Task<PagedResult<SearchResultItem>> SearchAsync(
        SearchRequest request,
        CancellationToken cancellationToken = default);

    Task<PagedResult<SearchResultItem>> SearchEmployeesAsync(
        SearchRequest request,
        CancellationToken cancellationToken = default);

    Task<PagedResult<SearchResultItem>> SearchDocumentsAsync(
        SearchRequest request,
        CancellationToken cancellationToken = default);
}