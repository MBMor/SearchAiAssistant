namespace SearchAiAssistant.Application.Assistant;

public sealed record AssistantSource(
    Guid SourceId,
    string SourceType,
    string Title,
    string ContentPreview,
    double Score,
    IReadOnlyList<string> Highlights);