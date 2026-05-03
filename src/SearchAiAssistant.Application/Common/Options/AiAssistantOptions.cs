namespace SearchAiAssistant.Application.Common.Options;

public sealed class AiAssistantOptions
{
    public const string SectionName = "AiAssistant";

    public string Provider { get; init; } = "Mock";

    public int MaxRetrievedSources { get; init; } = 5;

    public double MinimumScore { get; init; } = 0.1;
}