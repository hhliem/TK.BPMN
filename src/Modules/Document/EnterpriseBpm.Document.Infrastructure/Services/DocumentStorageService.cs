using EnterpriseBpm.Document.Application.Contracts;
using EnterpriseBpm.Document.Application.DTOs;
using EnterpriseBpm.Document.Domain.Entities;
using EnterpriseBpm.Document.Domain.Exceptions;
using EnterpriseBpm.Document.Domain.Services;
using EnterpriseBpm.Document.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace EnterpriseBpm.Document.Infrastructure.Services;

public class DocumentStorageService : IDocumentStorageService
{
    private readonly DocumentDbContext _dbContext;
    private readonly IExternalStorageClient _storageClient;
    private readonly ILogger<DocumentStorageService> _logger;

    public DocumentStorageService(
        DocumentDbContext dbContext,
        IExternalStorageClient storageClient,
        ILogger<DocumentStorageService> logger)
    {
        _dbContext = dbContext;
        _storageClient = storageClient;
        _logger = logger;
    }

    public async Task<AttachmentDto> UploadFileAsync(
        string workflowInstanceId,
        string formKey,
        string fileName,
        Stream fileStream,
        string contentType,
        Guid uploadedByUserId,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(workflowInstanceId))
            throw new ArgumentException("WorkflowInstanceId không được để trống.", nameof(workflowInstanceId));
        if (string.IsNullOrWhiteSpace(formKey))
            throw new ArgumentException("FormKey không được để trống.", nameof(formKey));

        // 1. Kiểm soát định dạng & dung lượng tối đa 25MB (File Size Guard & Whitelist)
        FileValidationRules.Validate(fileName, fileStream.Length, contentType);

        // 2. Định tuyến cây thư mục OneDrive logic: /BPMN_Enterprise_Storage/{Year}/{Month}/{InstanceId}
        var year = DateTime.UtcNow.Year;
        var month = DateTime.UtcNow.Month.ToString("D2");
        var sanitizedFileName = Path.GetFileName(fileName);
        var uniqueFileName = $"{Guid.NewGuid():N}_{sanitizedFileName}";
        var relativePath = $"/BPMN_Enterprise_Storage/{year}/{month}/{workflowInstanceId}/{uniqueFileName}";

        // 3. Truyền stream trực tiếp lên OneDrive thông qua ExternalStorageClient (Zero Binary in DB)
        var uploadResult = await _storageClient.UploadStreamAsync(relativePath, fileStream, contentType, cancellationToken);

        // 4. Lưu Metadata tham chiếu vào SQL Server
        var attachment = WorkflowAttachment.Create(
            workflowInstanceId: workflowInstanceId,
            formKey: formKey,
            originalFileName: sanitizedFileName,
            contentType: contentType,
            fileSizeBytes: fileStream.Length,
            oneDriveItemId: uploadResult.ItemId,
            oneDriveWebUrl: uploadResult.WebUrl,
            uploadedByUserId: uploadedByUserId);

        await _dbContext.WorkflowAttachments.AddAsync(attachment, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Đã lưu tệp đính kèm {FileName} ({SizeBytes} bytes) cho Instance {InstanceId}, AttachmentId: {AttachmentId}",
            sanitizedFileName, fileStream.Length, workflowInstanceId, attachment.Id);

        return MapToDto(attachment);
    }

    public async Task<SecurePreviewUrlDto> GetSecurePreviewUrlAsync(
        Guid attachmentId,
        CancellationToken cancellationToken = default)
    {
        var attachment = await _dbContext.WorkflowAttachments
            .AsNoTracking()
            .FirstOrDefaultAsync(a => a.Id == attachmentId, cancellationToken)
            ?? throw new DocumentNotFoundException(attachmentId.ToString());

        var expiration = TimeSpan.FromMinutes(15);
        var previewUrl = await _storageClient.CreateSharingLinkAsync(attachment.OneDriveItemId, expiration, cancellationToken);

        return new SecurePreviewUrlDto(
            AttachmentId: attachment.Id,
            PreviewUrl: previewUrl,
            ExpiresAt: DateTime.UtcNow.Add(expiration),
            IsTimeLimited: true
        );
    }

    public async Task<IReadOnlyList<AttachmentDto>> GetAttachmentsByWorkflowAsync(
        string workflowInstanceId,
        CancellationToken cancellationToken = default)
    {
        var attachments = await _dbContext.WorkflowAttachments
            .AsNoTracking()
            .Where(a => a.WorkflowInstanceId == workflowInstanceId)
            .OrderByDescending(a => a.CreatedAt)
            .ToListAsync(cancellationToken);

        return attachments.Select(MapToDto).ToList();
    }

    public async Task<AttachmentDto?> GetAttachmentByIdAsync(Guid attachmentId, CancellationToken cancellationToken = default)
    {
        var attachment = await _dbContext.WorkflowAttachments
            .AsNoTracking()
            .FirstOrDefaultAsync(a => a.Id == attachmentId, cancellationToken);

        return attachment != null ? MapToDto(attachment) : null;
    }

    public async Task DeleteAttachmentAsync(Guid attachmentId, Guid deletedByUserId, CancellationToken cancellationToken = default)
    {
        var attachment = await _dbContext.WorkflowAttachments
            .FirstOrDefaultAsync(a => a.Id == attachmentId, cancellationToken)
            ?? throw new DocumentNotFoundException(attachmentId.ToString());

        attachment.MarkDeleted(deletedByUserId);
        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Đã đánh dấu xóa mềm tệp đính kèm {AttachmentId} bởi người dùng {UserId}", attachmentId, deletedByUserId);
    }

    private static AttachmentDto MapToDto(WorkflowAttachment a) =>
        new(
            Id: a.Id,
            WorkflowInstanceId: a.WorkflowInstanceId,
            FormKey: a.FormKey,
            OriginalFileName: a.OriginalFileName,
            FileExtension: a.FileExtension,
            ContentType: a.ContentType,
            FileSizeBytes: a.FileSizeBytes,
            OneDriveWebUrl: a.OneDriveWebUrl,
            UploadedByUserId: a.UploadedByUserId,
            CreatedAt: a.CreatedAt
        );
}

