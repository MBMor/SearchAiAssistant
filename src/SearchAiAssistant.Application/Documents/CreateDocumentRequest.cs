namespace SearchAiAssistant.Application.Documents;

public sealed record CreateDocumentRequest(
    string Title,
    string Content,
    string Category,
    IReadOnlyList<string> Tags);