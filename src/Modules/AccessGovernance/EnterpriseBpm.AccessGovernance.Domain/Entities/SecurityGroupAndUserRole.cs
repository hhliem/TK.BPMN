using EnterpriseBpm.SharedKernel;

namespace EnterpriseBpm.AccessGovernance.Domain.Entities;

public class SecurityGroup : BaseEntity
{
    public string GroupCode { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    public virtual ICollection<SecurityGroupRole> SecurityGroupRoles { get; set; } = new List<SecurityGroupRole>();
    public virtual ICollection<SecurityGroupMember> Members { get; set; } = new List<SecurityGroupMember>();
}

public class SecurityGroupRole
{
    public Guid GroupId { get; set; }
    public virtual SecurityGroup Group { get; set; } = null!;

    public Guid RoleId { get; set; }
    public virtual Role Role { get; set; } = null!;
}

public class SecurityGroupMember
{
    public Guid GroupId { get; set; }
    public virtual SecurityGroup Group { get; set; } = null!;

    public Guid UserId { get; set; }
    public DateTime AssignedAt { get; set; } = DateTime.UtcNow;
}

public class UserRole
{
    public Guid UserId { get; set; }
    public Guid RoleId { get; set; }
    public virtual Role Role { get; set; } = null!;

    public DateTime AssignedAt { get; set; } = DateTime.UtcNow;
    public Guid? AssignedBy { get; set; }
}
