using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SearchAiAssistant.Application.Abstractions.Ai;
using SearchAiAssistant.Application.Assistant;
using SearchAiAssistant.Application.Common.Exceptions;

namespace SearchAiAssistant.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/assistant")]
public sealed class AssistantController(
    IAiAssistant aiAssistant,
    ILogger<AssistantController> logger) : ControllerBase
{
    private readonly IAiAssistant _aiAssistant = aiAssistant;
    private readonly ILogger<AssistantController> _logger = logger;

    [HttpPost("ask")]
    [ProducesResponseType<AssistantResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status503ServiceUnavailable)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<AssistantResponse>> Ask(
        AskAssistantRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation(
                "Assistant question received. MaxSources: {MaxSources}",
                request.MaxSources);

            var response = await _aiAssistant.AskAsync(
                request,
                cancellationToken);

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
            _logger.LogError(exception, "Assistant request failed.");

            return StatusCode(
                StatusCodes.Status503ServiceUnavailable,
                new ProblemDetails
                {
                    Title = "Assistant unavailable",
                    Detail = "The assistant could not process the request because the retrieval service is unavailable.",
                    Status = StatusCodes.Status503ServiceUnavailable
                });
        }
    }
}