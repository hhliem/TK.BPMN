using EnterpriseBpm.Document.Domain.Services;
using EnterpriseBpm.SharedKernel;
using EnterpriseBpm.SharedKernel.Contracts;

namespace EnterpriseBpm.Document.Domain.Entities;

public class WorkflowAttachment : BaseEntity<Guid>, ISoftDeletable
{
    public string WorkflowInstanceId { get; private set; } = default!;
    public string FormKey { get; private set; } = default!;
    public string OriginalFileName { get; private set; } = default!;
    public string FileExtension { get; private set; } = default!;
    public string ContentType { get; private set; } = default!;
    public long FileSizeBytes { get; private set; }
    public string OneDriveItemId { get; private set; } = default!;
    public string OneDriveWebUrl { get; private set; } = default!;
    public Guid UploadedByUserId { get; private set; }
    public DateTime CreatedAt { get; private set; } = DateTime.UtcNow;

    // ISoftDeletable
    public DateTime? DeletedAt { get; set; }
    public Guid? DeletedBy { get; set; }

    protected WorkflowAttachment() { }

    public static WorkflowAttachment Create(
        string workflowInstanceId,
        string formKey,
        string originalFileName,
        string contentType,
        long fileSizeBytes,
        string oneDriveItemId,
        string oneDriveWebUrl,
        Guid uploadedByUserId)
    {
        FileValidationRules.Validate(originalFileName, fileSizeBytes, contentType);

        return new WorkflowAttachment
        {
            Id = Guid.NewGuid(),
            WorkflowInstanceId = workflowInstanceId,
            FormKey = formKey,
            OriginalFileName = originalFileName,
            FileExtension = Path.GetExtension(originalFileName).ToLowerInvariant(),
            ContentType = contentType,
            FileSizeBytes = fileSizeBytes,
            OneDriveItemId = oneDriveItemId,
            OneDriveWebUrl = oneDriveWebUrl,
            UploadedByUserId = uploadedByUserId,
            CreatedAt = DateTime.UtcNow
        };
    }

    public void MarkDeleted(Guid deletedByUserId)
    {
        DeletedAt = DateTime.UtcNow;
        DeletedBy = deletedByUserId;
    }
}

