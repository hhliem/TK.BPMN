using EnterpriseBpm.SharedKernel;
using EnterpriseBpm.SharedKernel.Contracts;

namespace EnterpriseBpm.Form.Domain.Entities;

public class FormDefinition : BaseEntity, IAuditable
{
    public string FormKey { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string SchemaJson { get; set; } = string.Empty;
    public int Version { get; set; } = 1;
    public bool IsLatest { get; set; } = true;
    public bool IsActive { get; set; } = true;
    public string? ChangeNotes { get; set; }
    public Guid? PublishedBy { get; set; }

    // IAuditable
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public Guid? CreatedBy { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public Guid? UpdatedBy { get; set; }
}

public class MasterCategory : BaseEntity
{
    public string CategoryKey { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsActive { get; set; } = true;

    // Navigation
    public virtual ICollection<MasterItem> Items { get; set; } = new List<MasterItem>();
}

public class MasterItem : BaseEntity
{
    public Guid CategoryId { get; set; }
    public virtual MasterCategory Category { get; set; } = null!;

    public Guid? ParentId { get; set; }
    public virtual MasterItem? Parent { get; set; }
    public virtual ICollection<MasterItem> Children { get; set; } = new List<MasterItem>();

    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public int DisplayOrder { get; set; } = 0;
    public string? CustomAttributesJson { get; set; }
    public bool IsActive { get; set; } = true;
}

