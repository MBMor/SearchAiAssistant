using SearchAiAssistant.Domain.Entities;
using DocumentEntity = SearchAiAssistant.Domain.Entities.Document;

namespace SearchAiAssistant.Infrastructure.Search.OpenSearch.Models;

public static class SearchIndexDocumentFactory
{
    public static SearchIndexDocument FromEmployee(
        Employee employee,
        DateTimeOffset indexedAt)
    {
        ArgumentNullException.ThrowIfNull(employee);

        var fullName = $"{employee.FirstName} {employee.LastName}".Trim();

        return new SearchIndexDocument
        {
            Id = CreateDocumentId(SearchIndexDocumentTypes.Employee, employee.Id),
            SourceType = SearchIndexDocumentTypes.Employee,
            SourceId = employee.Id,

            Title = fullName,
            Content = BuildEmployeeContent(employee),

            Tags = employee.Skills,
            Department = employee.Department,
            JobTitle = employee.JobTitle,
            Location = employee.Location,
            EmployeeEmail = employee.Email,
            EmployeeFullName = fullName,

            IndexedAt = indexedAt
        };
    }

    public static SearchIndexDocument FromDocument(
        DocumentEntity document,
        DateTimeOffset indexedAt)
    {
        ArgumentNullException.ThrowIfNull(document);

        return new SearchIndexDocument
        {
            Id = CreateDocumentId(SearchIndexDocumentTypes.Document, document.Id),
            SourceType = SearchIndexDocumentTypes.Document,
            SourceId = document.Id,

            Title = document.Title,
            Content = document.Content,

            Tags = document.Tags,
            Category = document.Category,

            IndexedAt = indexedAt
        };
    }

    public static string CreateDocumentId(string sourceType, Guid sourceId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sourceType);

        if (sourceId == Guid.Empty)
        {
            throw new ArgumentException("Source id cannot be empty.", nameof(sourceId));
        }

        return $"{sourceType}:{sourceId:N}";
    }

    private static string BuildEmployeeContent(Employee employee)
    {
        var skills = employee.Skills.Count == 0
            ? "No skills listed"
            : string.Join(", ", employee.Skills);

        return string.Join(
            Environment.NewLine,
            $"Name: {employee.FirstName} {employee.LastName}",
            $"Email: {employee.Email}",
            $"Department: {employee.Department}",
            $"Job title: {employee.JobTitle}",
            $"Skills: {skills}",
            $"Location: {employee.Location}");
    }
}