using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SearchAiAssistant.Application.Auth;
using SearchAiAssistant.Application.Common.Exceptions;

namespace SearchAiAssistant.Api.Controllers;

[ApiController]
[Route("api/auth")]
public sealed class AuthController(
    IAuthService authService,
    ILogger<AuthController> logger) : ControllerBase
{
    private readonly IAuthService _authService = authService;
    private readonly ILogger<AuthController> _logger = logger;

    [HttpPost("register")]
    [AllowAnonymous]
    [ProducesResponseType<AuthResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<AuthResponse>> Register(
        RegisterUserRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var response = await _authService.RegisterAsync(request, cancellationToken);

            return Ok(response);
        }
        catch (DuplicateEmailException exception)
        {
            _logger.LogInformation(
                exception,
                "Registration failed because email is already registered: {Email}",
                exception.Email);

            return Conflict(new ProblemDetails
            {
                Title = "Email already registered",
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

    [HttpPost("login")]
    [AllowAnonymous]
    [ProducesResponseType<AuthResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<AuthResponse>> Login(
        LoginRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var response = await _authService.LoginAsync(request, cancellationToken);

            return Ok(response);
        }
        catch (InvalidCredentialsException exception)
        {
            _logger.LogInformation(exception, "Login failed because credentials are invalid.");

            return Unauthorized(new ProblemDetails
            {
                Title = "Invalid credentials",
                Detail = exception.Message,
                Status = StatusCodes.Status401Unauthorized
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
    }

    [HttpGet("me")]
    [Authorize]
    [ProducesResponseType<CurrentUserResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public ActionResult<CurrentUserResponse> Me()
    {
        var userIdValue = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var email = User.FindFirstValue(ClaimTypes.Email);
        var role = User.FindFirstValue(ClaimTypes.Role);

        if (!Guid.TryParse(userIdValue, out var userId) ||
            string.IsNullOrWhiteSpace(email) ||
            string.IsNullOrWhiteSpace(role))
        {
            return Unauthorized();
        }

        return Ok(new CurrentUserResponse(
            UserId: userId,
            Email: email,
            Role: role));
    }
}