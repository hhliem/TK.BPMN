namespace EnterpriseBpm.EventBus.Outbox;

/// <summary>
/// Hợp đồng lưu trữ và truy vấn tin nhắn Outbox cho Worker (TSK-802)
/// </summary>
public interface IOutboxStore
{
    Task<IReadOnlyList<OutboxMessage>> GetPendingMessagesAsync(int batchSize = 50, CancellationToken cancellationToken = default);
    Task MarkProcessedAsync(Guid messageId, CancellationToken cancellationToken = default);
    Task RecordFailureAsync(Guid messageId, string error, CancellationToken cancellationToken = default);
}

