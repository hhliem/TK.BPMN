using EnterpriseBpm.SharedKernel;

namespace EnterpriseBpm.AccessGovernance.Domain.Entities;

public class Permission : BaseEntity
{
    public string PermissionCode { get; set; } = string.Empty;
    public string ModuleName { get; set; } = string.Empty;
    public string Resource { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsSystem { get; set; } = true;

    // Navigation
    public virtual ICollection<RolePermission> RolePermissions { get; set; } = new List<RolePermission>();
}
