namespace EnterpriseBpm.SharedKernel.Contracts;

/// <summary>
/// Dịch vụ cung cấp thông tin định danh và phân quyền của User trong phiên làm việc hiện tại
/// </summary>
public interface ICurrentUserService
{
    Guid? UserId { get; }
    string? UserName { get; }
    string? UserPrincipalName { get; }
    Guid? OrgUnitId { get; }
    IReadOnlyList<string> Roles { get; }
    bool IsAuthenticated { get; }
}

