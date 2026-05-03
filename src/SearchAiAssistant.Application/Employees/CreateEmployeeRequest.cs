namespace SearchAiAssistant.Application.Employees;

public sealed record CreateEmployeeRequest(
    string FirstName,
    string LastName,
    string Email,
    string Department,
    string JobTitle,
    IReadOnlyList<string> Skills,
    string Location);