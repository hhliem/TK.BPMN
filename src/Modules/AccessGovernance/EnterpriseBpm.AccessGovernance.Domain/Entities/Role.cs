using EnterpriseBpm.AccessGovernance.Domain.Enums;
using EnterpriseBpm.SharedKernel;
using EnterpriseBpm.SharedKernel.Contracts;

namespace EnterpriseBpm.AccessGovernance.Domain.Entities;

public class Role : BaseEntity, IAuditable
{
    public string RoleCode { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public RoleType RoleType { get; set; } = RoleType.Business;
    public string? Description { get; set; }
    public bool IsActive { get; set; } = true;

    // IAuditable
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public Guid? CreatedBy { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public Guid? UpdatedBy { get; set; }

    // Navigation
    public virtual ICollection<RolePermission> RolePermissions { get; set; } = new List<RolePermission>();
    public virtual ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();
    public virtual ICollection<SecurityGroupRole> SecurityGroupRoles { get; set; } = new List<SecurityGroupRole>();
    public virtual ICollection<DataScopePolicy> DataScopePolicies { get; set; } = new List<DataScopePolicy>();
    public virtual ICollection<ApprovalLimit> ApprovalLimits { get; set; } = new List<ApprovalLimit>();
}
