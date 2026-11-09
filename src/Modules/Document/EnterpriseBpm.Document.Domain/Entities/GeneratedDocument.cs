using EnterpriseBpm.SharedKernel;

namespace EnterpriseBpm.Document.Domain.Entities;

public class GeneratedDocument : BaseEntity<Guid>
{
    public Guid TemplateId { get; private set; }
    public string WorkflowInstanceId { get; private set; } = default!;
    public string GeneratedFileName { get; private set; } = default!;
    public string OneDriveItemId { get; private set; } = default!;
    public string OneDriveWebUrl { get; private set; } = default!;
    public string ContentSha256Hash { get; private set; } = default!;
    public Guid GeneratedByUserId { get; private set; }
    public DateTime GeneratedAt { get; private set; } = DateTime.UtcNow;

    // Navigation
    public DocumentTemplate? Template { get; private set; }

    protected GeneratedDocument() { }

    public static GeneratedDocument Create(
        Guid templateId,
        string workflowInstanceId,
        string generatedFileName,
        string oneDriveItemId,
        string oneDriveWebUrl,
        string contentSha256Hash,
        Guid generatedByUserId)
    {
        if (string.IsNullOrWhiteSpace(workflowInstanceId))
            throw new ArgumentException("WorkflowInstanceId không được để trống.", nameof(workflowInstanceId));
        if (string.IsNullOrWhiteSpace(generatedFileName))
            throw new ArgumentException("GeneratedFileName không được để trống.", nameof(generatedFileName));
        if (string.IsNullOrWhiteSpace(oneDriveItemId))
            throw new ArgumentException("OneDriveItemId không được để trống.", nameof(oneDriveItemId));

        return new GeneratedDocument
        {
            Id = Guid.NewGuid(),
            TemplateId = templateId,
            WorkflowInstanceId = workflowInstanceId,
            GeneratedFileName = generatedFileName,
            OneDriveItemId = oneDriveItemId,
            OneDriveWebUrl = oneDriveWebUrl,
            ContentSha256Hash = contentSha256Hash,
            GeneratedByUserId = generatedByUserId,
            GeneratedAt = DateTime.UtcNow
        };
    }
}

