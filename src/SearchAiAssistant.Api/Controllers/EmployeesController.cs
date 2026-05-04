using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SearchAiAssistant.Application.Common.Exceptions;
using SearchAiAssistant.Application.Common.Pagination;
using SearchAiAssistant.Application.Employees;

namespace SearchAiAssistant.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/employees")]
public sealed class EmployeesController(
    IEmployeeService employeeService,
    ILogger<EmployeesController> logger) : ControllerBase
{
    private readonly IEmployeeService _employeeService = employeeService;
    private readonly ILogger<EmployeesController> _logger = logger;

    [HttpGet]
    [ProducesResponseType<PagedResult<EmployeeResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<PagedResult<EmployeeResponse>>> List(
        [FromQuery] EmployeeListRequest request,
        CancellationToken cancellationToken)
    {
        var response = await _employeeService.ListAsync(request, cancellationToken);

        return Ok(response);
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType<EmployeeResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<EmployeeResponse>> GetById(
        Guid id,
        CancellationToken cancellationToken)
    {
        var response = await _employeeService.GetByIdAsync(id, cancellationToken);

        if (response is null)
        {
            return NotFound(new ProblemDetails
            {
                Title = "Employee not found",
                Detail = $"Employee with id '{id}' was not found.",
                Status = StatusCodes.Status404NotFound
            });
        }

        return Ok(response);
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType<EmployeeResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<EmployeeResponse>> Create(
        CreateEmployeeRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var response = await _employeeService.CreateAsync(request, cancellationToken);

            return CreatedAtAction(
                nameof(GetById),
                new { id = response.Id },
                response);
        }
        catch (DuplicateEmployeeEmailException exception)
        {
            _logger.LogInformation(
                exception,
                "Employee creation failed because email is already used: {Email}",
                exception.Email);

            return Conflict(new ProblemDetails
            {
                Title = "Employee email already used",
                Detail = exception.Message,
                Status = StatusCodes.Status409Conflict
            });
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
    [ProducesResponseType<EmployeeResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<EmployeeResponse>> Update(
        Guid id,
        UpdateEmployeeRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var response = await _employeeService.UpdateAsync(id, request, cancellationToken);

            if (response is null)
            {
                return NotFound(new ProblemDetails
                {
                    Title = "Employee not found",
                    Detail = $"Employee with id '{id}' was not found.",
                    Status = StatusCodes.Status404NotFound
                });
            }

            return Ok(response);
        }
        catch (DuplicateEmployeeEmailException exception)
        {
            _logger.LogInformation(
                exception,
                "Employee update failed because email is already used: {Email}",
                exception.Email);

            return Conflict(new ProblemDetails
            {
                Title = "Employee email already used",
                Detail = exception.Message,
                Status = StatusCodes.Status409Conflict
            });
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
        var deleted = await _employeeService.DeleteAsync(id, cancellationToken);

        if (!deleted)
        {
            return NotFound(new ProblemDetails
            {
                Title = "Employee not found",
                Detail = $"Employee with id '{id}' was not found.",
                Status = StatusCodes.Status404NotFound
            });
        }

        return NoContent();
    }
}