using EnterpriseBpm.AccessGovernance.Application.DTOs;

namespace EnterpriseBpm.AccessGovernance.Application.Contracts;

public interface IPermissionEvaluator
{
    /// <summary>
    /// Kiểm tra người dùng có sở hữu quyền thao tác cụ thể hay không (< 2ms qua Redis/Memory cache)
    /// </summary>
    Task<bool> HasPermissionAsync(Guid userId, string permissionCode, CancellationToken cancellationToken = default);

    /// <summary>
    /// Lấy toàn bộ danh sách quyền của người dùng (tổng hợp từ UserRoles và SecurityGroups)
    /// </summary>
    Task<HashSet<string>> GetUserPermissionsAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Hủy bộ nhớ đệm phân quyền của một User ngay tức thì khi có thay đổi vai trò/nhóm
    /// </summary>
    Task InvalidateUserCacheAsync(Guid userId, CancellationToken cancellationToken = default);
}

public interface IDataScopeEvaluator
{
    /// <summary>
    /// Đánh giá phạm vi dữ liệu người dùng được phép truy vấn đối với một tài nguyên cụ thể
    /// </summary>
    Task<DataScopeResult> EvaluateScopeAsync(Guid userId, string resourceName, CancellationToken cancellationToken = default);
}

public interface IApprovalLimitChecker
{
    /// <summary>
    /// Kiểm tra người dùng có đủ hạn mức tài chính/nghiệp vụ để duyệt đơn hiện tại hay không
    /// </summary>
    Task<ApprovalLimitResult> CanApproveAmountAsync(
        Guid userId, 
        string formKey, 
        decimal amount, 
        string currency = "VND", 
        CancellationToken cancellationToken = default);
}

public interface IAccessGovernanceService
{
    Task<RolePermissionMatrixDto> GetRolePermissionsAsync(Guid roleId, CancellationToken cancellationToken = default);
    Task UpdateRolePermissionsAsync(Guid roleId, IEnumerable<Guid> permissionIds, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<string>> GetEffectivePermissionsAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<Guid> CreateApprovalLimitAsync(CreateApprovalLimitCommand command, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<NavigationMenuDto>> GetUserAuthorizedMenuAsync(Guid userId, CancellationToken cancellationToken = default);
    Task AssignRoleToUserAsync(Guid userId, Guid roleId, Guid assignedBy, CancellationToken cancellationToken = default);
    Task RevokeRoleFromUserAsync(Guid userId, Guid roleId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<RoleDto>> GetAllRolesAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<PermissionDto>> GetAllPermissionsAsync(CancellationToken cancellationToken = default);
}

