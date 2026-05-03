namespace SearchAiAssistant.Infrastructure.Search.OpenSearch;

public sealed class OpenSearchOptions
{
    public const string SectionName = "OpenSearch";

    public string Uri { get; init; } = "http://localhost:9200";

    public string IndexName { get; init; } = "search-ai-assistant";

    public int RequestTimeoutSeconds { get; init; } = 30;
}