using SearchAiAssistant.Application.Common.Pagination;
using SearchAiAssistant.Application.Employees;
using SearchAiAssistant.Domain.Entities;

namespace SearchAiAssistant.Application.Abstractions.Persistence;

public interface IEmployeeRepository
{
    Task<Employee?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<PagedResult<Employee>> ListAsync(
        EmployeeListRequest request,
        CancellationToken cancellationToken = default);

    Task<bool> ExistsByEmailAsync(
        string email,
        Guid? excludingEmployeeId = null,
        CancellationToken cancellationToken = default);

    Task AddAsync(Employee employee, CancellationToken cancellationToken = default);

    void Remove(Employee employee);
}