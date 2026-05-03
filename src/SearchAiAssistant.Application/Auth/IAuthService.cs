namespace SearchAiAssistant.Application.Auth;

public interface IAuthService
{
    Task<AuthResponse> RegisterAsync(
        RegisterUserRequest request,
        CancellationToken cancellationToken = default);

    Task<AuthResponse> LoginAsync(
        LoginRequest request,
        CancellationToken cancellationToken = default);
}