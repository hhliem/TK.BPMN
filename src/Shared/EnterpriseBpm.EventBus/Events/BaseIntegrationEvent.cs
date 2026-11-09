namespace EnterpriseBpm.EventBus.Events;

/// <summary>
/// Record cơ sở cho toàn bộ Integration Events trong hệ thống
/// </summary>
public abstract record BaseIntegrationEvent : IIntegrationEvent
{
    public Guid EventId { get; init; } = Guid.NewGuid();
    public DateTime OccurredOn { get; init; } = DateTime.UtcNow;
}

