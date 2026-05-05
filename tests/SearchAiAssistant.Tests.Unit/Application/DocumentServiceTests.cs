using Microsoft.Extensions.Options;
using SearchAiAssistant.Application.Common.Options;
using SearchAiAssistant.Application.Documents;
using SearchAiAssistant.Tests.Unit.TestDoubles;
using Xunit;
using DocumentEntity = SearchAiAssistant.Domain.Entities.Document;

namespace SearchAiAssistant.Tests.Unit.Application;

public sealed class DocumentServiceTests
{
    [Fact]
    public async Task CreateAsync_WithValidRequest_ShouldCreateDocumentSaveAndIndex()
    {
        var repository = new InMemoryDocumentRepository();
        var unitOfWork = new FakeUnitOfWork();
        var indexingService = new FakeIndexingService();

        var service = CreateService(repository, unitOfWork, indexingService);

        var response = await service.CreateAsync(new CreateDocumentRequest(
            Title: "Employee Benefits Policy",
            Content: "Employees receive benefits including remote work and learning budget.",
            Category: "HR Policy",
            Tags: ["benefits", "policy"]));

        Assert.NotEqual(Guid.Empty, response.Id);
        Assert.Equal("Employee Benefits Policy", response.Title);
        Assert.Equal("HR Policy", response.Category);
        Assert.Equal(["benefits", "policy"], response.Tags);
        Assert.Equal(1, unitOfWork.SaveChangesCallCount);
        Assert.Equal([response.Id], indexingService.IndexedDocumentIds);
    }

    [Fact]
    public async Task ListAsync_ShouldNormalizePaginationAndApplyMaxPageSize()
    {
        var repository = new InMemoryDocumentRepository();

        await repository.AddAsync(CreateDocument("Remote Work Policy", "HR Policy", ["remote-work"]));
        await repository.AddAsync(CreateDocument("Employee Benefits Policy", "HR Policy", ["benefits"]));
        await repository.AddAsync(CreateDocument("Security Policy", "Security", ["security"]));

        var service = CreateService(
            repository,
            new FakeUnitOfWork(),
            new FakeIndexingService(),
            new PaginationOptions
            {
                DefaultPageSize = 2,
                MaxPageSize = 2
            });

        var result = await service.ListAsync(new DocumentListRequest(
            Category: "HR Policy",
            Page: 0,
            PageSize: 100));

        Assert.Equal(1, result.Page);
        Assert.Equal(2, result.PageSize);
        Assert.Equal(2, result.TotalCount);
        Assert.Equal(1, result.TotalPages);
        Assert.Equal(2, result.Items.Count);
        Assert.All(result.Items, item => Assert.Equal("HR Policy", item.Category));
    }

    [Fact]
    public async Task UpdateAsync_WithExistingDocument_ShouldUpdateSaveAndIndex()
    {
        var repository = new InMemoryDocumentRepository();
        var unitOfWork = new FakeUnitOfWork();
        var indexingService = new FakeIndexingService();

        var document = CreateDocument("Remote Work Policy", "HR Policy", ["remote-work"]);

        await repository.AddAsync(document);

        var service = CreateService(repository, unitOfWork, indexingService);

        var response = await service.UpdateAsync(
            document.Id,
            new UpdateDocumentRequest(
                Title: "Hybrid Work Policy",
                Content: "Employees may work remotely up to three days per week.",
                Category: "Company Policy",
                Tags: ["hybrid-work", "policy"]));

        Assert.NotNull(response);
        Assert.Equal(document.Id, response.Id);
        Assert.Equal("Hybrid Work Policy", response.Title);
        Assert.Equal("Company Policy", response.Category);
        Assert.Equal(["hybrid-work", "policy"], response.Tags);
        Assert.Equal(1, unitOfWork.SaveChangesCallCount);
        Assert.Equal([document.Id], indexingService.IndexedDocumentIds);
    }

    [Fact]
    public async Task UpdateAsync_WithMissingDocument_ShouldReturnNull()
    {
        var service = CreateService(
            new InMemoryDocumentRepository(),
            new FakeUnitOfWork(),
            new FakeIndexingService());

        var response = await service.UpdateAsync(
            Guid.NewGuid(),
            new UpdateDocumentRequest(
                Title: "Missing",
                Content: "Missing content",
                Category: "Missing",
                Tags: ["missing"]));

        Assert.Null(response);
    }

    [Fact]
    public async Task DeleteAsync_WithExistingDocument_ShouldRemoveSaveAndRemoveFromIndex()
    {
        var repository = new InMemoryDocumentRepository();
        var unitOfWork = new FakeUnitOfWork();
        var indexingService = new FakeIndexingService();

        var document = CreateDocument("Remote Work Policy", "HR Policy", ["remote-work"]);

        await repository.AddAsync(document);

        var service = CreateService(repository, unitOfWork, indexingService);

        var deleted = await service.DeleteAsync(document.Id);

        Assert.True(deleted);
        Assert.Null(await repository.GetByIdAsync(document.Id));
        Assert.Equal(1, unitOfWork.SaveChangesCallCount);
        Assert.Equal([document.Id], indexingService.RemovedDocumentIds);
    }

    [Fact]
    public async Task DeleteAsync_WithMissingDocument_ShouldReturnFalse()
    {
        var service = CreateService(
            new InMemoryDocumentRepository(),
            new FakeUnitOfWork(),
            new FakeIndexingService());

        var deleted = await service.DeleteAsync(Guid.NewGuid());

        Assert.False(deleted);
    }

    private static DocumentService CreateService(
        InMemoryDocumentRepository repository,
        FakeUnitOfWork unitOfWork,
        FakeIndexingService indexingService,
        PaginationOptions? paginationOptions = null)
    {
        return new DocumentService(
            repository,
            new FixedDateTimeProvider(DateTimeOffset.Parse("2026-05-05T10:00:00Z")),
            unitOfWork,
            indexingService,
            Options.Create(paginationOptions ?? new PaginationOptions
            {
                DefaultPageSize = 20,
                MaxPageSize = 100
            }));
    }

    private static DocumentEntity CreateDocument(
        string title,
        string category,
        IReadOnlyList<string> tags)
    {
        return new DocumentEntity(
            id: Guid.NewGuid(),
            title: title,
            content: $"{title} content.",
            category: category,
            tags: tags,
            createdAt: DateTimeOffset.Parse("2026-05-05T10:00:00Z"));
    }
}