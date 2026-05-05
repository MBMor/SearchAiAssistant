namespace SearchAiAssistant.Infrastructure.Search.OpenSearch.Models;

public sealed class SearchIndexDocument
{
    public string Id { get; init; } = string.Empty;

    public string SourceType { get; init; } = string.Empty;

    public Guid SourceId { get; init; }

    public string Title { get; init; } = string.Empty;

    public string Content { get; init; } = string.Empty;

    public IReadOnlyList<string> Tags { get; init; } = [];

    public string? Category { get; init; }

    public string? Department { get; init; }

    public string? JobTitle { get; init; }

    public string? Location { get; init; }

    public string? EmployeeEmail { get; init; }

    public string? EmployeeFullName { get; init; }

    public DateTimeOffset IndexedAt { get; init; }
}