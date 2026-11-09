namespace EnterpriseBpm.Document.Application.DTOs;

public record AttachmentDto(
    Guid Id,
    string WorkflowInstanceId,
    string FormKey,
    string OriginalFileName,
    string FileExtension,
    string ContentType,
    long FileSizeBytes,
    string OneDriveWebUrl,
    Guid UploadedByUserId,
    DateTime CreatedAt
);

public record DocumentTemplateDto(
    Guid Id,
    string TemplateKey,
    string Title,
    string? Description,
    string Format,
    string OneDriveTemplateItemId,
    string PlaceholderSchema,
    bool IsActive,
    DateTime CreatedAt
);

public record CreateDocumentTemplateCommand(
    string TemplateKey,
    string Title,
    string? Description,
    string Format,
    string OneDriveTemplateItemId,
    string PlaceholderSchema
);

public record UpdateDocumentTemplateCommand(
    string Title,
    string? Description,
    string PlaceholderSchema
);

public record GeneratedDocumentDto(
    Guid Id,
    Guid TemplateId,
    string? TemplateTitle,
    string WorkflowInstanceId,
    string GeneratedFileName,
    string OneDriveWebUrl,
    string ContentSha256Hash,
    Guid GeneratedByUserId,
    DateTime GeneratedAt
);

public record GenerateDocumentRequest(
    string TemplateKey,
    Dictionary<string, object> PlaceholderValues
);

public record SecurePreviewUrlDto(
    Guid AttachmentId,
    string PreviewUrl,
    DateTime ExpiresAt,
    bool IsTimeLimited
);

