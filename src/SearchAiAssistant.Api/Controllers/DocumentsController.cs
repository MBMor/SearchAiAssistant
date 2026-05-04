using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SearchAiAssistant.Application.Common.Exceptions;
using SearchAiAssistant.Application.Common.Pagination;
using SearchAiAssistant.Application.Documents;

namespace SearchAiAssistant.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/documents")]
public sealed class DocumentsController(IDocumentService documentService) : ControllerBase
{
    private readonly IDocumentService _documentService = documentService;

    [HttpGet]
    [ProducesResponseType<PagedResult<DocumentResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<PagedResult<DocumentResponse>>> List(
        [FromQuery] DocumentListRequest request,
        CancellationToken cancellationToken)
    {
        var response = await _documentService.ListAsync(request, cancellationToken);

        return Ok(response);
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType<DocumentResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<DocumentResponse>> GetById(
        Guid id,
        CancellationToken cancellationToken)
    {
        var response = await _documentService.GetByIdAsync(id, cancellationToken);

        if (response is null)
        {
            return NotFound(new ProblemDetails
            {
                Title = "Document not found",
                Detail = $"Document with id '{id}' was not found.",
                Status = StatusCodes.Status404NotFound
            });
        }

        return Ok(response);
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType<DocumentResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<DocumentResponse>> Create(
        CreateDocumentRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var response = await _documentService.CreateAsync(request, cancellationToken);

            return CreatedAtAction(
                nameof(GetById),
                new { id = response.Id },
                response);
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
        catch (ArgumentException exception)
        {
            return BadRequest(new ProblemDetails
            {
                Title = "Validation failed",
                Detail = exception.Message,
                Status = StatusCodes.Status400BadRequest
            });
        }
    }

    [HttpPut("{id:guid}")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType<DocumentResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<DocumentResponse>> Update(
        Guid id,
        UpdateDocumentRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var response = await _documentService.UpdateAsync(id, request, cancellationToken);

            if (response is null)
            {
                return NotFound(new ProblemDetails
                {
                    Title = "Document not found",
                    Detail = $"Document with id '{id}' was not found.",
                    Status = StatusCodes.Status404NotFound
                });
            }

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
        catch (ArgumentException exception)
        {
            return BadRequest(new ProblemDetails
            {
                Title = "Validation failed",
                Detail = exception.Message,
                Status = StatusCodes.Status400BadRequest
            });
        }
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Delete(
        Guid id,
        CancellationToken cancellationToken)
    {
        var deleted = await _documentService.DeleteAsync(id, cancellationToken);

        if (!deleted)
        {
            return NotFound(new ProblemDetails
            {
                Title = "Document not found",
                Detail = $"Document with id '{id}' was not found.",
                Status = StatusCodes.Status404NotFound
            });
        }

        return NoContent();
    }
}