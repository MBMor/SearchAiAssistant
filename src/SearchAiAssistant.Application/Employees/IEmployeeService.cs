using SearchAiAssistant.Application.Common.Pagination;

namespace SearchAiAssistant.Application.Employees;

public interface IEmployeeService
{
    Task<EmployeeResponse> CreateAsync(
        CreateEmployeeRequest request,
        CancellationToken cancellationToken = default);

    Task<EmployeeResponse?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    Task<PagedResult<EmployeeResponse>> ListAsync(
        EmployeeListRequest request,
        CancellationToken cancellationToken = default);

    Task<EmployeeResponse?> UpdateAsync(
        Guid id,
        UpdateEmployeeRequest request,
        CancellationToken cancellationToken = default);

    Task<bool> DeleteAsync(
        Guid id,
        CancellationToken cancellationToken = default);
}