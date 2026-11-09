using EnterpriseBpm.Org.Application.DTOs;

namespace EnterpriseBpm.Org.Application.Contracts;

public interface IDelegationResolver
{
    /// <summary>
    /// Phân giải người duyệt thực tế: nếu người được cấu hình đang có ủy quyền hợp lệ,
    /// trả về ID của người được ủy quyền (DelegateeId); ngược lại trả về configuredApproverId.
    /// </summary>
    Task<Guid> ResolveActualApproverAsync(
        Guid configuredApproverId, 
        string formKey, 
        CancellationToken cancellationToken = default);
}

public interface IOrgHierarchyService
{
    Task<Guid?> GetDirectManagerIdAsync(Guid employeeId, CancellationToken cancellationToken = default);
    Task<IEnumerable<Guid>> GetApproversByRoleAsync(string roleKey, Guid requesterOrgUnitId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<OrgUnitTreeDto>> GetOrgTreeAsync(CancellationToken cancellationToken = default);
    Task<Guid> CreateOrgUnitAsync(CreateOrgUnitCommand command, CancellationToken cancellationToken = default);
    Task MoveOrgUnitAsync(Guid orgUnitId, Guid? newParentId, CancellationToken cancellationToken = default);
}

public interface IDelegationService
{
    Task<Guid> AssignDelegationAsync(AssignDelegationCommand command, CancellationToken cancellationToken = default);
    Task RevokeDelegationAsync(Guid delegationId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<DelegationDto>> GetMyDelegationsAsync(Guid employeeId, CancellationToken cancellationToken = default);
}

public interface IEmployeeDirectoryService
{
    Task<PagedResult<EmployeeDirectoryDto>> SearchEmployeesAsync(
        string? keyword,
        Guid? orgUnitId,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);

    Task<EmployeeProfileDto?> GetEmployeeProfileAsync(
        Guid employeeId,
        CancellationToken cancellationToken = default);
}

