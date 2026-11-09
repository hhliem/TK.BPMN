using EnterpriseBpm.Org.Application.Contracts;
using EnterpriseBpm.Org.Application.DTOs;
using EnterpriseBpm.Org.Domain.Entities;
using EnterpriseBpm.Org.Domain.Services;
using EnterpriseBpm.Org.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace EnterpriseBpm.Org.Infrastructure.Services;

public class OrgHierarchyService : IOrgHierarchyService
{
    private readonly OrgDbContext _dbContext;
    private readonly ILogger<OrgHierarchyService> _logger;

    public OrgHierarchyService(OrgDbContext dbContext, ILogger<OrgHierarchyService> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<Guid?> GetDirectManagerIdAsync(Guid employeeId, CancellationToken cancellationToken = default)
    {
        var managerId = await _dbContext.Employees
            .AsNoTracking()
            .Where(e => e.Id == employeeId && e.IsActive)
            .Select(e => e.DirectManagerId)
            .FirstOrDefaultAsync(cancellationToken);

        return managerId;
    }

    public async Task<IEnumerable<Guid>> GetApproversByRoleAsync(string roleKey, Guid requesterOrgUnitId, CancellationToken cancellationToken = default)
    {
        // Tìm các nhân viên sở hữu Role trong cùng đơn vị hoặc đơn vị cấp trên
        var approverIds = await _dbContext.Employees
            .AsNoTracking()
            .Where(e => e.IsActive && e.OrgUnitId == requesterOrgUnitId)
            .Where(e => e.EmployeeRoles.Any(er => er.Role.RoleKey == roleKey))
            .Select(e => e.Id)
            .ToListAsync(cancellationToken);

        // Nếu phòng ban không có người giữ Role, leo thang lên đơn vị cha
        if (!approverIds.Any())
        {
            var parentOrgId = await _dbContext.OrgUnits
                .AsNoTracking()
                .Where(o => o.Id == requesterOrgUnitId && o.IsActive)
                .Select(o => o.ParentId)
                .FirstOrDefaultAsync(cancellationToken);

            if (parentOrgId.HasValue)
            {
                approverIds = await _dbContext.Employees
                    .AsNoTracking()
                    .Where(e => e.IsActive && e.OrgUnitId == parentOrgId.Value)
                    .Where(e => e.EmployeeRoles.Any(er => er.Role.RoleKey == roleKey))
                    .Select(e => e.Id)
                    .ToListAsync(cancellationToken);
            }
        }

        return approverIds;
    }

    public async Task<IReadOnlyList<OrgUnitTreeDto>> GetOrgTreeAsync(CancellationToken cancellationToken = default)
    {
        var units = await _dbContext.OrgUnits
            .AsNoTracking()
            .Where(o => o.IsActive)
            .OrderBy(o => o.DisplayOrder)
            .ThenBy(o => o.Name)
            .ToListAsync(cancellationToken);

        var roots = units
            .Where(u => !u.ParentId.HasValue)
            .Select(u => MapToTreeDto(u, units))
            .ToList();

        return roots;
    }

    private static OrgUnitTreeDto MapToTreeDto(OrgUnit unit, List<OrgUnit> allUnits)
    {
        var children = allUnits
            .Where(u => u.ParentId == unit.Id)
            .OrderBy(u => u.DisplayOrder)
            .ThenBy(u => u.Name)
            .Select(c => MapToTreeDto(c, allUnits))
            .ToList();

        return new OrgUnitTreeDto(
            unit.Id,
            unit.ParentId,
            unit.Code,
            unit.Name,
            unit.UnitType,
            unit.DisplayOrder,
            children);
    }

    public async Task<Guid> CreateOrgUnitAsync(CreateOrgUnitCommand command, CancellationToken cancellationToken = default)
    {
        if (command.ParentId.HasValue)
        {
            var parentExists = await _dbContext.OrgUnits
                .AnyAsync(o => o.Id == command.ParentId.Value && o.IsActive, cancellationToken);
            if (!parentExists)
            {
                throw new KeyNotFoundException($"Không tìm thấy đơn vị cha với Id: {command.ParentId.Value}");
            }
        }

        var orgUnit = new OrgUnit
        {
            Id = Guid.NewGuid(),
            ParentId = command.ParentId,
            Code = command.Code,
            Name = command.Name,
            UnitType = command.UnitType,
            DisplayOrder = command.DisplayOrder,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        _dbContext.OrgUnits.Add(orgUnit);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return orgUnit.Id;
    }

    public async Task MoveOrgUnitAsync(Guid orgUnitId, Guid? newParentId, CancellationToken cancellationToken = default)
    {
        var unit = await _dbContext.OrgUnits
            .FirstOrDefaultAsync(o => o.Id == orgUnitId && o.IsActive, cancellationToken)
            ?? throw new KeyNotFoundException($"Không tìm thấy đơn vị tổ chức: {orgUnitId}");

        if (unit.ParentId == newParentId)
        {
            return; // Không đổi
        }

        // Lấy danh sách toàn bộ các quan hệ (Id, ParentId) trong bộ nhớ để duyệt ngược kiểm tra vòng lặp
        var parentLookup = await _dbContext.OrgUnits
            .AsNoTracking()
            .Where(o => o.IsActive)
            .Select(o => new { o.Id, o.ParentId })
            .ToDictionaryAsync(x => x.Id, x => x.ParentId, cancellationToken);

        // Anti-Circular Ancestry Check
        AntiCircularAncestryValidator.ValidateNoCycle(
            orgUnitId, 
            newParentId, 
            id => parentLookup.TryGetValue(id, out var pId) ? pId : null);

        unit.ParentId = newParentId;
        unit.UpdatedAt = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("Di chuyển đơn vị {UnitCode} sang ParentId mới: {NewParentId}", unit.Code, newParentId);
    }
}

