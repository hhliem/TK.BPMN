using EnterpriseBpm.Document.Domain.Enums;
using EnterpriseBpm.SharedKernel;
using EnterpriseBpm.SharedKernel.Contracts;

namespace EnterpriseBpm.Document.Domain.Entities;

public class DocumentTemplate : BaseEntity<Guid>, IAuditable
{
    public string TemplateKey { get; private set; } = default!;
    public string Title { get; private set; } = default!;
    public string? Description { get; private set; }
    public TemplateFormat Format { get; private set; } = TemplateFormat.Docx;
    public string OneDriveTemplateItemId { get; private set; } = default!;
    public string PlaceholderSchema { get; private set; } = "{}"; // JSON Schema các trường dữ liệu
    public bool IsActive { get; private set; } = true;

    // IAuditable
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public Guid? CreatedBy { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public Guid? UpdatedBy { get; set; }

    // Navigation
    private readonly List<GeneratedDocument> _generatedDocuments = new();
    public IReadOnlyCollection<GeneratedDocument> GeneratedDocuments => _generatedDocuments.AsReadOnly();

    protected DocumentTemplate() { }

    public static DocumentTemplate Create(
        string templateKey,
        string title,
        TemplateFormat format,
        string oneDriveTemplateItemId,
        string placeholderSchema,
        string? description = null)
    {
        if (string.IsNullOrWhiteSpace(templateKey))
            throw new ArgumentException("TemplateKey không được để trống.", nameof(templateKey));
        if (string.IsNullOrWhiteSpace(title))
            throw new ArgumentException("Title không được để trống.", nameof(title));
        if (string.IsNullOrWhiteSpace(oneDriveTemplateItemId))
            throw new ArgumentException("OneDriveTemplateItemId không được để trống.", nameof(oneDriveTemplateItemId));

        return new DocumentTemplate
        {
            Id = Guid.NewGuid(),
            TemplateKey = templateKey.Trim(),
            Title = title.Trim(),
            Description = description?.Trim(),
            Format = format,
            OneDriveTemplateItemId = oneDriveTemplateItemId,
            PlaceholderSchema = string.IsNullOrWhiteSpace(placeholderSchema) ? "{}" : placeholderSchema,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
    }

    public void Update(string title, string? description, string placeholderSchema)
    {
        Title = title;
        Description = description;
        PlaceholderSchema = placeholderSchema;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Deactivate()
    {
        IsActive = false;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Activate()
    {
        IsActive = true;
        UpdatedAt = DateTime.UtcNow;
    }
}

