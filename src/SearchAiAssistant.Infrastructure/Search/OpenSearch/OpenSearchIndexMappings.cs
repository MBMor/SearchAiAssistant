using SearchAiAssistant.Infrastructure.Search.OpenSearch.Models;

namespace SearchAiAssistant.Infrastructure.Search.OpenSearch;

public static class OpenSearchIndexMappings
{
    public static object CreateIndexRequestBody()
    {
        return new
        {
            settings = new
            {
                index = new
                {
                    number_of_shards = 1,
                    number_of_replicas = 0
                }
            },
            mappings = new
            {
                properties = new Dictionary<string, object>
                {
                    [SearchIndexFieldNames.Id] = Keyword(),
                    [SearchIndexFieldNames.SourceType] = Keyword(),
                    [SearchIndexFieldNames.SourceId] = Keyword(),

                    [SearchIndexFieldNames.Title] = TextWithKeyword(),
                    [SearchIndexFieldNames.Content] = Text(),
                    [SearchIndexFieldNames.Tags] = Keyword(),

                    [SearchIndexFieldNames.Category] = Keyword(),
                    [SearchIndexFieldNames.Department] = Keyword(),
                    [SearchIndexFieldNames.JobTitle] = Keyword(),
                    [SearchIndexFieldNames.Location] = Keyword(),

                    [SearchIndexFieldNames.EmployeeEmail] = Keyword(),
                    [SearchIndexFieldNames.EmployeeFullName] = TextWithKeyword(),

                    [SearchIndexFieldNames.IndexedAt] = Date()
                }
            }
        };
    }

    private static object Keyword()
    {
        return new
        {
            type = "keyword"
        };
    }

    private static object Text()
    {
        return new
        {
            type = "text"
        };
    }

    private static object TextWithKeyword()
    {
        return new
        {
            type = "text",
            fields = new
            {
                keyword = new
                {
                    type = "keyword",
                    ignore_above = 256
                }
            }
        };
    }

    private static object Date()
    {
        return new
        {
            type = "date"
        };
    }
}