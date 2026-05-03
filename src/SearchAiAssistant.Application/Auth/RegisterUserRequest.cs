using SearchAiAssistant.Domain.Enums;

namespace SearchAiAssistant.Application.Auth;

public sealed record RegisterUserRequest(
    string Email,
    string Password,
    UserRole Role = UserRole.User);