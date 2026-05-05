namespace SearchAiAssistant.Application.Search;

public sealed record SearchResultItem(
    Guid SourceId,
    string SourceType,
    string Title,
    string ContentPreview,
    IReadOnlyList<string> Tags,
    string? Category,
    string? Department,
    string? JobTitle,
    string? Location,
    double Score,
    IReadOnlyList<string> Highlights);