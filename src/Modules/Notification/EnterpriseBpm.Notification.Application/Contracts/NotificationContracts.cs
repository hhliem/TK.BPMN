using EnterpriseBpm.Notification.Domain.Enums;

namespace EnterpriseBpm.Notification.Application.Contracts;

public record NotificationDispatchItem(
    string TemplateCode,
    NotificationChannel Channel,
    string? RecipientEmail,
    Guid? RecipientUserId = null,
    string? RecipientPhone = null,
    Guid? SourceEventId = null,
    Dictionary<string, object?>? PayloadParameters = null
);

public record NotificationTemplateDto(
    Guid Id,
    string TemplateCode,
    string Channel,
    string? SubjectTemplate,
    string BodyTemplate,
    bool IsActive,
    DateTime UpdatedAt
);

public record CreateOrUpdateTemplateDto(
    string TemplateCode,
    string Channel,
    string? SubjectTemplate,
    string BodyTemplate,
    bool IsActive = true
);

public record NotificationLogDto(
    Guid Id,
    string TemplateCode,
    string? RecipientEmail,
    string? RecipientPhone,
    string Channel,
    string Status,
    int RetryCount,
    string? LastError,
    Guid? SourceEventId,
    DateTime? SentAt,
    DateTime CreatedAt
);

public record NotificationStatsDto(
    int TotalPending,
    int TotalSent,
    int TotalFailed,
    double SuccessRate
);

public interface INotificationDispatcher
{
    Task EnqueueNotificationAsync(NotificationDispatchItem item, CancellationToken cancellationToken = default);
}

public interface INotificationQueue
{
    ValueTask EnqueueAsync(NotificationDispatchItem item, CancellationToken cancellationToken = default);
    ValueTask<NotificationDispatchItem> DequeueAsync(CancellationToken cancellationToken = default);
}

public interface IMailProvider
{
    Task SendEmailAsync(string recipientEmail, string subject, string htmlBody, CancellationToken cancellationToken = default);
}

public interface ITeamsWebhookProvider
{
    Task SendTeamsAdaptiveCardAsync(string webhookUrl, string title, string textBody, IDictionary<string, object?>? facts = null, CancellationToken cancellationToken = default);
}

public interface INotificationTemplateService
{
    Task<IReadOnlyList<NotificationTemplateDto>> GetAllTemplatesAsync(CancellationToken cancellationToken = default);
    Task<NotificationTemplateDto> GetByCodeAsync(string code, CancellationToken cancellationToken = default);
    Task<NotificationTemplateDto> CreateOrUpdateTemplateAsync(CreateOrUpdateTemplateDto dto, CancellationToken cancellationToken = default);
    Task<string> RenderPreviewAsync(string templateCode, Dictionary<string, object?> sampleData, CancellationToken cancellationToken = default);
}

public interface INotificationLogService
{
    Task<IReadOnlyList<NotificationLogDto>> GetLogsAsync(string? status = null, int take = 50, CancellationToken cancellationToken = default);
    Task<NotificationStatsDto> GetStatsAsync(CancellationToken cancellationToken = default);
}

