using EnterpriseBpm.Org.Domain.Enums;

namespace EnterpriseBpm.Org.Application.DTOs;

public record PagedResult<T>(
    IReadOnlyList<T> Items,
    int TotalCount,
    int PageIndex,
    int PageSize
);

public record OrgUnitTreeDto(
    Guid Id,
    Guid? ParentId,
    string Code,
    string Name,
    UnitType UnitType,
    int DisplayOrder,
    List<OrgUnitTreeDto> Children
);

public record CreateOrgUnitCommand(
    Guid? ParentId,
    string Code,
    string Name,
    UnitType UnitType,
    int DisplayOrder
);

public record AssignDelegationCommand(
    Guid DelegatorId,
    Guid DelegateeId,
    DateTime FromDate,
    DateTime ToDate,
    string? ScopeFormKey,
    string Reason
);

public record DelegationDto(
    Guid Id,
    Guid DelegatorId,
    string DelegatorName,
    Guid DelegateeId,
    string DelegateeName,
    DateTime FromDate,
    DateTime ToDate,
    string? ScopeFormKey,
    string Reason,
    bool IsActive
);

public record EmployeeDirectoryDto(
    Guid Id,
    string EmployeeCode,
    string FullName,
    string UserPrincipalName,
    Guid OrgUnitId,
    string OrgUnitName,
    Guid PositionId,
    string PositionTitle,
    string? JobTitle,
    string? PhoneNumber,
    string? Extension,
    string? WorkLocation,
    string? AvatarUrl
);

public record EmployeeProfileDto(
    Guid Id,
    string EmployeeCode,
    string FullName,
    string UserPrincipalName,
    Guid OrgUnitId,
    string OrgUnitName,
    Guid PositionId,
    string PositionTitle,
    string? JobTitle,
    string? PhoneNumber,
    string? Extension,
    string? WorkLocation,
    string? AvatarUrl,
    Guid? DirectManagerId,
    string? DirectManagerName,
    DateOnly? DateOfBirth,
    DateOnly? JoinDate,
    bool IsActive
);

