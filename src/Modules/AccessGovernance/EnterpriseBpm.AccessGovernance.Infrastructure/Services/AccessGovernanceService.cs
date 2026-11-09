using EnterpriseBpm.AccessGovernance.Application.Contracts;
using EnterpriseBpm.AccessGovernance.Application.DTOs;
using EnterpriseBpm.AccessGovernance.Domain.Entities;
using EnterpriseBpm.AccessGovernance.Domain.Enums;
using EnterpriseBpm.AccessGovernance.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace EnterpriseBpm.AccessGovernance.Infrastructure.Services;

public class AccessGovernanceService : IAccessGovernanceService
{
    private readonly AuthDbContext _dbContext;
    private readonly IPermissionEvaluator _permissionEvaluator;
    private readonly ILogger<AccessGovernanceService> _logger;

    public AccessGovernanceService(
        AuthDbContext dbContext,
        IPermissionEvaluator permissionEvaluator,
        ILogger<AccessGovernanceService> logger)
    {
        _dbContext = dbContext;
        _permissionEvaluator = permissionEvaluator;
        _logger = logger;
    }

    public async Task<RolePermissionMatrixDto> GetRolePermissionsAsync(Guid roleId, CancellationToken cancellationToken = default)
    {
        var role = await _dbContext.Roles
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.Id == roleId, cancellationToken)
            ?? throw new KeyNotFoundException($"Không tìm thấy vai trò với Id: {roleId}");

        var assignedPermissionIds = await _dbContext.RolePermissions
            .AsNoTracking()
            .Where(rp => rp.RoleId == roleId)
            .Select(rp => rp.PermissionId)
            .ToListAsync(cancellationToken);

        var allPermissions = await _dbContext.Permissions
            .AsNoTracking()
            .OrderBy(p => p.ModuleName)
            .ThenBy(p => p.Resource)
            .ThenBy(p => p.Action)
            .Select(p => new PermissionDto(
                p.Id,
                p.PermissionCode,
                p.ModuleName,
                p.Resource,
                p.Action,
                p.Name,
                p.Description,
                p.IsSystem))
            .ToListAsync(cancellationToken);

        return new RolePermissionMatrixDto(
            RoleId: role.Id,
            RoleCode: role.RoleCode,
            RoleName: role.Name,
            AssignedPermissionIds: assignedPermissionIds,
            AllPermissions: allPermissions);
    }

    public async Task UpdateRolePermissionsAsync(Guid roleId, IEnumerable<Guid> permissionIds, CancellationToken cancellationToken = default)
    {
        var role = await _dbContext.Roles
            .FirstOrDefaultAsync(r => r.Id == roleId, cancellationToken)
            ?? throw new KeyNotFoundException($"Không tìm thấy vai trò với Id: {roleId}");

        // Xóa các mapping cũ
        var existingRolePerms = await _dbContext.RolePermissions
            .Where(rp => rp.RoleId == roleId)
            .ToListAsync(cancellationToken);

        _dbContext.RolePermissions.RemoveRange(existingRolePerms);

        // Thêm các mapping mới
        var distinctIds = permissionIds.Distinct().ToList();
        var newRolePerms = distinctIds.Select(pId => new RolePermission
        {
            RoleId = roleId,
            PermissionId = pId
        });

        await _dbContext.RolePermissions.AddRangeAsync(newRolePerms, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Cập nhật thành công {Count} quyền cho Vai trò {RoleCode} ({RoleId})", distinctIds.Count, role.RoleCode, roleId);

        // Instant Cache Eviction: Tìm toàn bộ User đang giữ Role này và xóa cache
        var affectedUserIds = await _dbContext.UserRoles
            .AsNoTracking()
            .Where(ur => ur.RoleId == roleId)
            .Select(ur => ur.UserId)
            .ToListAsync(cancellationToken);

        foreach (var userId in affectedUserIds)
        {
            await _permissionEvaluator.InvalidateUserCacheAsync(userId, cancellationToken);
        }
    }

    public async Task<IReadOnlyList<string>> GetEffectivePermissionsAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var perms = await _permissionEvaluator.GetUserPermissionsAsync(userId, cancellationToken);
        return perms.OrderBy(p => p).ToList();
    }

    public async Task<Guid> CreateApprovalLimitAsync(CreateApprovalLimitCommand command, CancellationToken cancellationToken = default)
    {
        var limit = new ApprovalLimit
        {
            Id = Guid.NewGuid(),
            PositionId = command.PositionId,
            RoleId = command.RoleId,
            FormKey = command.FormKey,
            Currency = command.Currency,
            MaxLimitAmount = command.MaxLimitAmount,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        _dbContext.ApprovalLimits.Add(limit);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return limit.Id;
    }

    public async Task<IReadOnlyList<NavigationMenuDto>> GetUserAuthorizedMenuAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var userPerms = await _permissionEvaluator.GetUserPermissionsAsync(userId, cancellationToken);

        var allMenus = await _dbContext.MenuPermissions
            .AsNoTracking()
            .Where(m => m.IsActive)
            .OrderBy(m => m.DisplayOrder)
            .ToListAsync(cancellationToken);

        // Lọc menu theo quyền người dùng
        var authorizedMenus = allMenus
            .Where(m => string.IsNullOrWhiteSpace(m.RequiredPermissionCode) || userPerms.Contains(m.RequiredPermissionCode))
            .ToList();

        // Xây dựng cây Menu (Tree Structure: Root items và Children)
        var rootMenus = authorizedMenus
            .Where(m => string.IsNullOrEmpty(m.ParentMenuKey))
            .Select(m => new NavigationMenuDto(
                m.Id,
                m.MenuKey,
                m.ParentMenuKey,
                m.Title,
                m.RouteUrl,
                m.IconClass,
                m.RequiredPermissionCode,
                m.DisplayOrder,
                new List<NavigationMenuDto>()))
            .ToList();

        var childMenus = authorizedMenus
            .Where(m => !string.IsNullOrEmpty(m.ParentMenuKey))
            .ToList();

        foreach (var root in rootMenus)
        {
            var children = childMenus
                .Where(c => c.ParentMenuKey == root.MenuKey)
                .Select(c => new NavigationMenuDto(
                    c.Id,
                    c.MenuKey,
                    c.ParentMenuKey,
                    c.Title,
                    c.RouteUrl,
                    c.IconClass,
                    c.RequiredPermissionCode,
                    c.DisplayOrder,
                    new List<NavigationMenuDto>()))
                .ToList();

            root.Children.AddRange(children);
        }

        return rootMenus;
    }

    public async Task AssignRoleToUserAsync(Guid userId, Guid roleId, Guid assignedBy, CancellationToken cancellationToken = default)
    {
        var exists = await _dbContext.UserRoles
            .AnyAsync(ur => ur.UserId == userId && ur.RoleId == roleId, cancellationToken);

        if (!exists)
        {
            _dbContext.UserRoles.Add(new UserRole
            {
                UserId = userId,
                RoleId = roleId,
                AssignedAt = DateTime.UtcNow,
                AssignedBy = assignedBy
            });

            await _dbContext.SaveChangesAsync(cancellationToken);
            await _permissionEvaluator.InvalidateUserCacheAsync(userId, cancellationToken);
        }
    }

    public async Task RevokeRoleFromUserAsync(Guid userId, Guid roleId, CancellationToken cancellationToken = default)
    {
        var userRole = await _dbContext.UserRoles
            .FirstOrDefaultAsync(ur => ur.UserId == userId && ur.RoleId == roleId, cancellationToken);

        if (userRole != null)
        {
            _dbContext.UserRoles.Remove(userRole);
            await _dbContext.SaveChangesAsync(cancellationToken);
            await _permissionEvaluator.InvalidateUserCacheAsync(userId, cancellationToken);
        }
    }

    public async Task<IReadOnlyList<RoleDto>> GetAllRolesAsync(CancellationToken cancellationToken = default)
    {
        return await _dbContext.Roles
            .AsNoTracking()
            .OrderBy(r => r.RoleCode)
            .Select(r => new RoleDto(
                r.Id,
                r.RoleCode,
                r.Name,
                r.RoleType.ToString(),
                r.Description,
                r.IsActive))
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<PermissionDto>> GetAllPermissionsAsync(CancellationToken cancellationToken = default)
    {
        return await _dbContext.Permissions
            .AsNoTracking()
            .OrderBy(p => p.ModuleName)
            .ThenBy(p => p.PermissionCode)
            .Select(p => new PermissionDto(
                p.Id,
                p.PermissionCode,
                p.ModuleName,
                p.Resource,
                p.Action,
                p.Name,
                p.Description,
                p.IsSystem))
            .ToListAsync(cancellationToken);
    }
}

