using Xunit;
using DocumentEntity = SearchAiAssistant.Domain.Entities.Document;

namespace SearchAiAssistant.Tests.Unit.Domain;

public sealed class DocumentTests
{
    [Fact]
    public void Constructor_WithValidData_ShouldCreateDocument()
    {
        var createdAt = DateTimeOffset.Parse("2026-05-05T10:00:00Z");

        var document = new DocumentEntity(
            id: Guid.NewGuid(),
            title: " Remote Work Policy ",
            content: " Employees may work remotely. ",
            category: " HR Policy ",
            tags: ["remote-work", "policy", "remote-work", " benefits "],
            createdAt: createdAt);

        Assert.Equal("Remote Work Policy", document.Title);
        Assert.Equal("Employees may work remotely.", document.Content);
        Assert.Equal("HR Policy", document.Category);
        Assert.Equal(["remote-work", "policy", "benefits"], document.Tags);
        Assert.Equal(createdAt, document.CreatedAt);
        Assert.Equal(createdAt, document.UpdatedAt);
    }

    [Fact]
    public void Constructor_WithEmptyTitle_ShouldThrowArgumentException()
    {
        var exception = Assert.Throws<ArgumentException>(() =>
            new DocumentEntity(
                id: Guid.NewGuid(),
                title: "",
                content: "Employees may work remotely.",
                category: "HR Policy",
                tags: ["policy"],
                createdAt: DateTimeOffset.UtcNow));

        Assert.Contains("title is required", exception.Message);
    }

    [Fact]
    public void Constructor_WithEmptyContent_ShouldThrowArgumentException()
    {
        var exception = Assert.Throws<ArgumentException>(() =>
            new DocumentEntity(
                id: Guid.NewGuid(),
                title: "Remote Work Policy",
                content: "",
                category: "HR Policy",
                tags: ["policy"],
                createdAt: DateTimeOffset.UtcNow));

        Assert.Contains("content is required", exception.Message);
    }

    [Fact]
    public void Update_WithValidData_ShouldUpdateDocumentAndUpdatedAt()
    {
        var createdAt = DateTimeOffset.Parse("2026-05-05T10:00:00Z");
        var updatedAt = DateTimeOffset.Parse("2026-05-06T10:00:00Z");

        var document = new DocumentEntity(
            id: Guid.NewGuid(),
            title: "Remote Work Policy",
            content: "Employees may work remotely.",
            category: "HR Policy",
            tags: ["remote-work"],
            createdAt: createdAt);

        document.Update(
            title: "Hybrid Work Policy",
            content: "Employees may work remotely up to three days per week.",
            category: "Company Policy",
            tags: ["hybrid-work", "policy"],
            updatedAt: updatedAt);

        Assert.Equal("Hybrid Work Policy", document.Title);
        Assert.Equal("Employees may work remotely up to three days per week.", document.Content);
        Assert.Equal("Company Policy", document.Category);
        Assert.Equal(["hybrid-work", "policy"], document.Tags);
        Assert.Equal(createdAt, document.CreatedAt);
        Assert.Equal(updatedAt, document.UpdatedAt);
    }
}