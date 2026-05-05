using SearchAiAssistant.Domain.Entities;
using SearchAiAssistant.Infrastructure.Search.OpenSearch.Models;
using Xunit;
using DocumentEntity = SearchAiAssistant.Domain.Entities.Document;

namespace SearchAiAssistant.Tests.Unit.Infrastructure;

public sealed class SearchIndexDocumentFactoryTests
{
    [Fact]
    public void FromEmployee_ShouldCreateSearchIndexDocument()
    {
        var employee = new Employee(
            id: Guid.Parse("11111111-1111-1111-1111-111111111111"),
            firstName: "Anna",
            lastName: "Novak",
            email: "anna.novak@example.com",
            department: "Engineering",
            jobTitle: "Backend Developer",
            skills: ["C#", ".NET", "PostgreSQL"],
            location: "Prague",
            createdAt: DateTimeOffset.Parse("2026-05-05T10:00:00Z"));

        var indexedAt = DateTimeOffset.Parse("2026-05-05T11:00:00Z");

        var result = SearchIndexDocumentFactory.FromEmployee(employee, indexedAt);

        Assert.Equal("employee:11111111111111111111111111111111", result.Id);
        Assert.Equal(SearchIndexDocumentTypes.Employee, result.SourceType);
        Assert.Equal(employee.Id, result.SourceId);
        Assert.Equal("Anna Novak", result.Title);
        Assert.Contains("Department: Engineering", result.Content);
        Assert.Contains("Job title: Backend Developer", result.Content);
        Assert.Equal(["C#", ".NET", "PostgreSQL"], result.Tags);
        Assert.Equal("Engineering", result.Department);
        Assert.Equal("Backend Developer", result.JobTitle);
        Assert.Equal("Prague", result.Location);
        Assert.Equal("anna.novak@example.com", result.EmployeeEmail);
        Assert.Equal("Anna Novak", result.EmployeeFullName);
        Assert.Equal(indexedAt, result.IndexedAt);
    }

    [Fact]
    public void FromDocument_ShouldCreateSearchIndexDocument()
    {
        var document = new DocumentEntity(
            id: Guid.Parse("22222222-2222-2222-2222-222222222222"),
            title: "Employee Benefits Policy",
            content: "Employees receive benefits including learning budget.",
            category: "HR Policy",
            tags: ["benefits", "policy"],
            createdAt: DateTimeOffset.Parse("2026-05-05T10:00:00Z"));

        var indexedAt = DateTimeOffset.Parse("2026-05-05T11:00:00Z");

        var result = SearchIndexDocumentFactory.FromDocument(document, indexedAt);

        Assert.Equal("document:22222222222222222222222222222222", result.Id);
        Assert.Equal(SearchIndexDocumentTypes.Document, result.SourceType);
        Assert.Equal(document.Id, result.SourceId);
        Assert.Equal("Employee Benefits Policy", result.Title);
        Assert.Equal("Employees receive benefits including learning budget.", result.Content);
        Assert.Equal(["benefits", "policy"], result.Tags);
        Assert.Equal("HR Policy", result.Category);
        Assert.Null(result.Department);
        Assert.Null(result.JobTitle);
        Assert.Null(result.Location);
        Assert.Null(result.EmployeeEmail);
        Assert.Null(result.EmployeeFullName);
        Assert.Equal(indexedAt, result.IndexedAt);
    }

    [Fact]
    public void CreateDocumentId_WithValidSource_ShouldReturnStableId()
    {
        var sourceId = Guid.Parse("33333333-3333-3333-3333-333333333333");

        var result = SearchIndexDocumentFactory.CreateDocumentId(
            SearchIndexDocumentTypes.Document,
            sourceId);

        Assert.Equal("document:33333333333333333333333333333333", result);
    }

    [Fact]
    public void CreateDocumentId_WithEmptySourceType_ShouldThrowArgumentException()
    {
        Assert.Throws<ArgumentException>(() =>
            SearchIndexDocumentFactory.CreateDocumentId(
                "",
                Guid.NewGuid()));
    }

    [Fact]
    public void CreateDocumentId_WithEmptySourceId_ShouldThrowArgumentException()
    {
        Assert.Throws<ArgumentException>(() =>
            SearchIndexDocumentFactory.CreateDocumentId(
                SearchIndexDocumentTypes.Document,
                Guid.Empty));
    }
}