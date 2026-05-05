using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using SearchAiAssistant.Application.Abstractions.Search;
using SearchAiAssistant.Application.Assistant;
using SearchAiAssistant.Application.Common.Exceptions;
using SearchAiAssistant.Application.Common.Options;
using SearchAiAssistant.Application.Common.Pagination;
using SearchAiAssistant.Application.Search;
using SearchAiAssistant.Infrastructure.Ai;
using Xunit;

namespace SearchAiAssistant.Tests.Unit.Infrastructure;

public sealed class LocalRuleBasedAiAssistantTests
{
    [Fact]
    public async Task AskAsync_WithEmptyQuestion_ShouldThrowValidationException()
    {
        var assistant = CreateAssistant([]);

        await Assert.ThrowsAsync<ValidationException>(() =>
            assistant.AskAsync(new AskAssistantRequest(
                Question: "",
                MaxSources: 5)));
    }

    [Fact]
    public async Task AskAsync_WithNoSources_ShouldReturnNotEnoughInformation()
    {
        var assistant = CreateAssistant([]);

        var response = await assistant.AskAsync(new AskAssistantRequest(
            Question: "What is the company car policy?",
            MaxSources: 5));

        Assert.False(response.HasEnoughInformation);
        Assert.Empty(response.Sources);
        Assert.Contains("not have enough information", response.Answer);
    }

    [Fact]
    public async Task AskAsync_WithRelevantSources_ShouldReturnAnswerAndSources()
    {
        var sourceId = Guid.NewGuid();

        var assistant = CreateAssistant([
            new SearchResultItem(
                SourceId: sourceId,
                SourceType: SearchSourceTypes.Document,
                Title: "Employee Benefits Policy",
                ContentPreview: "Employees receive benefits including learning budget.",
                Tags: ["benefits"],
                Category: "HR Policy",
                Department: null,
                JobTitle: null,
                Location: null,
                Score: 2.5,
                Highlights: ["Employees receive <mark>benefits</mark> including learning budget."])
        ]);

        var response = await assistant.AskAsync(new AskAssistantRequest(
            Question: "What benefits do employees have?",
            MaxSources: 5));

        Assert.True(response.HasEnoughInformation);
        Assert.Single(response.Sources);
        Assert.Equal(sourceId, response.Sources[0].SourceId);
        Assert.Contains("Employee Benefits Policy", response.Answer);
        Assert.Contains("<mark>benefits</mark>", response.Answer);
    }

    [Fact]
    public async Task AskAsync_ShouldRespectConfiguredMaxRetrievedSources()
    {
        var sources = Enumerable
            .Range(1, 5)
            .Select(index => new SearchResultItem(
                SourceId: Guid.NewGuid(),
                SourceType: SearchSourceTypes.Document,
                Title: $"Policy {index}",
                ContentPreview: $"Policy {index} content.",
                Tags: ["policy"],
                Category: "HR Policy",
                Department: null,
                JobTitle: null,
                Location: null,
                Score: 1.0,
                Highlights: []))
            .ToList();

        var assistant = CreateAssistant(
            sources,
            options: new AiAssistantOptions
            {
                Provider = "Mock",
                MaxRetrievedSources = 2,
                MinimumScore = 0.1
            });

        var response = await assistant.AskAsync(new AskAssistantRequest(
            Question: "policy",
            MaxSources: 5));

        Assert.True(response.HasEnoughInformation);
        Assert.Equal(2, response.Sources.Count);
    }

    [Fact]
    public async Task AskAsync_ShouldIgnoreSourcesBelowMinimumScore()
    {
        var assistant = CreateAssistant(
            [
                new SearchResultItem(
                    SourceId: Guid.NewGuid(),
                    SourceType: SearchSourceTypes.Document,
                    Title: "Low Score Policy",
                    ContentPreview: "Low score content.",
                    Tags: ["policy"],
                    Category: "HR Policy",
                    Department: null,
                    JobTitle: null,
                    Location: null,
                    Score: 0.01,
                    Highlights: [])
            ],
            options: new AiAssistantOptions
            {
                Provider = "Mock",
                MaxRetrievedSources = 5,
                MinimumScore = 0.1
            });

        var response = await assistant.AskAsync(new AskAssistantRequest(
            Question: "policy",
            MaxSources: 5));

        Assert.False(response.HasEnoughInformation);
        Assert.Empty(response.Sources);
    }

    private static LocalRuleBasedAiAssistant CreateAssistant(
        IReadOnlyList<SearchResultItem> searchResults,
        AiAssistantOptions? options = null)
    {
        return new LocalRuleBasedAiAssistant(
            new FakeSearchService(searchResults),
            Options.Create(options ?? new AiAssistantOptions
            {
                Provider = "Mock",
                MaxRetrievedSources = 5,
                MinimumScore = 0.1
            }),
            NullLogger<LocalRuleBasedAiAssistant>.Instance);
    }

    private sealed class FakeSearchService(IReadOnlyList<SearchResultItem> items) : ISearchService
    {
        private readonly IReadOnlyList<SearchResultItem> _items = items;

        public Task<PagedResult<SearchResultItem>> SearchAsync(
            SearchRequest request,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(new PagedResult<SearchResultItem>(
                Items: _items.Take(request.PageSize).ToList(),
                Page: request.Page,
                PageSize: request.PageSize,
                TotalCount: _items.Count));
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
    }
}