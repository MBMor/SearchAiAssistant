using System.Net;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SearchAiAssistant.Application.Abstractions.Search;
using SearchAiAssistant.Application.Common.Exceptions;
using SearchAiAssistant.Application.Common.Options;
using SearchAiAssistant.Application.Common.Pagination;
using SearchAiAssistant.Application.Search;

namespace SearchAiAssistant.Infrastructure.Search.OpenSearch;

public sealed class OpenSearchSearchService(
    HttpClient httpClient,
    IOptions<OpenSearchOptions> openSearchOptions,
    IOptions<PaginationOptions> paginationOptions,
    IOptions<SearchOptions> searchOptions,
    ILogger<OpenSearchSearchService> logger) : ISearchService
{
    private const int ContentPreviewMaxLength = 300;

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly HttpClient _httpClient = httpClient;
    private readonly OpenSearchOptions _openSearchOptions = openSearchOptions.Value;
    private readonly PaginationOptions _paginationOptions = paginationOptions.Value;
    private readonly SearchOptions _searchOptions = searchOptions.Value;
    private readonly ILogger<OpenSearchSearchService> _logger = logger;
    private static readonly string[] MultiMatchFields =
                        [
                            "title^3",
                            "employeeFullName^3",
                            "content",
                            "tags",
                            "department",
                            "jobTitle",
                            "category",
                            "location"
                        ];

    public async Task<PagedResult<SearchResultItem>> SearchAsync(
        SearchRequest request,
        CancellationToken cancellationToken = default)
    {
        var normalizedRequest = NormalizeRequest(request);

        var openSearchRequestBody = BuildOpenSearchRequestBody(normalizedRequest);

        var json = JsonSerializer.Serialize(openSearchRequestBody, JsonOptions);

        using var content = new StringContent(json, Encoding.UTF8, "application/json");

        var requestUri = $"{Uri.EscapeDataString(_openSearchOptions.IndexName)}/_search";

        _logger.LogInformation(
            "Running OpenSearch query. Index: {IndexName}, Query: {Query}, SourceType: {SourceType}, Page: {Page}, PageSize: {PageSize}",
            _openSearchOptions.IndexName,
            normalizedRequest.Query,
            normalizedRequest.SourceType,
            normalizedRequest.Page,
            normalizedRequest.PageSize);

        using var response = await _httpClient.PostAsync(
            requestUri,
            content,
            cancellationToken);

        var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);

        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            _logger.LogInformation(
                "OpenSearch index '{IndexName}' does not exist yet. Returning empty search result.",
                _openSearchOptions.IndexName);

            return EmptyResult(normalizedRequest);
        }

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError(
                "OpenSearch search request failed. StatusCode: {StatusCode}, Response: {ResponseBody}",
                response.StatusCode,
                responseBody);

            throw new InvalidOperationException("OpenSearch search request failed.");
        }

        return ParseSearchResponse(responseBody, normalizedRequest);
    }

    public Task<PagedResult<SearchResultItem>> SearchEmployeesAsync(
        SearchRequest request,
        CancellationToken cancellationToken = default)
    {
        return SearchAsync(
            request with { SourceType = SearchSourceTypes.Employee },
            cancellationToken);
    }

    public Task<PagedResult<SearchResultItem>> SearchDocumentsAsync(
        SearchRequest request,
        CancellationToken cancellationToken = default)
    {
        return SearchAsync(
            request with { SourceType = SearchSourceTypes.Document },
            cancellationToken);
    }

    private SearchRequest NormalizeRequest(SearchRequest request)
    {
        var page = request.Page < 1
            ? 1
            : request.Page;

        var pageSize = request.PageSize < 1
            ? _paginationOptions.DefaultPageSize
            : request.PageSize;

        pageSize = Math.Min(pageSize, _paginationOptions.MaxPageSize);

        var sourceType = NormalizeOptionalValue(request.SourceType);

        if (!string.IsNullOrWhiteSpace(sourceType))
        {
            sourceType = SearchSourceTypes.Normalize(sourceType);

            if (!SearchSourceTypes.IsSupported(sourceType))
            {
                throw new ValidationException(
                    $"Unsupported source type '{request.SourceType}'. Supported values are '{SearchSourceTypes.Employee}' and '{SearchSourceTypes.Document}'.");
            }
        }

        return request with
        {
            Query = NormalizeOptionalValue(request.Query),
            SourceType = sourceType,
            Category = NormalizeOptionalValue(request.Category),
            Department = NormalizeOptionalValue(request.Department),
            JobTitle = NormalizeOptionalValue(request.JobTitle),
            Tag = NormalizeOptionalValue(request.Tag),
            Sort = NormalizeOptionalValue(request.Sort),
            Page = page,
            PageSize = pageSize
        };
    }

    private object BuildOpenSearchRequestBody(SearchRequest request)
    {
        var filters = new List<object>();

        AddTermFilter(filters, "sourceType", request.SourceType);
        AddTermFilter(filters, "category", request.Category);
        AddTermFilter(filters, "department", request.Department);
        AddTermFilter(filters, "jobTitle", request.JobTitle);
        AddTermFilter(filters, "tags", request.Tag);

        var must = new List<object>
        {
            string.IsNullOrWhiteSpace(request.Query)
                ? new Dictionary<string, object>
                {
                    ["match_all"] = new { }
                }
                : new Dictionary<string, object>
                {
                    ["multi_match"] = new
                    {
                        query = request.Query,
                        fields = MultiMatchFields,
                        fuzziness = "AUTO"
                    }
                }
        };

        var boolQuery = new Dictionary<string, object>
        {
            ["must"] = must
        };

        if (filters.Count > 0)
        {
            boolQuery["filter"] = filters;
        }

        var body = new Dictionary<string, object?>
        {
            ["from"] = (request.Page - 1) * request.PageSize,
            ["size"] = request.PageSize,
            ["track_total_hits"] = true,
            ["query"] = new Dictionary<string, object>
            {
                ["bool"] = boolQuery
            }
        };

        if (_searchOptions.EnableHighlighting && !string.IsNullOrWhiteSpace(request.Query))
        {
            body["highlight"] = new Dictionary<string, object>
            {
                ["pre_tags"] = new[] { "<mark>" },
                ["post_tags"] = new[] { "</mark>" },
                ["fields"] = new Dictionary<string, object>
                {
                    ["title"] = new { },
                    ["employeeFullName"] = new { },
                    ["content"] = new { },
                    ["tags"] = new { },
                    ["department"] = new { },
                    ["jobTitle"] = new { },
                    ["category"] = new { },
                    ["location"] = new { }
                }
            };
        }

        if (string.Equals(request.Sort, "title", StringComparison.OrdinalIgnoreCase))
        {
            body["sort"] = new object[]
            {
                new Dictionary<string, object>
                {
                    ["title.keyword"] = new
                    {
                        order = "asc"
                    }
                }
            };
        }

        return body;
    }

    private static void AddTermFilter(
        List<object> filters,
        string fieldName,
        string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return;
        }

        filters.Add(new Dictionary<string, object>
        {
            ["term"] = new Dictionary<string, object>
            {
                [fieldName] = value
            }
        });
    }

    private static PagedResult<SearchResultItem> ParseSearchResponse(
        string responseBody,
        SearchRequest request)
    {
        using var jsonDocument = JsonDocument.Parse(responseBody);

        if (!jsonDocument.RootElement.TryGetProperty("hits", out var hitsElement))
        {
            return EmptyResult(request);
        }

        var totalCount = ReadTotalCount(hitsElement);

        if (!hitsElement.TryGetProperty("hits", out var hitItemsElement) ||
            hitItemsElement.ValueKind != JsonValueKind.Array)
        {
            return new PagedResult<SearchResultItem>(
                Items: [],
                Page: request.Page,
                PageSize: request.PageSize,
                TotalCount: totalCount);
        }

        var items = new List<SearchResultItem>();

        foreach (var hitElement in hitItemsElement.EnumerateArray())
        {
            if (!hitElement.TryGetProperty("_source", out var sourceElement))
            {
                continue;
            }

            var sourceId = ReadGuid(sourceElement, "sourceId");

            if (sourceId == Guid.Empty)
            {
                continue;
            }

            var content = ReadString(sourceElement, "content") ?? string.Empty;
            var highlights = ReadHighlights(hitElement);

            items.Add(new SearchResultItem(
                SourceId: sourceId,
                SourceType: ReadString(sourceElement, "sourceType") ?? string.Empty,
                Title: ReadString(sourceElement, "title") ?? string.Empty,
                ContentPreview: CreateContentPreview(content),
                Tags: ReadStringArray(sourceElement, "tags"),
                Category: ReadString(sourceElement, "category"),
                Department: ReadString(sourceElement, "department"),
                JobTitle: ReadString(sourceElement, "jobTitle"),
                Location: ReadString(sourceElement, "location"),
                Score: ReadScore(hitElement),
                Highlights: highlights));
        }

        return new PagedResult<SearchResultItem>(
            Items: items,
            Page: request.Page,
            PageSize: request.PageSize,
            TotalCount: totalCount);
    }

    private static int ReadTotalCount(JsonElement hitsElement)
    {
        if (!hitsElement.TryGetProperty("total", out var totalElement))
        {
            return 0;
        }

        if (totalElement.ValueKind == JsonValueKind.Number &&
            totalElement.TryGetInt32(out var totalNumber))
        {
            return totalNumber;
        }

        if (totalElement.ValueKind == JsonValueKind.Object &&
            totalElement.TryGetProperty("value", out var valueElement) &&
            valueElement.TryGetInt32(out var totalValue))
        {
            return totalValue;
        }

        return 0;
    }

    private static double ReadScore(JsonElement hitElement)
    {
        if (!hitElement.TryGetProperty("_score", out var scoreElement) ||
            scoreElement.ValueKind == JsonValueKind.Null)
        {
            return 0;
        }

        return scoreElement.TryGetDouble(out var score)
            ? score
            : 0;
    }

    private static Guid ReadGuid(JsonElement element, string propertyName)
    {
        var value = ReadString(element, propertyName);

        return Guid.TryParse(value, out var guid)
            ? guid
            : Guid.Empty;
    }

    private static string? ReadString(JsonElement element, string propertyName)
    {
        if (!element.TryGetProperty(propertyName, out var propertyElement) ||
            propertyElement.ValueKind == JsonValueKind.Null)
        {
            return null;
        }

        return propertyElement.ValueKind == JsonValueKind.String
            ? propertyElement.GetString()
            : propertyElement.ToString();
    }

    private static List<string> ReadStringArray(
        JsonElement element,
        string propertyName)
    {
        if (!element.TryGetProperty(propertyName, out var propertyElement) ||
            propertyElement.ValueKind != JsonValueKind.Array)
        {
            return [];
        }

        return propertyElement
            .EnumerateArray()
            .Where(item => item.ValueKind == JsonValueKind.String)
            .Select(item => item.GetString())
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Select(value => value!)
            .ToList();
    }

    private static List<string> ReadHighlights(JsonElement hitElement)
    {
        if (!hitElement.TryGetProperty("highlight", out var highlightElement) ||
            highlightElement.ValueKind != JsonValueKind.Object)
        {
            return [];
        }

        var highlights = new List<string>();

        foreach (var highlightedField in highlightElement.EnumerateObject())
        {
            if (highlightedField.Value.ValueKind != JsonValueKind.Array)
            {
                continue;
            }

            foreach (var highlightedValue in highlightedField.Value.EnumerateArray())
            {
                if (highlightedValue.ValueKind != JsonValueKind.String)
                {
                    continue;
                }

                var value = highlightedValue.GetString();

                if (!string.IsNullOrWhiteSpace(value))
                {
                    highlights.Add(value);
                }
            }
        }

        return highlights;
    }

    private static string CreateContentPreview(string content)
    {
        if (string.IsNullOrWhiteSpace(content))
        {
            return string.Empty;
        }

        var normalizedContent = content
            .ReplaceLineEndings(" ")
            .Trim();

        return normalizedContent.Length <= ContentPreviewMaxLength
            ? normalizedContent
            : $"{normalizedContent[..ContentPreviewMaxLength]}...";
    }

    private static string? NormalizeOptionalValue(string? value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? null
            : value.Trim();
    }

    private static PagedResult<SearchResultItem> EmptyResult(SearchRequest request)
    {
        return new PagedResult<SearchResultItem>(
            Items: [],
            Page: request.Page,
            PageSize: request.PageSize,
            TotalCount: 0);
    }
}