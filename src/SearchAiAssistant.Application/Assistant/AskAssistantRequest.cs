namespace SearchAiAssistant.Application.Assistant;

public sealed record AskAssistantRequest(
    string Question,
    int MaxSources = 5);