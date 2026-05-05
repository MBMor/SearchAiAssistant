using Microsoft.Extensions.Options;
using SearchAiAssistant.Application.Common.Exceptions;
using SearchAiAssistant.Application.Common.Options;
using SearchAiAssistant.Application.Employees;
using SearchAiAssistant.Domain.Entities;
using SearchAiAssistant.Tests.Unit.TestDoubles;
using Xunit;

namespace SearchAiAssistant.Tests.Unit.Application;

public sealed class EmployeeServiceTests
{
    [Fact]
    public async Task CreateAsync_WithValidRequest_ShouldCreateEmployeeSaveAndIndex()
    {
        var repository = new InMemoryEmployeeRepository();
        var unitOfWork = new FakeUnitOfWork();
        var indexingService = new FakeIndexingService();

        var service = CreateService(repository, unitOfWork, indexingService);

        var response = await service.CreateAsync(new CreateEmployeeRequest(
            FirstName: "Anna",
            LastName: "Novak",
            Email: " ANNA.NOVAK@EXAMPLE.COM ",
            Department: "Engineering",
            JobTitle: "Backend Developer",
            Skills: ["C#", ".NET"],
            Location: "Prague"));

        Assert.NotEqual(Guid.Empty, response.Id);
        Assert.Equal("anna.novak@example.com", response.Email);
        Assert.Equal("Engineering", response.Department);
        Assert.Equal(["C#", ".NET"], response.Skills);
        Assert.Single(repository.Employees);
        Assert.Equal(1, unitOfWork.SaveChangesCallCount);
        Assert.Equal([response.Id], indexingService.IndexedEmployeeIds);
    }

    [Fact]
    public async Task CreateAsync_WithDuplicateEmail_ShouldThrowDuplicateEmployeeEmailException()
    {
        var repository = new InMemoryEmployeeRepository();

        repository.AddExisting(new Employee(
            id: Guid.NewGuid(),
            firstName: "Anna",
            lastName: "Novak",
            email: "anna.novak@example.com",
            department: "Engineering",
            jobTitle: "Backend Developer",
            skills: ["C#"],
            location: "Prague",
            createdAt: DateTimeOffset.UtcNow));

        var service = CreateService(
            repository,
            new FakeUnitOfWork(),
            new FakeIndexingService());

        await Assert.ThrowsAsync<DuplicateEmployeeEmailException>(() =>
            service.CreateAsync(new CreateEmployeeRequest(
                FirstName: "Other",
                LastName: "Person",
                Email: "ANNA.NOVAK@EXAMPLE.COM",
                Department: "Engineering",
                JobTitle: "Developer",
                Skills: ["C#"],
                Location: "Prague")));
    }

    [Fact]
    public async Task ListAsync_ShouldNormalizePaginationAndApplyMaxPageSize()
    {
        var repository = new InMemoryEmployeeRepository();

        repository.AddExisting(CreateEmployee("Anna", "Novak", "anna@example.com", "Engineering", "Backend Developer", ["C#"]));
        repository.AddExisting(CreateEmployee("Jan", "Svoboda", "jan@example.com", "Engineering", "Backend Developer", [".NET"]));
        repository.AddExisting(CreateEmployee("Eva", "Kralova", "eva@example.com", "HR", "HR Specialist", ["Recruiting"]));

        var service = CreateService(
            repository,
            new FakeUnitOfWork(),
            new FakeIndexingService(),
            new PaginationOptions
            {
                DefaultPageSize = 2,
                MaxPageSize = 2
            });

        var result = await service.ListAsync(new EmployeeListRequest(
            Department: "Engineering",
            Page: 0,
            PageSize: 100));

        Assert.Equal(1, result.Page);
        Assert.Equal(2, result.PageSize);
        Assert.Equal(2, result.TotalCount);
        Assert.Equal(1, result.TotalPages);
        Assert.Equal(2, result.Items.Count);
        Assert.All(result.Items, item => Assert.Equal("Engineering", item.Department));
    }

    [Fact]
    public async Task UpdateAsync_WithExistingEmployee_ShouldUpdateSaveAndIndex()
    {
        var repository = new InMemoryEmployeeRepository();
        var unitOfWork = new FakeUnitOfWork();
        var indexingService = new FakeIndexingService();

        var employee = CreateEmployee(
            "Anna",
            "Novak",
            "anna@example.com",
            "Engineering",
            "Backend Developer",
            ["C#"]);

        repository.AddExisting(employee);

        var service = CreateService(repository, unitOfWork, indexingService);

        var response = await service.UpdateAsync(
            employee.Id,
            new UpdateEmployeeRequest(
                FirstName: "Anna",
                LastName: "Novak",
                Email: "anna.updated@example.com",
                Department: "Platform",
                JobTitle: "Senior Backend Developer",
                Skills: ["C#", ".NET"],
                Location: "Brno"));

        Assert.NotNull(response);
        Assert.Equal("anna.updated@example.com", response.Email);
        Assert.Equal("Platform", response.Department);
        Assert.Equal("Senior Backend Developer", response.JobTitle);
        Assert.Equal(1, unitOfWork.SaveChangesCallCount);
        Assert.Equal([employee.Id], indexingService.IndexedEmployeeIds);
    }

    [Fact]
    public async Task DeleteAsync_WithExistingEmployee_ShouldRemoveSaveAndRemoveFromIndex()
    {
        var repository = new InMemoryEmployeeRepository();
        var unitOfWork = new FakeUnitOfWork();
        var indexingService = new FakeIndexingService();

        var employee = CreateEmployee(
            "Anna",
            "Novak",
            "anna@example.com",
            "Engineering",
            "Backend Developer",
            ["C#"]);

        repository.AddExisting(employee);

        var service = CreateService(repository, unitOfWork, indexingService);

        var deleted = await service.DeleteAsync(employee.Id);

        Assert.True(deleted);
        Assert.Empty(repository.Employees);
        Assert.Equal(1, unitOfWork.SaveChangesCallCount);
        Assert.Equal([employee.Id], indexingService.RemovedEmployeeIds);
    }

    [Fact]
    public async Task DeleteAsync_WithMissingEmployee_ShouldReturnFalse()
    {
        var service = CreateService(
            new InMemoryEmployeeRepository(),
            new FakeUnitOfWork(),
            new FakeIndexingService());

        var deleted = await service.DeleteAsync(Guid.NewGuid());

        Assert.False(deleted);
    }

    private static EmployeeService CreateService(
        InMemoryEmployeeRepository repository,
        FakeUnitOfWork unitOfWork,
        FakeIndexingService indexingService,
        PaginationOptions? paginationOptions = null)
    {
        return new EmployeeService(
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

    private static Employee CreateEmployee(
        string firstName,
        string lastName,
        string email,
        string department,
        string jobTitle,
        IReadOnlyList<string> skills)
    {
        return new Employee(
            id: Guid.NewGuid(),
            firstName: firstName,
            lastName: lastName,
            email: email,
            department: department,
            jobTitle: jobTitle,
            skills: skills,
            location: "Prague",
            createdAt: DateTimeOffset.Parse("2026-05-05T10:00:00Z"));
    }
}