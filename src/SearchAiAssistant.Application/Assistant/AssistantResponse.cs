namespace SearchAiAssistant.Application.Assistant;

public sealed record AssistantResponse(
    string Question,
    string Answer,
    IReadOnlyList<AssistantSource> Sources,
    bool HasEnoughInformation);