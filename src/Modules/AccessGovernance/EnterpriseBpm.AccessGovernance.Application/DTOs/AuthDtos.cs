namespace EnterpriseBpm.AccessGovernance.Application.DTOs;

public record DataScopeResult(
    string ScopeType, // "Self", "Department", "Division", "Global"
    Guid? OrgUnitId,
    IReadOnlyList<Guid> AccessibleOrgUnitIds
);

public record ApprovalLimitResult(
    bool IsAllowed, 
    decimal MaxAllowedLimit, 
    string? ReasonMessage
);

public record RoleDto(
    Guid Id,
    string RoleCode,
    string Name,
    string RoleType,
    string? Description,
    bool IsActive
);

public record PermissionDto(
    Guid Id,
    string PermissionCode,
    string ModuleName,
    string Resource,
    string Action,
    string Name,
    string? Description,
    bool IsSystem
);

public record RolePermissionMatrixDto(
    Guid RoleId,
    string RoleCode,
    string RoleName,
    IReadOnlyList<Guid> AssignedPermissionIds,
    IReadOnlyList<PermissionDto> AllPermissions
);

public record UpdateRolePermissionsDto(
    List<Guid> PermissionIds
);

public record AssignUserRoleDto(
    Guid UserId,
    Guid RoleId
);

public record CreateApprovalLimitCommand(
    Guid? PositionId,
    Guid? RoleId,
    string FormKey,
    string Currency,
    decimal MaxLimitAmount
);

public record NavigationMenuDto(
    Guid Id,
    string MenuKey,
    string? ParentMenuKey,
    string Title,
    string RouteUrl,
    string? IconClass,
    string? RequiredPermissionCode,
    int DisplayOrder,
    List<NavigationMenuDto> Children
);

