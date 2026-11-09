using EnterpriseBpm.SharedKernel;
using EnterpriseBpm.SharedKernel.Contracts;

namespace EnterpriseBpm.Org.Domain.Entities;

public class Employee : BaseEntity, IAuditable, ISoftDeletable
{
    public string UserPrincipalName { get; set; } = string.Empty;
    public string EmployeeCode { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;

    public Guid OrgUnitId { get; set; }
    public virtual OrgUnit OrgUnit { get; set; } = null!;

    public Guid PositionId { get; set; }
    public virtual Position Position { get; set; } = null!;

    public Guid? DirectManagerId { get; set; }
    public virtual Employee? DirectManager { get; set; }
    public virtual ICollection<Employee> Subordinates { get; set; } = new List<Employee>();

    // Directory Fields
    public string? PhoneNumber { get; set; }
    public string? Extension { get; set; }
    public string? AvatarUrl { get; set; }
    public string? JobTitle { get; set; }
    public string? WorkLocation { get; set; }
    public DateOnly? DateOfBirth { get; set; }
    public DateOnly? JoinDate { get; set; }
    public bool IsActive { get; set; } = true;

    // IAuditable
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public Guid? CreatedBy { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public Guid? UpdatedBy { get; set; }

    // ISoftDeletable
    public DateTime? DeletedAt { get; set; }
    public Guid? DeletedBy { get; set; }
    public bool IsDeleted => DeletedAt.HasValue;

    // Navigation
    public virtual ICollection<EmployeeRole> EmployeeRoles { get; set; } = new List<EmployeeRole>();
    public virtual ICollection<WorkflowDelegation> GivenDelegations { get; set; } = new List<WorkflowDelegation>();
    public virtual ICollection<WorkflowDelegation> ReceivedDelegations { get; set; } = new List<WorkflowDelegation>();
}

public class Role : BaseEntity, IAuditable
{
    public string RoleKey { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }

    // IAuditable
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public Guid? CreatedBy { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public Guid? UpdatedBy { get; set; }

    // Navigation
    public virtual ICollection<EmployeeRole> EmployeeRoles { get; set; } = new List<EmployeeRole>();
}

public class EmployeeRole
{
    public Guid EmployeeId { get; set; }
    public virtual Employee Employee { get; set; } = null!;

    public Guid RoleId { get; set; }
    public virtual Role Role { get; set; } = null!;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public Guid? CreatedBy { get; set; }
}

public class WorkflowDelegation : BaseEntity, IAuditable
{
    public Guid DelegatorId { get; set; }
    public virtual Employee Delegator { get; set; } = null!;

    public Guid DelegateeId { get; set; }
    public virtual Employee Delegatee { get; set; } = null!;

    public DateTime FromDate { get; set; }
    public DateTime ToDate { get; set; }
    public string? ScopeFormKey { get; set; }
    public string Reason { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;

    // IAuditable
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public Guid? CreatedBy { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public Guid? UpdatedBy { get; set; }
}

