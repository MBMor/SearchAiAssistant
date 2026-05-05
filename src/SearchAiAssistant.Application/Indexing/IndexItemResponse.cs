namespace SearchAiAssistant.Application.Indexing;

public sealed record IndexItemResponse(
    Guid SourceId,
    string SourceType,
    bool Indexed,
    string Message);