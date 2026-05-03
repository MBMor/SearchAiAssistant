namespace SearchAiAssistant.Application.Auth;

public sealed record CurrentUserResponse(
    Guid UserId,
    string Email,
    string Role);