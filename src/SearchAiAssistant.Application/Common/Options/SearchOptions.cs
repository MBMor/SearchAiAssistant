namespace SearchAiAssistant.Application.Common.Options;

public sealed class SearchOptions
{
    public const string SectionName = "Search";

    public string DefaultSort { get; init; } = "relevance";

    public bool EnableHighlighting { get; init; } = true;
}