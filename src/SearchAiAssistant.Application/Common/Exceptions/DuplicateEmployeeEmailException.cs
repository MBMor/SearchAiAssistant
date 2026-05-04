namespace SearchAiAssistant.Application.Common.Exceptions;

public sealed class DuplicateEmployeeEmailException(string email) 
    : Exception($"Employee email '{email}' is already used.")
{
    public string Email { get; } = email;
}