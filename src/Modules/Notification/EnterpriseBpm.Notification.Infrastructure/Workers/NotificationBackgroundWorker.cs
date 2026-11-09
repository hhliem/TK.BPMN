using EnterpriseBpm.Notification.Application.Contracts;
using EnterpriseBpm.Notification.Domain.Enums;
using EnterpriseBpm.Notification.Domain.Services;
using EnterpriseBpm.Notification.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace EnterpriseBpm.Notification.Infrastructure.Workers;

public class NotificationBackgroundWorker : BackgroundService
{
    private readonly INotificationQueue _queue;
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<NotificationBackgroundWorker> _logger;

    public NotificationBackgroundWorker(
        INotificationQueue queue,
        IServiceProvider serviceProvider,
        ILogger<NotificationBackgroundWorker> logger)
    {
        _queue = queue;
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("[Notification Background Worker] Hàng đợi gửi thông báo ngầm đã khởi động.");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var item = await _queue.DequeueAsync(stoppingToken);
                await ProcessNotificationItemAsync(item, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[Notification Background Worker] Lỗi xử lý hàng đợi thông báo.");
                await Task.Delay(1000, stoppingToken);
            }
        }
    }

    private async Task ProcessNotificationItemAsync(NotificationDispatchItem item, CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<NotificationDbContext>();
        var mailProvider = scope.ServiceProvider.GetRequiredService<IMailProvider>();
        var teamsProvider = scope.ServiceProvider.GetRequiredService<ITeamsWebhookProvider>();

        // Lấy log gần nhất đang Pending
        var log = await dbContext.NotificationLogs
            .OrderByDescending(l => l.CreatedAt)
            .FirstOrDefaultAsync(l => l.TemplateCode == item.TemplateCode && l.Status == NotificationStatus.Pending, cancellationToken);

        try
        {
            var template = await dbContext.NotificationTemplates
                .AsNoTracking()
                .FirstOrDefaultAsync(t => t.TemplateCode == item.TemplateCode && t.IsActive, cancellationToken);

            var subject = template?.SubjectTemplate != null
                ? TemplateRenderer.Render(template.SubjectTemplate, item.PayloadParameters ?? new(), sanitizeHtml: false)
                : $"[BPM Notification] {item.TemplateCode}";

            var body = template != null
                ? TemplateRenderer.Render(template.BodyTemplate, item.PayloadParameters ?? new(), sanitizeHtml: true)
                : $"Thông báo tự động từ hệ thống: {item.TemplateCode}";

            if (item.Channel == NotificationChannel.Email && !string.IsNullOrWhiteSpace(item.RecipientEmail))
            {
                await mailProvider.SendEmailAsync(item.RecipientEmail, subject, body, cancellationToken);
            }
            else if (item.Channel == NotificationChannel.Teams)
            {
                await teamsProvider.SendTeamsAdaptiveCardAsync(
                    webhookUrl: string.Empty,
                    title: subject,
                    textBody: body,
                    facts: item.PayloadParameters,
                    cancellationToken: cancellationToken);
            }

            if (log != null)
            {
                log.MarkSent();
                await dbContext.SaveChangesAsync(cancellationToken);
            }
            _logger.LogInformation("[Notification Background Worker] Gửi thành công thông báo {TemplateCode} tới {Recipient}",
                item.TemplateCode, item.RecipientEmail ?? "Teams");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "[Notification Background Worker] Gửi thông báo {TemplateCode} thất bại. Ghi nhận lỗi.", item.TemplateCode);
            if (log != null)
            {
                log.RecordFailure(ex.Message);
                await dbContext.SaveChangesAsync(cancellationToken);
            }
        }
    }
}

