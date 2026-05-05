namespace SearchAiAssistant.Application.Search;

public sealed record SearchRequest(
    string? Query = null,
    string? SourceType = null,
    string? Category = null,
    string? Department = null,
    string? JobTitle = null,
    string? Tag = null,
    int Page = 1,
    int PageSize = 20,
    string? Sort = null);