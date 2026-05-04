namespace SearchAiAssistant.Application.Common.Exceptions;

public sealed class DuplicateEmailException(string email) 
    : Exception($"Email '{email}' is already registered.")
{
    public string Email { get; } = email;
}