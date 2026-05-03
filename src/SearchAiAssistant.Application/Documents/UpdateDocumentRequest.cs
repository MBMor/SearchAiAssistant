namespace SearchAiAssistant.Application.Documents;

public sealed record UpdateDocumentRequest(
    string Title,
    string Content,
    string Category,
    IReadOnlyList<string> Tags);