using System.Text;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SearchAiAssistant.Application.Abstractions.Ai;
using SearchAiAssistant.Application.Abstractions.Search;
using SearchAiAssistant.Application.Assistant;
using SearchAiAssistant.Application.Common.Exceptions;
using SearchAiAssistant.Application.Common.Options;
using SearchAiAssistant.Application.Search;

namespace SearchAiAssistant.Infrastructure.Ai;

public sealed class LocalRuleBasedAiAssistant : IAiAssistant
{
    private const int DefaultMaxSources = 5;

    private readonly ISearchService _searchService;
    private readonly AiAssistantOptions _options;
    private readonly ILogger<LocalRuleBasedAiAssistant> _logger;

    public LocalRuleBasedAiAssistant(
        ISearchService searchService,
        IOptions<AiAssistantOptions> options,
        ILogger<LocalRuleBasedAiAssistant> logger)
    {
        _searchService = searchService;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<AssistantResponse> AskAsync(
        AskAssistantRequest request,
        CancellationToken cancellationToken = default)
    {
        var question = NormalizeQuestion(request.Question);
        var maxSources = NormalizeMaxSources(request.MaxSources);

        _logger.LogInformation(
            "Running local AI assistant query. Question: {Question}, MaxSources: {MaxSources}",
            question,
            maxSources);

        var searchRequest = new SearchRequest(
            Query: question,
            Page: 1,
            PageSize: maxSources);

        var searchResult = await _searchService.SearchAsync(
            searchRequest,
            cancellationToken);

        var relevantSources = searchResult.Items
            .Where(item => item.Score >= _options.MinimumScore)
            .Take(maxSources)
            .ToList();

        if (relevantSources.Count == 0)
        {
            return CreateNotEnoughInformationResponse(question);
        }

        var sources = relevantSources
            .Select(ToAssistantSource)
            .ToList();

        var answer = BuildAnswerFromSources(question, relevantSources);

        return new AssistantResponse(
            Question: question,
            Answer: answer,
            Sources: sources,
            HasEnoughInformation: true);
    }

    private int NormalizeMaxSources(int maxSources)
    {
        var configuredMaxSources = _options.MaxRetrievedSources > 0
            ? _options.MaxRetrievedSources
            : DefaultMaxSources;

        if (maxSources <= 0)
        {
            return configuredMaxSources;
        }

        return Math.Min(maxSources, configuredMaxSources);
    }

    private static string NormalizeQuestion(string? question)
    {
        if (string.IsNullOrWhiteSpace(question))
        {
            throw new ValidationException("Question is required.");
        }

        return question.Trim();
    }

    private static AssistantResponse CreateNotEnoughInformationResponse(string question)
    {
        return new AssistantResponse(
            Question: question,
            Answer: "I do not have enough information in the retrieved company knowledge base to answer this question.",
            Sources: [],
            HasEnoughInformation: false);
    }

    private static AssistantSource ToAssistantSource(SearchResultItem item)
    {
        return new AssistantSource(
            SourceId: item.SourceId,
            SourceType: item.SourceType,
            Title: item.Title,
            ContentPreview: item.ContentPreview,
            Score: item.Score,
            Highlights: item.Highlights);
    }

    private static string BuildAnswerFromSources(
        string question,
        IReadOnlyList<SearchResultItem> sources)
    {
        var builder = new StringBuilder();

        builder.AppendLine("Based on the retrieved company knowledge base sources, here is what I found.");
        builder.AppendLine();
        builder.AppendLine($"Question: {question}");
        builder.AppendLine();

        foreach (var source in sources)
        {
            builder.Append("- ");
            builder.Append(source.Title);

            if (!string.IsNullOrWhiteSpace(source.SourceType))
            {
                builder.Append(" [");
                builder.Append(source.SourceType);
                builder.Append(']');
            }

            builder.Append(": ");

            var bestSnippet = GetBestSnippet(source);

            builder.AppendLine(bestSnippet);
        }

        builder.AppendLine();
        builder.Append("This answer is limited to the sources returned by search.");

        return builder.ToString();
    }

    private static string GetBestSnippet(SearchResultItem source)
    {
        var highlight = source.Highlights
            .FirstOrDefault(value => !string.IsNullOrWhiteSpace(value));

        if (!string.IsNullOrWhiteSpace(highlight))
        {
            return highlight.Trim();
        }

        if (!string.IsNullOrWhiteSpace(source.ContentPreview))
        {
            return source.ContentPreview.Trim();
        }

        var metadata = BuildMetadataSnippet(source);

        return string.IsNullOrWhiteSpace(metadata)
            ? "No content preview is available."
            : metadata;
    }

    private static string BuildMetadataSnippet(SearchResultItem source)
    {
        var parts = new List<string>();

        if (!string.IsNullOrWhiteSpace(source.Department))
        {
            parts.Add($"Department: {source.Department}");
        }

        if (!string.IsNullOrWhiteSpace(source.JobTitle))
        {
            parts.Add($"Job title: {source.JobTitle}");
        }

        if (!string.IsNullOrWhiteSpace(source.Category))
        {
            parts.Add($"Category: {source.Category}");
        }

        if (!string.IsNullOrWhiteSpace(source.Location))
        {
            parts.Add($"Location: {source.Location}");
        }

        if (source.Tags.Count > 0)
        {
            parts.Add($"Tags: {string.Join(", ", source.Tags)}");
        }

        return string.Join("; ", parts);
    }
}