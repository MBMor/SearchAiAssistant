using Microsoft.Extensions.Options;
using SearchAiAssistant.Application.Abstractions.Persistence;
using SearchAiAssistant.Application.Common.Abstractions;
using SearchAiAssistant.Application.Common.Exceptions;
using SearchAiAssistant.Application.Common.Options;
using SearchAiAssistant.Application.Common.Pagination;
using SearchAiAssistant.Domain.Entities;

namespace SearchAiAssistant.Application.Employees;

public sealed class EmployeeService(
    IEmployeeRepository employeeRepository,
    IDateTimeProvider dateTimeProvider,
    IUnitOfWork unitOfWork,
    IOptions<PaginationOptions> paginationOptions) : IEmployeeService
{
    private readonly IEmployeeRepository _employeeRepository = employeeRepository;
    private readonly IDateTimeProvider _dateTimeProvider = dateTimeProvider;
    private readonly IUnitOfWork _unitOfWork = unitOfWork;
    private readonly PaginationOptions _paginationOptions = paginationOptions.Value;

    public async Task<EmployeeResponse> CreateAsync(
        CreateEmployeeRequest request,
        CancellationToken cancellationToken = default)
    {
        var normalizedEmail = NormalizeEmail(request.Email);

        if (await _employeeRepository.ExistsByEmailAsync(normalizedEmail, cancellationToken: cancellationToken))
        {
            throw new DuplicateEmployeeEmailException(normalizedEmail);
        }

        var now = _dateTimeProvider.UtcNow;

        var employee = new Employee(
            id: Guid.NewGuid(),
            firstName: request.FirstName,
            lastName: request.LastName,
            email: normalizedEmail,
            department: request.Department,
            jobTitle: request.JobTitle,
            skills: request.Skills ?? [],
            location: request.Location,
            createdAt: now);

        await _employeeRepository.AddAsync(employee, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return ToResponse(employee);
    }

    public async Task<EmployeeResponse?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var employee = await _employeeRepository.GetByIdAsync(id, cancellationToken);

        return employee is null
            ? null
            : ToResponse(employee);
    }

    public async Task<PagedResult<EmployeeResponse>> ListAsync(
        EmployeeListRequest request,
        CancellationToken cancellationToken = default)
    {
        var normalizedRequest = NormalizeListRequest(request);

        var result = await _employeeRepository.ListAsync(
            normalizedRequest,
            cancellationToken);

        var items = result.Items
            .Select(ToResponse)
            .ToList();

        return new PagedResult<EmployeeResponse>(
            Items: items,
            Page: result.Page,
            PageSize: result.PageSize,
            TotalCount: result.TotalCount);
    }

    public async Task<EmployeeResponse?> UpdateAsync(
        Guid id,
        UpdateEmployeeRequest request,
        CancellationToken cancellationToken = default)
    {
        var employee = await _employeeRepository.GetByIdAsync(id, cancellationToken);

        if (employee is null)
        {
            return null;
        }

        var normalizedEmail = NormalizeEmail(request.Email);

        if (await _employeeRepository.ExistsByEmailAsync(
                normalizedEmail,
                excludingEmployeeId: id,
                cancellationToken: cancellationToken))
        {
            throw new DuplicateEmployeeEmailException(normalizedEmail);
        }

        employee.Update(
            firstName: request.FirstName,
            lastName: request.LastName,
            email: normalizedEmail,
            department: request.Department,
            jobTitle: request.JobTitle,
            skills: request.Skills ?? [],
            location: request.Location,
            updatedAt: _dateTimeProvider.UtcNow);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return ToResponse(employee);
    }

    public async Task<bool> DeleteAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var employee = await _employeeRepository.GetByIdAsync(id, cancellationToken);

        if (employee is null)
        {
            return false;
        }

        _employeeRepository.Remove(employee);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return true;
    }

    private EmployeeListRequest NormalizeListRequest(EmployeeListRequest request)
    {
        var page = request.Page < 1
            ? 1
            : request.Page;

        var pageSize = request.PageSize < 1
            ? _paginationOptions.DefaultPageSize
            : request.PageSize;

        pageSize = Math.Min(pageSize, _paginationOptions.MaxPageSize);

        return request with
        {
            Department = NormalizeOptionalFilter(request.Department),
            JobTitle = NormalizeOptionalFilter(request.JobTitle),
            Skill = NormalizeOptionalFilter(request.Skill),
            Page = page,
            PageSize = pageSize
        };
    }

    private static string? NormalizeOptionalFilter(string? value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? null
            : value.Trim();
    }

    private static string NormalizeEmail(string? email)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            throw new ValidationException("Employee email is required.");
        }

        var normalizedEmail = email.Trim().ToLowerInvariant();

        if (!normalizedEmail.Contains('@', StringComparison.Ordinal))
        {
            throw new ValidationException("Employee email must be valid.");
        }

        return normalizedEmail;
    }

    private static EmployeeResponse ToResponse(Employee employee)
    {
        return new EmployeeResponse(
            Id: employee.Id,
            FirstName: employee.FirstName,
            LastName: employee.LastName,
            Email: employee.Email,
            Department: employee.Department,
            JobTitle: employee.JobTitle,
            Skills: employee.Skills,
            Location: employee.Location,
            CreatedAt: employee.CreatedAt,
            UpdatedAt: employee.UpdatedAt);
    }
}