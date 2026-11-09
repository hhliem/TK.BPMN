using EnterpriseBpm.Form.Application.Contracts;
using EnterpriseBpm.Form.Application.DTOs;
using EnterpriseBpm.Form.Domain.Entities;
using EnterpriseBpm.Form.Domain.Exceptions;
using EnterpriseBpm.Form.Domain.Services;
using EnterpriseBpm.Form.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace EnterpriseBpm.Form.Infrastructure.Services;

public class FormVersionService : IFormVersionService
{
    private readonly FormDbContext _dbContext;
    private readonly ILogger<FormVersionService> _logger;

    public FormVersionService(FormDbContext dbContext, ILogger<FormVersionService> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<Guid> RegisterFormAsync(RegisterFormCommand command, Guid? publishedBy, CancellationToken cancellationToken = default)
    {
        var sanitizedSchema = FormSchemaSanitizer.Sanitize(command.SchemaJson);

        var exists = await _dbContext.FormDefinitions
            .AnyAsync(f => f.FormKey == command.FormKey, cancellationToken);

        if (exists)
        {
            throw new InvalidOperationException($"Biểu mẫu với mã '{command.FormKey}' đã tồn tại. Hãy dùng chức năng tạo phiên bản mới (CreateNewVersion).");
        }

        var form = new FormDefinition
        {
            Id = Guid.NewGuid(),
            FormKey = command.FormKey,
            Title = command.Title,
            SchemaJson = sanitizedSchema,
            Version = 1,
            IsLatest = true,
            IsActive = true,
            ChangeNotes = "Khởi tạo phiên bản đầu tiên",
            PublishedBy = publishedBy,
            CreatedAt = DateTime.UtcNow
        };

        _dbContext.FormDefinitions.Add(form);
        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Đăng ký thành công biểu mẫu mới: {FormKey} (Version 1)", form.FormKey);
        return form.Id;
    }

    public async Task<FormDefinitionDto> CreateNewVersionAsync(
        string formKey, 
        string schemaJson, 
        string? changeNotes, 
        Guid? publishedBy, 
        CancellationToken cancellationToken = default)
    {
        var sanitizedSchema = FormSchemaSanitizer.Sanitize(schemaJson);

        // 1. Lấy phiên bản hiện hành IsLatest = 1
        var current = await _dbContext.FormDefinitions
            .FirstOrDefaultAsync(f => f.FormKey == formKey && f.IsLatest, cancellationToken)
            ?? throw new FormNotFoundException(formKey);

        // 2. Chuyển phiên bản hiện tại thành IsLatest = false
        current.IsLatest = false;
        current.UpdatedAt = DateTime.UtcNow;

        // 3. Tạo bản ghi phiên bản kế tiếp
        var newVersion = new FormDefinition
        {
            Id = Guid.NewGuid(),
            FormKey = formKey,
            Title = current.Title,
            SchemaJson = sanitizedSchema,
            Version = current.Version + 1,
            IsLatest = true,
            IsActive = true,
            ChangeNotes = changeNotes,
            PublishedBy = publishedBy,
            CreatedAt = DateTime.UtcNow
        };

        _dbContext.FormDefinitions.Add(newVersion);
        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Nâng cấp biểu mẫu '{FormKey}' lên Version {NewVersion}", formKey, newVersion.Version);

        return new FormDefinitionDto(
            newVersion.Id,
            newVersion.FormKey,
            newVersion.Title,
            newVersion.SchemaJson,
            newVersion.Version,
            newVersion.IsLatest,
            newVersion.ChangeNotes,
            newVersion.PublishedBy,
            newVersion.CreatedAt);
    }

    public async Task<FormDefinitionDto?> GetVersionAsync(string formKey, int? version, CancellationToken cancellationToken = default)
    {
        var query = _dbContext.FormDefinitions.AsNoTracking().Where(f => f.FormKey == formKey);

        if (version.HasValue)
        {
            query = query.Where(f => f.Version == version.Value);
        }
        else
        {
            query = query.Where(f => f.IsLatest);
        }

        var form = await query.FirstOrDefaultAsync(cancellationToken);
        if (form == null)
        {
            return null;
        }

        return new FormDefinitionDto(
            form.Id,
            form.FormKey,
            form.Title,
            form.SchemaJson,
            form.Version,
            form.IsLatest,
            form.ChangeNotes,
            form.PublishedBy,
            form.CreatedAt);
    }

    public async Task<FormDefinitionDto> RollbackToVersionAsync(
        string formKey, 
        int targetVersion, 
        string? rollbackReason, 
        Guid? rolledBackBy, 
        CancellationToken cancellationToken = default)
    {
        // 1. Tìm bản ghi targetVersion
        var target = await _dbContext.FormDefinitions
            .AsNoTracking()
            .FirstOrDefaultAsync(f => f.FormKey == formKey && f.Version == targetVersion, cancellationToken)
            ?? throw new FormNotFoundException(formKey, targetVersion);

        // 2. Lấy bản ghi hiện hành để xác định version cao nhất
        var currentLatest = await _dbContext.FormDefinitions
            .FirstOrDefaultAsync(f => f.FormKey == formKey && f.IsLatest, cancellationToken)
            ?? throw new FormNotFoundException(formKey);

        currentLatest.IsLatest = false;
        currentLatest.UpdatedAt = DateTime.UtcNow;

        // 3. Tạo bản ghi mới kế thừa nội dung targetVersion (Rollback An toàn: không xóa lịch sử)
        var newVersion = new FormDefinition
        {
            Id = Guid.NewGuid(),
            FormKey = formKey,
            Title = target.Title,
            SchemaJson = target.SchemaJson,
            Version = currentLatest.Version + 1,
            IsLatest = true,
            IsActive = true,
            ChangeNotes = $"[Rollback] Khôi phục từ Version {targetVersion}. Lý do: {rollbackReason ?? "Yêu cầu khôi phục phiên bản ổn định"}",
            PublishedBy = rolledBackBy,
            CreatedAt = DateTime.UtcNow
        };

        _dbContext.FormDefinitions.Add(newVersion);
        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Rollback thành công biểu mẫu '{FormKey}' từ Version {TargetVersion} thành Version {NewVersion}", formKey, targetVersion, newVersion.Version);

        return new FormDefinitionDto(
            newVersion.Id,
            newVersion.FormKey,
            newVersion.Title,
            newVersion.SchemaJson,
            newVersion.Version,
            newVersion.IsLatest,
            newVersion.ChangeNotes,
            newVersion.PublishedBy,
            newVersion.CreatedAt);
    }

    public async Task<IReadOnlyList<FormVersionHistoryDto>> GetVersionHistoryAsync(string formKey, CancellationToken cancellationToken = default)
    {
        return await _dbContext.FormDefinitions
            .AsNoTracking()
            .Where(f => f.FormKey == formKey)
            .OrderByDescending(f => f.Version)
            .Select(f => new FormVersionHistoryDto(
                f.Id,
                f.Version,
                f.IsLatest,
                f.ChangeNotes,
                f.PublishedBy,
                f.CreatedAt))
            .ToListAsync(cancellationToken);
    }
}

