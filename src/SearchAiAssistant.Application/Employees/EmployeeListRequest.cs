namespace SearchAiAssistant.Application.Employees;

public sealed record EmployeeListRequest(
    string? Department = null,
    string? JobTitle = null,
    string? Skill = null,
    int Page = 1,
    int PageSize = 20);