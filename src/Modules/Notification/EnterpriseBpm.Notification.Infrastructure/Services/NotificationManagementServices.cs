using EnterpriseBpm.Notification.Application.Contracts;
using EnterpriseBpm.Notification.Domain.Entities;
using EnterpriseBpm.Notification.Domain.Enums;
using EnterpriseBpm.Notification.Domain.Exceptions;
using EnterpriseBpm.Notification.Domain.Services;
using EnterpriseBpm.Notification.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace EnterpriseBpm.Notification.Infrastructure.Services;

public class NotificationTemplateService : INotificationTemplateService
{
    private readonly NotificationDbContext _dbContext;

    public NotificationTemplateService(NotificationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyList<NotificationTemplateDto>> GetAllTemplatesAsync(CancellationToken cancellationToken = default)
    {
        var templates = await _dbContext.NotificationTemplates
            .AsNoTracking()
            .OrderBy(t => t.TemplateCode)
            .ToListAsync(cancellationToken);

        return templates.Select(t => new NotificationTemplateDto(
            t.Id,
            t.TemplateCode,
            t.Channel.ToString(),
            t.SubjectTemplate,
            t.BodyTemplate,
            t.IsActive,
            t.UpdatedAt
        )).ToList();
    }

    public async Task<NotificationTemplateDto> GetByCodeAsync(string code, CancellationToken cancellationToken = default)
    {
        var template = await _dbContext.NotificationTemplates
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.TemplateCode == code, cancellationToken)
            ?? throw new NotificationTemplateNotFoundException(code);

        return new NotificationTemplateDto(
            template.Id,
            template.TemplateCode,
            template.Channel.ToString(),
            template.SubjectTemplate,
            template.BodyTemplate,
            template.IsActive,
            template.UpdatedAt
        );
    }

    public async Task<NotificationTemplateDto> CreateOrUpdateTemplateAsync(CreateOrUpdateTemplateDto dto, CancellationToken cancellationToken = default)
    {
        var template = await _dbContext.NotificationTemplates
            .FirstOrDefaultAsync(t => t.TemplateCode == dto.TemplateCode, cancellationToken);

        var channel = Enum.TryParse<NotificationChannel>(dto.Channel, true, out var ch) ? ch : NotificationChannel.Email;

        if (template == null)
        {
            template = new NotificationTemplate(dto.TemplateCode, channel, dto.SubjectTemplate, dto.BodyTemplate)
            {
                IsActive = dto.IsActive,
                UpdatedAt = DateTime.UtcNow
            };
            _dbContext.NotificationTemplates.Add(template);
        }
        else
        {
            template.Channel = channel;
            template.SubjectTemplate = dto.SubjectTemplate;
            template.BodyTemplate = dto.BodyTemplate;
            template.IsActive = dto.IsActive;
            template.UpdatedAt = DateTime.UtcNow;
        }

        await _dbContext.SaveChangesAsync(cancellationToken);

        return new NotificationTemplateDto(
            template.Id,
            template.TemplateCode,
            template.Channel.ToString(),
            template.SubjectTemplate,
            template.BodyTemplate,
            template.IsActive,
            template.UpdatedAt
        );
    }

    public async Task<string> RenderPreviewAsync(string templateCode, Dictionary<string, object?> sampleData, CancellationToken cancellationToken = default)
    {
        var template = await _dbContext.NotificationTemplates
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.TemplateCode == templateCode, cancellationToken)
            ?? throw new NotificationTemplateNotFoundException(templateCode);

        return TemplateRenderer.Render(template.BodyTemplate, sampleData, sanitizeHtml: false);
    }
}

public class NotificationLogService : INotificationLogService
{
    private readonly NotificationDbContext _dbContext;

    public NotificationLogService(NotificationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyList<NotificationLogDto>> GetLogsAsync(string? status = null, int take = 50, CancellationToken cancellationToken = default)
    {
        var query = _dbContext.NotificationLogs.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(status) && Enum.TryParse<NotificationStatus>(status, true, out var st))
        {
            query = query.Where(l => l.Status == st);
        }

        var logs = await query
            .OrderByDescending(l => l.CreatedAt)
            .Take(take)
            .ToListAsync(cancellationToken);

        return logs.Select(l => new NotificationLogDto(
            l.Id,
            l.TemplateCode,
            l.RecipientEmail,
            l.RecipientPhone,
            l.Channel.ToString(),
            l.Status.ToString(),
            l.RetryCount,
            l.LastError,
            l.SourceEventId,
            l.SentAt,
            l.CreatedAt
        )).ToList();
    }

    public async Task<NotificationStatsDto> GetStatsAsync(CancellationToken cancellationToken = default)
    {
        var pending = await _dbContext.NotificationLogs.CountAsync(l => l.Status == NotificationStatus.Pending, cancellationToken);
        var sent = await _dbContext.NotificationLogs.CountAsync(l => l.Status == NotificationStatus.Sent, cancellationToken);
        var failed = await _dbContext.NotificationLogs.CountAsync(l => l.Status == NotificationStatus.Failed, cancellationToken);

        var total = sent + failed;
        var successRate = total > 0 ? Math.Round((double)sent / total * 100, 2) : 100.0;

        return new NotificationStatsDto(pending, sent, failed, successRate);
    }
}

