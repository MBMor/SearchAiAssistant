using Microsoft.EntityFrameworkCore;
using SearchAiAssistant.Application.Abstractions.Persistence;
using SearchAiAssistant.Application.Common.Pagination;
using SearchAiAssistant.Application.Employees;
using SearchAiAssistant.Domain.Entities;

namespace SearchAiAssistant.Infrastructure.Persistence.Repositories;

public sealed class EmployeeRepository(SearchAiAssistantDbContext dbContext) : IEmployeeRepository
{
    private readonly SearchAiAssistantDbContext _dbContext = dbContext;

    public Task<Employee?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        return _dbContext.Employees
            .FirstOrDefaultAsync(employee => employee.Id == id, cancellationToken);
    }

    public async Task<PagedResult<Employee>> ListAsync(
        EmployeeListRequest request,
        CancellationToken cancellationToken = default)
    {
        var query = _dbContext.Employees
            .AsNoTracking()
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(request.Department))
        {
            var department = request.Department.Trim().ToLower();

            query = query.Where(employee =>
                employee.Department.Equals(department, StringComparison.CurrentCultureIgnoreCase));
        }

        if (!string.IsNullOrWhiteSpace(request.JobTitle))
        {
            var jobTitle = request.JobTitle.Trim().ToLower();

            query = query.Where(employee =>
                employee.JobTitle.Equals(jobTitle, StringComparison.CurrentCultureIgnoreCase));
        }

        if (!string.IsNullOrWhiteSpace(request.Skill))
        {
            var skill = request.Skill.Trim();

            query = query.Where(employee =>
                employee.Skills.Contains(skill));
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderBy(employee => employee.LastName)
            .ThenBy(employee => employee.FirstName)
            .ThenBy(employee => employee.Id)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<Employee>(
            Items: items,
            Page: request.Page,
            PageSize: request.PageSize,
            TotalCount: totalCount);
    }

    public Task<bool> ExistsByEmailAsync(
        string email,
        Guid? excludingEmployeeId = null,
        CancellationToken cancellationToken = default)
    {
        var normalizedEmail = email.Trim().ToLowerInvariant();

        return _dbContext.Employees
            .AnyAsync(
                employee =>
                    employee.Email == normalizedEmail &&
                    (!excludingEmployeeId.HasValue || employee.Id != excludingEmployeeId.Value),
                cancellationToken);
    }

    public async Task AddAsync(
        Employee employee,
        CancellationToken cancellationToken = default)
    {
        await _dbContext.Employees.AddAsync(employee, cancellationToken);
    }

    public void Remove(Employee employee)
    {
        _dbContext.Employees.Remove(employee);
    }
}