using SearchAiAssistant.Application.Indexing;
using SearchAiAssistant.Application.Search;
using SearchAiAssistant.Domain.Entities;
using SearchAiAssistant.Tests.Unit.TestDoubles;
using Xunit;
using DocumentEntity = SearchAiAssistant.Domain.Entities.Document;

namespace SearchAiAssistant.Tests.Unit.Application;

public sealed class IndexManagementServiceTests
{
    [Fact]
    public async Task RebuildAsync_ShouldRecreateIndexAndIndexAllEmployeesAndDocuments()
    {
        var employeeRepository = new InMemoryEmployeeRepository();
        var documentRepository = new InMemoryDocumentRepository();
        var indexingService = new FakeIndexingService();

        var employee1 = CreateEmployee("Anna", "Novak", "anna@example.com");
        var employee2 = CreateEmployee("Jan", "Svoboda", "jan@example.com");
        var document = CreateDocument("Employee Benefits Policy");

        employeeRepository.AddExisting(employee1);
        employeeRepository.AddExisting(employee2);
        await documentRepository.AddAsync(document);

        var service = new IndexManagementService(
            employeeRepository,
            documentRepository,
            indexingService);

        var response = await service.RebuildAsync();

        Assert.Equal(1, indexingService.RecreateIndexCallCount);
        Assert.Equal(2, response.EmployeesIndexed);
        Assert.Equal(1, response.DocumentsIndexed);
        Assert.Equal(3, response.TotalIndexed);
        Assert.Equal([employee1.Id, employee2.Id], indexingService.IndexedEmployeeIds);
        Assert.Equal([document.Id], indexingService.IndexedDocumentIds);
    }

    [Fact]
    public async Task IndexEmployeeAsync_WithExistingEmployee_ShouldIndexAndReturnSuccess()
    {
        var employeeRepository = new InMemoryEmployeeRepository();
        var documentRepository = new InMemoryDocumentRepository();
        var indexingService = new FakeIndexingService();

        var employee = CreateEmployee("Anna", "Novak", "anna@example.com");

        employeeRepository.AddExisting(employee);

        var service = new IndexManagementService(
            employeeRepository,
            documentRepository,
            indexingService);

        var response = await service.IndexEmployeeAsync(employee.Id);

        Assert.True(response.Indexed);
        Assert.Equal(employee.Id, response.SourceId);
        Assert.Equal(SearchSourceTypes.Employee, response.SourceType);
        Assert.Equal([employee.Id], indexingService.IndexedEmployeeIds);
    }

    [Fact]
    public async Task IndexEmployeeAsync_WithMissingEmployee_ShouldReturnNotIndexed()
    {
        var service = new IndexManagementService(
            new InMemoryEmployeeRepository(),
            new InMemoryDocumentRepository(),
            new FakeIndexingService());

        var employeeId = Guid.NewGuid();

        var response = await service.IndexEmployeeAsync(employeeId);

        Assert.False(response.Indexed);
        Assert.Equal(employeeId, response.SourceId);
        Assert.Equal(SearchSourceTypes.Employee, response.SourceType);
        Assert.Contains("was not found", response.Message);
    }

    [Fact]
    public async Task IndexDocumentAsync_WithExistingDocument_ShouldIndexAndReturnSuccess()
    {
        var employeeRepository = new InMemoryEmployeeRepository();
        var documentRepository = new InMemoryDocumentRepository();
        var indexingService = new FakeIndexingService();

        var document = CreateDocument("Remote Work Policy");

        await documentRepository.AddAsync(document);

        var service = new IndexManagementService(
            employeeRepository,
            documentRepository,
            indexingService);

        var response = await service.IndexDocumentAsync(document.Id);

        Assert.True(response.Indexed);
        Assert.Equal(document.Id, response.SourceId);
        Assert.Equal(SearchSourceTypes.Document, response.SourceType);
        Assert.Equal([document.Id], indexingService.IndexedDocumentIds);
    }

    [Fact]
    public async Task IndexDocumentAsync_WithMissingDocument_ShouldReturnNotIndexed()
    {
        var service = new IndexManagementService(
            new InMemoryEmployeeRepository(),
            new InMemoryDocumentRepository(),
            new FakeIndexingService());

        var documentId = Guid.NewGuid();

        var response = await service.IndexDocumentAsync(documentId);

        Assert.False(response.Indexed);
        Assert.Equal(documentId, response.SourceId);
        Assert.Equal(SearchSourceTypes.Document, response.SourceType);
        Assert.Contains("was not found", response.Message);
    }

    private static Employee CreateEmployee(
        string firstName,
        string lastName,
        string email)
    {
        return new Employee(
            id: Guid.NewGuid(),
            firstName: firstName,
            lastName: lastName,
            email: email,
            department: "Engineering",
            jobTitle: "Backend Developer",
            skills: ["C#", ".NET"],
            location: "Prague",
            createdAt: DateTimeOffset.Parse("2026-05-05T10:00:00Z"));
    }

    private static DocumentEntity CreateDocument(string title)
    {
        return new DocumentEntity(
            id: Guid.NewGuid(),
            title: title,
            content: $"{title} content.",
            category: "HR Policy",
            tags: ["policy"],
            createdAt: DateTimeOffset.Parse("2026-05-05T10:00:00Z"));
    }
}