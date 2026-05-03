namespace SearchAiAssistant.Application.Auth;

public sealed record AuthResponse(
    Guid UserId,
    string Email,
    string Role,
    string AccessToken);