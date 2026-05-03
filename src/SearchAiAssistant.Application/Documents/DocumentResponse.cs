namespace SearchAiAssistant.Application.Documents;

public sealed record DocumentResponse(
    Guid Id,
    string Title,
    string Content,
    string Category,
    IReadOnlyList<string> Tags,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);