using SearchAiAssistant.Application.Abstractions.Authentication;
using SearchAiAssistant.Application.Abstractions.Indexing;
using SearchAiAssistant.Application.Abstractions.Persistence;
using SearchAiAssistant.Application.Common.Abstractions;
using SearchAiAssistant.Application.Common.Pagination;
using SearchAiAssistant.Application.Documents;
using SearchAiAssistant.Application.Employees;
using SearchAiAssistant.Domain.Entities;
using DocumentEntity = SearchAiAssistant.Domain.Entities.Document;

namespace SearchAiAssistant.Tests.Unit.TestDoubles;

internal sealed class FixedDateTimeProvider(DateTimeOffset utcNow) : IDateTimeProvider
{
    public DateTimeOffset UtcNow { get; } = utcNow;
}

internal sealed class FakePasswordHasher : IPasswordHasher
{
    public string HashPassword(string password)
    {
        return $"hashed:{password}";
    }

    public bool VerifyPassword(string password, string passwordHash)
    {
        return passwordHash == HashPassword(password);
    }
}

internal sealed class FakeJwtTokenGenerator : IJwtTokenGenerator
{
    public string GenerateAccessToken(User user)
    {
        return $"token-for:{user.Id}";
    }
}

internal sealed class FakeUnitOfWork : IUnitOfWork
{
    public int SaveChangesCallCount { get; private set; }

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        SaveChangesCallCount++;

        return Task.FromResult(1);
    }
}

internal sealed class FakeIndexingService : IIndexingService
{
    public List<Guid> IndexedEmployeeIds { get; } = [];

    public List<Guid> IndexedDocumentIds { get; } = [];

    public List<Guid> RemovedEmployeeIds { get; } = [];

    public List<Guid> RemovedDocumentIds { get; } = [];

    public int RecreateIndexCallCount { get; private set; }

    public Task RecreateIndexAsync(CancellationToken cancellationToken = default)
    {
        RecreateIndexCallCount++;

        return Task.CompletedTask;
    }

    public Task IndexEmployeeAsync(
        Employee employee,
        CancellationToken cancellationToken = default)
    {
        IndexedEmployeeIds.Add(employee.Id);

        return Task.CompletedTask;
    }

    public Task IndexDocumentAsync(
        DocumentEntity document,
        CancellationToken cancellationToken = default)
    {
        IndexedDocumentIds.Add(document.Id);

        return Task.CompletedTask;
    }

    public Task RemoveEmployeeAsync(
        Guid employeeId,
        CancellationToken cancellationToken = default)
    {
        RemovedEmployeeIds.Add(employeeId);

        return Task.CompletedTask;
    }

    public Task RemoveDocumentAsync(
        Guid documentId,
        CancellationToken cancellationToken = default)
    {
        RemovedDocumentIds.Add(documentId);

        return Task.CompletedTask;
    }
}

internal sealed class InMemoryUserRepository : IUserRepository
{
    private readonly List<User> _users = [];

    public IReadOnlyList<User> Users => _users;

    public Task<User?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult(_users.FirstOrDefault(user => user.Id == id));
    }

    public Task<User?> GetByEmailAsync(
        string email,
        CancellationToken cancellationToken = default)
    {
        var normalizedEmail = email.Trim().ToLowerInvariant();

        return Task.FromResult(_users.FirstOrDefault(user => user.Email == normalizedEmail));
    }

    public Task<bool> ExistsByEmailAsync(
        string email,
        CancellationToken cancellationToken = default)
    {
        var normalizedEmail = email.Trim().ToLowerInvariant();

        return Task.FromResult(_users.Any(user => user.Email == normalizedEmail));
    }

    public Task AddAsync(
        User user,
        CancellationToken cancellationToken = default)
    {
        _users.Add(user);

        return Task.CompletedTask;
    }

    public void AddExisting(User user)
    {
        _users.Add(user);
    }
}

internal sealed class InMemoryEmployeeRepository : IEmployeeRepository
{
    private readonly List<Employee> _employees = [];

    public IReadOnlyList<Employee> Employees => _employees;

    public Task<Employee?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult(_employees.FirstOrDefault(employee => employee.Id == id));
    }

    public Task<IReadOnlyList<Employee>> ListAllAsync(
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult<IReadOnlyList<Employee>>(_employees.ToList());
    }

    public Task<PagedResult<Employee>> ListAsync(
        EmployeeListRequest request,
        CancellationToken cancellationToken = default)
    {
        var query = _employees.AsEnumerable();

        if (!string.IsNullOrWhiteSpace(request.Department))
        {
            query = query.Where(employee =>
                string.Equals(employee.Department, request.Department, StringComparison.OrdinalIgnoreCase));
        }

        if (!string.IsNullOrWhiteSpace(request.JobTitle))
        {
            query = query.Where(employee =>
                string.Equals(employee.JobTitle, request.JobTitle, StringComparison.OrdinalIgnoreCase));
        }

        if (!string.IsNullOrWhiteSpace(request.Skill))
        {
            query = query.Where(employee =>
                employee.Skills.Contains(request.Skill, StringComparer.OrdinalIgnoreCase));
        }

        var filtered = query
            .OrderBy(employee => employee.LastName)
            .ThenBy(employee => employee.FirstName)
            .ToList();

        var items = filtered
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToList();

        return Task.FromResult(new PagedResult<Employee>(
            Items: items,
            Page: request.Page,
            PageSize: request.PageSize,
            TotalCount: filtered.Count));
    }

    public Task<bool> ExistsByEmailAsync(
        string email,
        Guid? excludingEmployeeId = null,
        CancellationToken cancellationToken = default)
    {
        var normalizedEmail = email.Trim().ToLowerInvariant();

        var exists = _employees.Any(employee =>
            employee.Email == normalizedEmail &&
            (!excludingEmployeeId.HasValue || employee.Id != excludingEmployeeId.Value));

        return Task.FromResult(exists);
    }

    public Task AddAsync(
        Employee employee,
        CancellationToken cancellationToken = default)
    {
        _employees.Add(employee);

        return Task.CompletedTask;
    }

    public void Remove(Employee employee)
    {
        _employees.Remove(employee);
    }

    public void AddExisting(Employee employee)
    {
        _employees.Add(employee);
    }
}

internal sealed class InMemoryDocumentRepository : IDocumentRepository
{
    private readonly List<DocumentEntity> _documents = [];

    public Task<DocumentEntity?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult(_documents.FirstOrDefault(document => document.Id == id));
    }

    public Task<IReadOnlyList<DocumentEntity>> ListAllAsync(
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult<IReadOnlyList<DocumentEntity>>(_documents.ToList());
    }

    public Task<PagedResult<DocumentEntity>> ListAsync(
        DocumentListRequest request,
        CancellationToken cancellationToken = default)
    {
        var query = _documents.AsEnumerable();

        if (!string.IsNullOrWhiteSpace(request.Category))
        {
            query = query.Where(document =>
                string.Equals(document.Category, request.Category, StringComparison.OrdinalIgnoreCase));
        }

        if (!string.IsNullOrWhiteSpace(request.Tag))
        {
            query = query.Where(document =>
                document.Tags.Contains(request.Tag, StringComparer.OrdinalIgnoreCase));
        }

        var filtered = query
            .OrderBy(document => document.Title)
            .ToList();

        var items = filtered
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToList();

        return Task.FromResult(new PagedResult<DocumentEntity>(
            Items: items,
            Page: request.Page,
            PageSize: request.PageSize,
            TotalCount: filtered.Count));
    }

    public Task AddAsync(
        DocumentEntity document,
        CancellationToken cancellationToken = default)
    {
        _documents.Add(document);

        return Task.CompletedTask;
    }

    public void Remove(DocumentEntity document)
    {
        _documents.Remove(document);
    }
}