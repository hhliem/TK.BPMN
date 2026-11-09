using EnterpriseBpm.SharedKernel;

namespace EnterpriseBpm.EventBus.Outbox;

/// <summary>
/// Thực thể lưu trữ sự kiện trong Transactional Outbox Pattern (TSK-801)
/// Đảm bảo At-Least-Once Delivery cho các Integration Events giữa các Module.
/// </summary>
public class OutboxMessage : BaseEntity<Guid>
{
    public string EventType { get; set; } = default!;
    public string Payload { get; set; } = default!;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ProcessedAt { get; set; }
    public string? Error { get; set; }
    public int RetryCount { get; set; } = 0;

    public OutboxMessage()
    {
        Id = Guid.NewGuid();
        CreatedAt = DateTime.UtcNow;
    }

    public OutboxMessage(string eventType, string payload) : this()
    {
        EventType = eventType;
        Payload = payload;
    }

    public void MarkProcessed()
    {
        ProcessedAt = DateTime.UtcNow;
        Error = null;
    }

    public void RecordFailure(string error)
    {
        RetryCount++;
        Error = error;
    }
}

