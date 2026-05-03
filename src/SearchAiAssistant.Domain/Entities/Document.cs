using SearchAiAssistant.Domain.Common;

namespace SearchAiAssistant.Domain.Entities;

public sealed class Document : Entity
{
    public string Title { get; private set; } = string.Empty;

    public string Content { get; private set; } = string.Empty;

    public string Category { get; private set; } = string.Empty;

    public List<string> Tags { get; private set; } = [];

    public DateTimeOffset CreatedAt { get; private set; }

    public DateTimeOffset UpdatedAt { get; private set; }

    private Document()
    {
    }

    public Document(
        Guid id,
        string title,
        string content,
        string category,
        IEnumerable<string>? tags,
        DateTimeOffset createdAt)
        : base(id)
    {
        Title = Guard.Required(title, nameof(title), maxLength: 250);
        Content = Guard.Required(content, nameof(content), maxLength: 20_000);
        Category = Guard.Required(category, nameof(category), maxLength: 150);
        Tags = Guard.NormalizeStringList(tags, nameof(tags), maxItemLength: 100);
        CreatedAt = createdAt;
        UpdatedAt = createdAt;
    }

    public void Update(
        string title,
        string content,
        string category,
        IEnumerable<string>? tags,
        DateTimeOffset updatedAt)
    {
        Title = Guard.Required(title, nameof(title), maxLength: 250);
        Content = Guard.Required(content, nameof(content), maxLength: 20_000);
        Category = Guard.Required(category, nameof(category), maxLength: 150);
        Tags = Guard.NormalizeStringList(tags, nameof(tags), maxItemLength: 100);
        UpdatedAt = updatedAt;
    }
}