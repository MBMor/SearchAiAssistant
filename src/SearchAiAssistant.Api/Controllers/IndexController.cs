using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SearchAiAssistant.Application.Indexing;

namespace SearchAiAssistant.Api.Controllers;

[ApiController]
[Authorize(Roles = "Admin")]
[Route("api/index")]
public sealed class IndexController(
    IIndexManagementService indexManagementService,
    ILogger<IndexController> logger) : ControllerBase
{
    private readonly IIndexManagementService _indexManagementService = indexManagementService;
    private readonly ILogger<IndexController> _logger = logger;

    [HttpPost("rebuild")]
    [ProducesResponseType<RebuildIndexResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status503ServiceUnavailable)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<RebuildIndexResponse>> Rebuild(
        CancellationToken cancellationToken)
    {
        try
        {
            var response = await _indexManagementService.RebuildAsync(cancellationToken);

            return Ok(response);
        }
        catch (InvalidOperationException exception)
        {
            _logger.LogError(exception, "Search index rebuild failed.");

            return StatusCode(
                StatusCodes.Status503ServiceUnavailable,
                new ProblemDetails
                {
                    Title = "Search index rebuild failed",
                    Detail = "The search index could not be rebuilt.",
                    Status = StatusCodes.Status503ServiceUnavailable
                });
        }
    }

    [HttpPost("employees/{id:guid}")]
    [ProducesResponseType<IndexItemResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<IndexItemResponse>> IndexEmployee(
        Guid id,
        CancellationToken cancellationToken)
    {
        var response = await _indexManagementService.IndexEmployeeAsync(
            id,
            cancellationToken);

        if (!response.Indexed)
        {
            return NotFound(new ProblemDetails
            {
                Title = "Employee not found",
                Detail = response.Message,
                Status = StatusCodes.Status404NotFound
            });
        }

        return Ok(response);
    }

    [HttpPost("documents/{id:guid}")]
    [ProducesResponseType<IndexItemResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<IndexItemResponse>> IndexDocument(
        Guid id,
        CancellationToken cancellationToken)
    {
        var response = await _indexManagementService.IndexDocumentAsync(
            id,
            cancellationToken);

        if (!response.Indexed)
        {
            return NotFound(new ProblemDetails
            {
                Title = "Document not found",
                Detail = response.Message,
                Status = StatusCodes.Status404NotFound
            });
        }

        return Ok(response);
    }
}