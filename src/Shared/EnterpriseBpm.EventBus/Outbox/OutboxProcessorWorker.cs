using System.Text.Json;
using EnterpriseBpm.EventBus.Events;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace EnterpriseBpm.EventBus.Outbox;

/// <summary>
/// Background Worker quét và phát sự kiện từ bảng Outbox định kỳ (TSK-802)
/// Đảm bảo tính nhất quán cuối cùng (Eventual Consistency) và At-Least-Once Delivery.
/// </summary>
public class OutboxProcessorWorker : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<OutboxProcessorWorker> _logger;
    private readonly TimeSpan _pollInterval = TimeSpan.FromSeconds(3);
    private const int MaxRetries = 5;

    public OutboxProcessorWorker(
        IServiceProvider serviceProvider,
        ILogger<OutboxProcessorWorker> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("[OutboxWorker] Đã khởi động Background Worker xử lý Transactional Outbox (Chu kỳ: {Interval}s).", _pollInterval.TotalSeconds);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessPendingMessagesAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[OutboxWorker] Lỗi không mong muốn trong vòng lặp quét Outbox.");
            }

            try
            {
                await Task.Delay(_pollInterval, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }

        _logger.LogInformation("[OutboxWorker] Đã dừng Background Worker xử lý Outbox an toàn.");
    }

    public async Task<int> ProcessPendingMessagesAsync(CancellationToken cancellationToken = default)
    {
        using var scope = _serviceProvider.CreateScope();
        var outboxStore = scope.ServiceProvider.GetService<IOutboxStore>();
        if (outboxStore == null)
        {
            // Nếu chưa đăng ký IOutboxStore (ví dụ môi trường test tối giản)
            return 0;
        }

        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
        var pendingMessages = await outboxStore.GetPendingMessagesAsync(50, cancellationToken);

        if (pendingMessages.Count == 0) return 0;

        int processedCount = 0;

        foreach (var message in pendingMessages)
        {
            if (cancellationToken.IsCancellationRequested) break;

            try
            {
                var eventType = Type.GetType(message.EventType);
                if (eventType == null)
                {
                    // Thử tìm trong các assembly đã tải
                    eventType = AppDomain.CurrentDomain.GetAssemblies()
                        .Select(a => a.GetType(message.EventType))
                        .FirstOrDefault(t => t != null);
                }

                if (eventType == null)
                {
                    await outboxStore.RecordFailureAsync(message.Id, $"Không thể tìm thấy kiểu dữ liệu sự kiện: {message.EventType}", cancellationToken);
                    continue;
                }

                var eventInstance = JsonSerializer.Deserialize(message.Payload, eventType);
                if (eventInstance is INotification notification)
                {
                    await mediator.Publish(notification, cancellationToken);
                    await outboxStore.MarkProcessedAsync(message.Id, cancellationToken);
                    processedCount++;
                    _logger.LogDebug("[OutboxWorker] Đã phát sự kiện thành công: {EventType} (Id: {Id})", message.EventType, message.Id);
                }
                else
                {
                    await outboxStore.RecordFailureAsync(message.Id, "Dữ liệu sự kiện không triển khai INotification", cancellationToken);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "[OutboxWorker] Lỗi khi phát sự kiện Outbox (Id: {Id}, Lần thử: {RetryCount}): {Error}",
                    message.Id, message.RetryCount + 1, ex.Message);
                await outboxStore.RecordFailureAsync(message.Id, ex.ToString(), cancellationToken);
            }
        }

        return processedCount;
    }
}

