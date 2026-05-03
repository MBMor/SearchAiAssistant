namespace SearchAiAssistant.Application.Employees;

public sealed record EmployeeResponse(
    Guid Id,
    string FirstName,
    string LastName,
    string Email,
    string Department,
    string JobTitle,
    IReadOnlyList<string> Skills,
    string Location,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);