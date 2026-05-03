using SearchAiAssistant.Domain.Entities;

namespace SearchAiAssistant.Application.Abstractions.Authentication;

public interface IJwtTokenGenerator
{
    string GenerateAccessToken(User user);
}