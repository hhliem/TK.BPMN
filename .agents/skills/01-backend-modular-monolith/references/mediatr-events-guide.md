# Hướng dẫn MediatR Integration Events

## Định nghĩa Sự kiện Cơ sở
```csharp
namespace EnterpriseBpm.SharedKernel.Events;

public interface IIntegrationEvent : INotification
{
    Guid EventId { get; }
    DateTime OccurredOn { get; }
}

public abstract record BaseIntegrationEvent : IIntegrationEvent
{
    public Guid EventId { get; init; } = Guid.NewGuid();
    public DateTime OccurredOn { get; init; } = DateTime.UtcNow;
}
```

## Luồng Xử lý Mẫu
1. **WorkflowEngine** phê duyệt đơn -> Gọi `_mediator.Publish(new WorkflowApprovedEvent(...))`.
2. **Notification Module** bắt `INotificationHandler<WorkflowApprovedEvent>` -> Đẩy email vào Queue.
3. **Analytics Module** bắt `INotificationHandler<WorkflowApprovedEvent>` -> Cập nhật chỉ số `WorkflowSummaryMetrics`.
