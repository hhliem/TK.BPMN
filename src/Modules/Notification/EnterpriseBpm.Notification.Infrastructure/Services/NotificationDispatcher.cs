using EnterpriseBpm.Notification.Application.Contracts;
using EnterpriseBpm.Notification.Domain.Entities;
using EnterpriseBpm.Notification.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace EnterpriseBpm.Notification.Infrastructure.Services;

public class NotificationDispatcher : INotificationDispatcher
{
    private readonly NotificationDbContext _dbContext;
    private readonly INotificationQueue _queue;
    private readonly ILogger<NotificationDispatcher> _logger;

    public NotificationDispatcher(
        NotificationDbContext dbContext,
        INotificationQueue queue,
        ILogger<NotificationDispatcher> logger)
    {
        _dbContext = dbContext;
        _queue = queue;
        _logger = logger;
    }

    public async Task EnqueueNotificationAsync(NotificationDispatchItem item, CancellationToken cancellationToken = default)
    {
        // 1. Kiểm tra chống trùng lặp (Deduplication by SourceEventId)
        if (item.SourceEventId.HasValue && !string.IsNullOrEmpty(item.RecipientEmail))
        {
            var exists = await _dbContext.NotificationLogs.AnyAsync(l =>
                l.SourceEventId == item.SourceEventId.Value &&
                l.TemplateCode == item.TemplateCode &&
                l.RecipientEmail == item.RecipientEmail,
                cancellationToken);

            if (exists)
            {
                _logger.LogWarning("[Notification Deduplication] Đã phát hiện thông báo trùng lặp cho EventId {EventId}, Template {TemplateCode}, Recipient {Recipient}. Bỏ qua.",
                    item.SourceEventId, item.TemplateCode, item.RecipientEmail);
                return;
            }
        }

        // 2. Ghi nhận log khởi tạo
        var log = new NotificationLog(
            templateCode: item.TemplateCode,
            channel: item.Channel,
            recipientEmail: item.RecipientEmail,
            recipientPhone: item.RecipientPhone,
            sourceEventId: item.SourceEventId
        );

        _dbContext.NotificationLogs.Add(log);
        await _dbContext.SaveChangesAsync(cancellationToken);

        // 3. Đưa vào Bounded Channel Queue
        await _queue.EnqueueAsync(item, cancellationToken);
        _logger.LogInformation("[Notification Dispatcher] Đã đưa thông báo {TemplateCode} vào hàng đợi xử lý ngầm (LogId: {LogId})",
            item.TemplateCode, log.Id);
    }
}

