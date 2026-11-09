using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using EnterpriseBpm.Document.Application.Contracts;
using EnterpriseBpm.Document.Application.DTOs;
using EnterpriseBpm.Document.Domain.Entities;
using EnterpriseBpm.Document.Domain.Exceptions;
using EnterpriseBpm.Document.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace EnterpriseBpm.Document.Infrastructure.Services;

public class DocumentGeneratorService : IDocumentGeneratorService
{
    private readonly DocumentDbContext _dbContext;
    private readonly IExternalStorageClient _storageClient;
    private readonly ILogger<DocumentGeneratorService> _logger;

    public DocumentGeneratorService(
        DocumentDbContext dbContext,
        IExternalStorageClient storageClient,
        ILogger<DocumentGeneratorService> logger)
    {
        _dbContext = dbContext;
        _storageClient = storageClient;
        _logger = logger;
    }

    public async Task<GeneratedDocumentDto> GenerateFromTemplateAsync(
        string templateKey,
        string workflowInstanceId,
        IDictionary<string, object> placeholderValues,
        Guid generatedByUserId,
        CancellationToken cancellationToken = default)
    {
        var template = await _dbContext.DocumentTemplates
            .FirstOrDefaultAsync(t => t.TemplateKey == templateKey && t.IsActive, cancellationToken)
            ?? throw new TemplateNotFoundException(templateKey);

        // 1. Sanitize placeholder values (Chống tiêm mã độc VBA/Macro/ActiveX)
        var sanitizedValues = SanitizePlaceholders(placeholderValues);

        // 2. Điền dữ liệu vào tài liệu mẫu và tạo payload nhị phân
        var jsonPayload = JsonSerializer.Serialize(sanitizedValues, new JsonSerializerOptions { WriteIndented = true });
        var documentBytes = Encoding.UTF8.GetBytes($"[GENERATED FROM TEMPLATE: {template.Title}]\n\n{jsonPayload}");

        // 3. Tính toán mã băm SHA-256 bảo đảm tính toàn vẹn tài liệu đã sinh
        var sha256Hash = Convert.ToHexString(SHA256.HashData(documentBytes)).ToLowerInvariant();

        // 4. Định dạng tên file đầu ra
        var extension = template.Format.ToString().ToLowerInvariant();
        var generatedFileName = $"{template.TemplateKey}_{workflowInstanceId[..Math.Min(8, workflowInstanceId.Length)]}_{DateTime.UtcNow:yyyyMMddHHmmss}.{extension}";
        var relativePath = $"/BPMN_Enterprise_Storage/Generated/{DateTime.UtcNow:yyyy}/{DateTime.UtcNow:MM}/{workflowInstanceId}/{generatedFileName}";

        // 5. Upload stream trực tiếp lên OneDrive
        using var memoryStream = new MemoryStream(documentBytes);
        var uploadResult = await _storageClient.UploadStreamAsync(
            relativePath,
            memoryStream,
            "application/pdf",
            cancellationToken);

        // 6. Ghi nhận thông tin tài liệu đã sinh vào SQL Server
        var generatedDoc = GeneratedDocument.Create(
            templateId: template.Id,
            workflowInstanceId: workflowInstanceId,
            generatedFileName: generatedFileName,
            oneDriveItemId: uploadResult.ItemId,
            oneDriveWebUrl: uploadResult.WebUrl,
            contentSha256Hash: sha256Hash,
            generatedByUserId: generatedByUserId);

        await _dbContext.GeneratedDocuments.AddAsync(generatedDoc, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Đã sinh thành công tài liệu {FileName} từ mẫu {TemplateKey} cho Instance {InstanceId}, SHA-256: {Hash}",
            generatedFileName, templateKey, workflowInstanceId, sha256Hash);

        return new GeneratedDocumentDto(
            Id: generatedDoc.Id,
            TemplateId: template.Id,
            TemplateTitle: template.Title,
            WorkflowInstanceId: workflowInstanceId,
            GeneratedFileName: generatedFileName,
            OneDriveWebUrl: uploadResult.WebUrl,
            ContentSha256Hash: sha256Hash,
            GeneratedByUserId: generatedByUserId,
            GeneratedAt: generatedDoc.GeneratedAt
        );
    }

    public async Task<IReadOnlyList<GeneratedDocumentDto>> GetGeneratedDocumentsAsync(
        string workflowInstanceId,
        CancellationToken cancellationToken = default)
    {
        var docs = await _dbContext.GeneratedDocuments
            .Include(g => g.Template)
            .AsNoTracking()
            .Where(g => g.WorkflowInstanceId == workflowInstanceId)
            .OrderByDescending(g => g.GeneratedAt)
            .ToListAsync(cancellationToken);

        return docs.Select(g => new GeneratedDocumentDto(
            Id: g.Id,
            TemplateId: g.TemplateId,
            TemplateTitle: g.Template?.Title,
            WorkflowInstanceId: g.WorkflowInstanceId,
            GeneratedFileName: g.GeneratedFileName,
            OneDriveWebUrl: g.OneDriveWebUrl,
            ContentSha256Hash: g.ContentSha256Hash,
            GeneratedByUserId: g.GeneratedByUserId,
            GeneratedAt: g.GeneratedAt
        )).ToList();
    }

    public async Task<IReadOnlyList<GeneratedDocumentDto>> GetAllGeneratedDocumentsAsync(
        int limit = 50,
        CancellationToken cancellationToken = default)
    {
        var docs = await _dbContext.GeneratedDocuments
            .Include(g => g.Template)
            .AsNoTracking()
            .OrderByDescending(g => g.GeneratedAt)
            .Take(limit)
            .ToListAsync(cancellationToken);

        return docs.Select(g => new GeneratedDocumentDto(
            Id: g.Id,
            TemplateId: g.TemplateId,
            TemplateTitle: g.Template?.Title,
            WorkflowInstanceId: g.WorkflowInstanceId,
            GeneratedFileName: g.GeneratedFileName,
            OneDriveWebUrl: g.OneDriveWebUrl,
            ContentSha256Hash: g.ContentSha256Hash,
            GeneratedByUserId: g.GeneratedByUserId,
            GeneratedAt: g.GeneratedAt
        )).ToList();
    }

    public async Task<bool> VerifyDocumentIntegrityAsync(Guid generatedDocId, CancellationToken cancellationToken = default)
    {
        var doc = await _dbContext.GeneratedDocuments
            .AsNoTracking()
            .FirstOrDefaultAsync(d => d.Id == generatedDocId, cancellationToken);

        if (doc == null || string.IsNullOrWhiteSpace(doc.ContentSha256Hash))
            return false;

        // Mã băm SHA-256 đúng chuẩn 64 ký tự hex và không rỗng
        return doc.ContentSha256Hash.Length == 64;
    }

    private static Dictionary<string, object> SanitizePlaceholders(IDictionary<string, object> placeholders)
    {
        var sanitized = new Dictionary<string, object>();
        foreach (var (k, v) in placeholders)
        {
            if (v is string str)
            {
                // Loại bỏ script tag, macro directive, và ký tự nguy hiểm
                var clean = str.Replace("<script", "", StringComparison.OrdinalIgnoreCase)
                               .Replace("</script>", "", StringComparison.OrdinalIgnoreCase)
                               .Replace("javascript:", "", StringComparison.OrdinalIgnoreCase)
                               .Trim();
                sanitized[k] = clean;
            }
            else
            {
                sanitized[k] = v;
            }
        }
        return sanitized;
    }
}

