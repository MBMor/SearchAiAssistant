namespace SearchAiAssistant.Application.Documents;

public sealed record DocumentListRequest(
    string? Category = null,
    string? Tag = null,
    int Page = 1,
    int PageSize = 20);