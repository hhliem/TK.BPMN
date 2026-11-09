using EnterpriseBpm.Org.Application.Contracts;
using EnterpriseBpm.Org.Application.DTOs;
using EnterpriseBpm.Org.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace EnterpriseBpm.Org.Infrastructure.Services;

public class EmployeeDirectoryService : IEmployeeDirectoryService
{
    private readonly OrgDbContext _dbContext;
    private readonly ILogger<EmployeeDirectoryService> _logger;

    public EmployeeDirectoryService(OrgDbContext dbContext, ILogger<EmployeeDirectoryService> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<PagedResult<EmployeeDirectoryDto>> SearchEmployeesAsync(
        string? keyword,
        Guid? orgUnitId,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        if (page < 1) page = 1;
        if (pageSize < 1) pageSize = 20;

        var query = _dbContext.Employees
            .AsNoTracking()
            .Where(e => e.IsActive);

        if (orgUnitId.HasValue)
        {
            query = query.Where(e => e.OrgUnitId == orgUnitId.Value);
        }

        if (!string.IsNullOrWhiteSpace(keyword))
        {
            var clean = keyword.Trim().ToLower();
            query = query.Where(e =>
                e.FullName.ToLower().Contains(clean) ||
                e.EmployeeCode.ToLower().Contains(clean) ||
                (e.PhoneNumber != null && e.PhoneNumber.Contains(clean)) ||
                (e.Extension != null && e.Extension.Contains(clean)) ||
                (e.JobTitle != null && e.JobTitle.ToLower().Contains(clean)));
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderBy(e => e.FullName)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(e => new EmployeeDirectoryDto(
                e.Id,
                e.EmployeeCode,
                e.FullName,
                e.UserPrincipalName,
                e.OrgUnitId,
                e.OrgUnit.Name,
                e.PositionId,
                e.Position.Title,
                e.JobTitle,
                e.PhoneNumber,
                e.Extension,
                e.WorkLocation,
                e.AvatarUrl))
            .ToListAsync(cancellationToken);

        return new PagedResult<EmployeeDirectoryDto>(items, totalCount, page, pageSize);
    }

    public async Task<EmployeeProfileDto?> GetEmployeeProfileAsync(Guid employeeId, CancellationToken cancellationToken = default)
    {
        var emp = await _dbContext.Employees
            .AsNoTracking()
            .Where(e => e.Id == employeeId && e.IsActive)
            .Select(e => new EmployeeProfileDto(
                e.Id,
                e.EmployeeCode,
                e.FullName,
                e.UserPrincipalName,
                e.OrgUnitId,
                e.OrgUnit.Name,
                e.PositionId,
                e.Position.Title,
                e.JobTitle,
                e.PhoneNumber,
                e.Extension,
                e.WorkLocation,
                e.AvatarUrl,
                e.DirectManagerId,
                e.DirectManager != null ? e.DirectManager.FullName : null,
                e.DateOfBirth,
                e.JoinDate,
                e.IsActive))
            .FirstOrDefaultAsync(cancellationToken);

        return emp;
    }
}

