using EnterpriseBpm.Org.Domain.Enums;
using EnterpriseBpm.SharedKernel;
using EnterpriseBpm.SharedKernel.Contracts;

namespace EnterpriseBpm.Org.Domain.Entities;

public class OrgUnit : BaseEntity, IAuditable, ISoftDeletable
{
    public Guid? ParentId { get; set; }
    public virtual OrgUnit? Parent { get; set; }
    public virtual ICollection<OrgUnit> Children { get; set; } = new List<OrgUnit>();

    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public UnitType UnitType { get; set; } = UnitType.Department;
    public int DisplayOrder { get; set; } = 0;
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
    public virtual ICollection<Employee> Employees { get; set; } = new List<Employee>();
}

public class Position : BaseEntity, IAuditable
{
    public string Code { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public int Level { get; set; } = 0;
    public bool IsActive { get; set; } = true;

    // IAuditable
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public Guid? CreatedBy { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public Guid? UpdatedBy { get; set; }

    // Navigation
    public virtual ICollection<Employee> Employees { get; set; } = new List<Employee>();
}

