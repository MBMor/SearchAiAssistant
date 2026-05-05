using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SearchAiAssistant.Application.Abstractions.Search;
using SearchAiAssistant.Application.Common.Exceptions;
using SearchAiAssistant.Application.Common.Pagination;
using SearchAiAssistant.Application.Search;

namespace SearchAiAssistant.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/search")]
public sealed class SearchController(
    ISearchService searchService,
    ILogger<SearchController> logger) : ControllerBase
{
    private readonly ISearchService _searchService = searchService;
    private readonly ILogger<SearchController> _logger = logger;

    [HttpGet]
    [ProducesResponseType<PagedResult<SearchResultItem>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status503ServiceUnavailable)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public Task<ActionResult<PagedResult<SearchResultItem>>> Search(
        [FromQuery] SearchRequest request,
        CancellationToken cancellationToken)
    {
        return ExecuteSearchAsync(
            () => _searchService.SearchAsync(request, cancellationToken));
    }

    [HttpGet("employees")]
    [ProducesResponseType<PagedResult<SearchResultItem>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status503ServiceUnavailable)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public Task<ActionResult<PagedResult<SearchResultItem>>> SearchEmployees(
        [FromQuery] SearchRequest request,
        CancellationToken cancellationToken)
    {
        return ExecuteSearchAsync(
            () => _searchService.SearchEmployeesAsync(request, cancellationToken));
    }

    [HttpGet("documents")]
    [ProducesResponseType<PagedResult<SearchResultItem>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status503ServiceUnavailable)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public Task<ActionResult<PagedResult<SearchResultItem>>> SearchDocuments(
        [FromQuery] SearchRequest request,
        CancellationToken cancellationToken)
    {
        return ExecuteSearchAsync(
            () => _searchService.SearchDocumentsAsync(request, cancellationToken));
    }

    private async Task<ActionResult<PagedResult<SearchResultItem>>> ExecuteSearchAsync(
        Func<Task<PagedResult<SearchResultItem>>> search)
    {
        try
        {
            var response = await search();

            return Ok(response);
        }
        catch (ValidationException exception)
        {
            return BadRequest(new ProblemDetails
            {
                Title = "Validation failed",
                Detail = exception.Message,
                Status = StatusCodes.Status400BadRequest
            });
        }
        catch (InvalidOperationException exception)
        {
            _logger.LogError(exception, "Search request failed.");

            return StatusCode(
                StatusCodes.Status503ServiceUnavailable,
                new ProblemDetails
                {
                    Title = "Search service unavailable",
                    Detail = "The search service could not process the request.",
                    Status = StatusCodes.Status503ServiceUnavailable
                });
        }
    }
}