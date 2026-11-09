using EnterpriseBpm.AccessGovernance.Domain.Enums;
using EnterpriseBpm.SharedKernel;

namespace EnterpriseBpm.AccessGovernance.Domain.Entities;

public class DataScopePolicy : BaseEntity
{
    public Guid RoleId { get; set; }
    public virtual Role Role { get; set; } = null!;

    public string Resource { get; set; } = string.Empty;
    public DataScopeType ScopeType { get; set; } = DataScopeType.Department;
    public string? CustomFilterExpression { get; set; }
}

public class ApprovalLimit : BaseEntity
{
    public Guid? PositionId { get; set; }
    public Guid? RoleId { get; set; }
    public virtual Role? Role { get; set; }

    public string FormKey { get; set; } = "*";
    public string Currency { get; set; } = "VND";
    public decimal MaxLimitAmount { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public class FormPermission : BaseEntity
{
    public string FormKey { get; set; } = string.Empty;
    public SubjectType SubjectType { get; set; } = SubjectType.Role;
    public Guid SubjectId { get; set; }
    public bool CanInitiate { get; set; } = true;
    public bool CanViewAll { get; set; } = false;
    public bool CanExport { get; set; } = false;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public class MenuPermission : BaseEntity
{
    public string MenuKey { get; set; } = string.Empty;
    public string? ParentMenuKey { get; set; }
    public string Title { get; set; } = string.Empty;
    public string RouteUrl { get; set; } = string.Empty;
    public string? IconClass { get; set; }
    public string? RequiredPermissionCode { get; set; }
    public int DisplayOrder { get; set; } = 0;
    public bool IsActive { get; set; } = true;
}
