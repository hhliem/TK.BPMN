using EnterpriseBpm.Document.Application.Contracts;
using EnterpriseBpm.Document.Application.DTOs;
using EnterpriseBpm.Document.Domain.Entities;
using EnterpriseBpm.Document.Domain.Enums;
using EnterpriseBpm.Document.Domain.Exceptions;
using EnterpriseBpm.Document.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace EnterpriseBpm.Document.Infrastructure.Services;

public class DocumentTemplateService : IDocumentTemplateService
{
    private readonly DocumentDbContext _dbContext;
    private readonly ILogger<DocumentTemplateService> _logger;

    public DocumentTemplateService(
        DocumentDbContext dbContext,
        ILogger<DocumentTemplateService> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<Guid> CreateTemplateAsync(CreateDocumentTemplateCommand command, CancellationToken cancellationToken = default)
    {
        var existing = await _dbContext.DocumentTemplates
            .AnyAsync(t => t.TemplateKey == command.TemplateKey, cancellationToken);

        if (existing)
        {
            throw new InvalidOperationException($"Mã mẫu tài liệu '{command.TemplateKey}' đã tồn tại.");
        }

        if (!Enum.TryParse<TemplateFormat>(command.Format, true, out var formatEnum))
        {
            throw new ArgumentException($"Định dạng mẫu '{command.Format}' không hợp lệ (hỗ trợ: Docx, Pdf, Xlsx).", nameof(command.Format));
        }

        var template = DocumentTemplate.Create(
            templateKey: command.TemplateKey,
            title: command.Title,
            format: formatEnum,
            oneDriveTemplateItemId: command.OneDriveTemplateItemId,
            placeholderSchema: command.PlaceholderSchema,
            description: command.Description);

        await _dbContext.DocumentTemplates.AddAsync(template, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Đã khởi tạo thành công mẫu tài liệu {TemplateKey} (ID: {TemplateId})", template.TemplateKey, template.Id);
        return template.Id;
    }

    public async Task UpdateTemplateAsync(Guid templateId, UpdateDocumentTemplateCommand command, CancellationToken cancellationToken = default)
    {
        var template = await _dbContext.DocumentTemplates
            .FirstOrDefaultAsync(t => t.Id == templateId, cancellationToken)
            ?? throw new TemplateNotFoundException(templateId.ToString());

        template.Update(command.Title, command.Description, command.PlaceholderSchema);
        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Đã cập nhật thông tin mẫu tài liệu {TemplateId}", templateId);
    }

    public async Task<DocumentTemplateDto?> GetTemplateByKeyAsync(string templateKey, CancellationToken cancellationToken = default)
    {
        var template = await _dbContext.DocumentTemplates
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.TemplateKey == templateKey, cancellationToken);

        return template != null ? MapToDto(template) : null;
    }

    public async Task<IReadOnlyList<DocumentTemplateDto>> GetAllTemplatesAsync(bool? activeOnly = null, CancellationToken cancellationToken = default)
    {
        var query = _dbContext.DocumentTemplates.AsNoTracking();
        if (activeOnly.HasValue && activeOnly.Value)
        {
            query = query.Where(t => t.IsActive);
        }

        var list = await query.OrderBy(t => t.Title).ToListAsync(cancellationToken);
        return list.Select(MapToDto).ToList();
    }

    private static DocumentTemplateDto MapToDto(DocumentTemplate t) =>
        new(
            Id: t.Id,
            TemplateKey: t.TemplateKey,
            Title: t.Title,
            Description: t.Description,
            Format: t.Format.ToString(),
            OneDriveTemplateItemId: t.OneDriveTemplateItemId,
            PlaceholderSchema: t.PlaceholderSchema,
            IsActive: t.IsActive,
            CreatedAt: t.CreatedAt
        );
}

