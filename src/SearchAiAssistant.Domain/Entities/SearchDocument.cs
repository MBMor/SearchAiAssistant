using SearchAiAssistant.Domain.Common;
using SearchAiAssistant.Domain.Enums;

namespace SearchAiAssistant.Domain.Entities;

public sealed class SearchDocument : Entity
{
    public SearchDocumentSourceType SourceType { get; private set; }

    public Guid SourceId { get; private set; }

    public string Title { get; private set; } = string.Empty;

    public string Content { get; private set; } = string.Empty;

    public List<string> Tags { get; private set; } = [];

    public string? Category { get; private set; }

    public string? Department { get; private set; }

    public DateTimeOffset IndexedAt { get; private set; }

    private SearchDocument()
    {
    }

    public SearchDocument(
        Guid id,
        SearchDocumentSourceType sourceType,
        Guid sourceId,
        string title,
        string content,
        IEnumerable<string>? tags,
        string? category,
        string? department,
        DateTimeOffset indexedAt)
        : base(id)
    {
        if (sourceId == Guid.Empty)
        {
            throw new ArgumentException("Source id cannot be empty.", nameof(sourceId));
        }

        SourceType = sourceType;
        SourceId = sourceId;
        Title = Guard.Required(title, nameof(title), maxLength: 250);
        Content = Guard.Required(content, nameof(content), maxLength: 20_000);
        Tags = Guard.NormalizeStringList(tags, nameof(tags), maxItemLength: 100);
        Category = string.IsNullOrWhiteSpace(category)
            ? null
            : Guard.Required(category, nameof(category), maxLength: 150);
        Department = string.IsNullOrWhiteSpace(department)
            ? null
            : Guard.Required(department, nameof(department), maxLength: 150);
        IndexedAt = indexedAt;
    }

    public void MarkIndexed(DateTimeOffset indexedAt)
    {
        IndexedAt = indexedAt;
    }
}