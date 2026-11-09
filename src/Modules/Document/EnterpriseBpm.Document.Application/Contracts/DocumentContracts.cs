using EnterpriseBpm.Document.Application.DTOs;

namespace EnterpriseBpm.Document.Application.Contracts;

public record StorageUploadResult(string ItemId, string WebUrl);

public interface IExternalStorageClient
{
    Task<StorageUploadResult> UploadStreamAsync(
        string relativePath,
        Stream stream,
        string contentType,
        CancellationToken cancellationToken = default);

    Task<string> CreateSharingLinkAsync(
        string itemId,
        TimeSpan expiration,
        CancellationToken cancellationToken = default);
}

public interface IDocumentStorageService
{
    Task<AttachmentDto> UploadFileAsync(
        string workflowInstanceId,
        string formKey,
        string fileName,
        Stream fileStream,
        string contentType,
        Guid uploadedByUserId,
        CancellationToken cancellationToken = default);

    Task<SecurePreviewUrlDto> GetSecurePreviewUrlAsync(
        Guid attachmentId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<AttachmentDto>> GetAttachmentsByWorkflowAsync(
        string workflowInstanceId,
        CancellationToken cancellationToken = default);

    Task<AttachmentDto?> GetAttachmentByIdAsync(
        Guid attachmentId,
        CancellationToken cancellationToken = default);

    Task DeleteAttachmentAsync(
        Guid attachmentId,
        Guid deletedByUserId,
        CancellationToken cancellationToken = default);
}

public interface IDocumentTemplateService
{
    Task<Guid> CreateTemplateAsync(
        CreateDocumentTemplateCommand command,
        CancellationToken cancellationToken = default);

    Task UpdateTemplateAsync(
        Guid templateId,
        UpdateDocumentTemplateCommand command,
        CancellationToken cancellationToken = default);

    Task<DocumentTemplateDto?> GetTemplateByKeyAsync(
        string templateKey,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<DocumentTemplateDto>> GetAllTemplatesAsync(
        bool? activeOnly = null,
        CancellationToken cancellationToken = default);
}

public interface IDocumentGeneratorService
{
    Task<GeneratedDocumentDto> GenerateFromTemplateAsync(
        string templateKey,
        string workflowInstanceId,
        IDictionary<string, object> placeholderValues,
        Guid generatedByUserId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<GeneratedDocumentDto>> GetGeneratedDocumentsAsync(
        string workflowInstanceId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<GeneratedDocumentDto>> GetAllGeneratedDocumentsAsync(
        int limit = 50,
        CancellationToken cancellationToken = default);

    Task<bool> VerifyDocumentIntegrityAsync(
        Guid generatedDocId,
        CancellationToken cancellationToken = default);
}

