using SearchAiAssistant.Application.Assistant;

namespace SearchAiAssistant.Application.Abstractions.Ai;

public interface IAiAssistant
{
    Task<AssistantResponse> AskAsync(
        AskAssistantRequest request,
        CancellationToken cancellationToken = default);
}