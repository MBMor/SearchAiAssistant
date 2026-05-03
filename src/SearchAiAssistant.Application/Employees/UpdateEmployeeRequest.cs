namespace SearchAiAssistant.Application.Employees;

public sealed record UpdateEmployeeRequest(
    string FirstName,
    string LastName,
    string Email,
    string Department,
    string JobTitle,
    IReadOnlyList<string> Skills,
    string Location);