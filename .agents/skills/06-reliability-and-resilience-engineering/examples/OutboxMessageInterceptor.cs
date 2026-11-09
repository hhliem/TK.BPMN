using System.Text.Json;
using EnterpriseBpm.SharedKernel.Events;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace EnterpriseBpm.Workflow.Infrastructure.Interceptors;

public class OutboxMessage
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string EventType { get; set; } = default!;
    public string PayloadJson { get; set; } = default!;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ProcessedAt { get; set; }
    public int RetryCount { get; set; }
    public string? LastError { get; set; }
}

public interface IHasDomainEvents
{
    IReadOnlyList<IIntegrationEvent> DomainEvents { get; }
    void ClearDomainEvents();
}

/// <summary>
/// Tự động bắt các Integration Events phát sinh từ Domain Entities và lưu vào bảng OutboxMessages
/// trong cùng Database Transaction khi gọi SaveChangesAsync.
/// </summary>
public class OutboxMessageSaveChangesInterceptor : SaveChangesInterceptor
{
    public override async ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        var context = eventData.Context;
        if (context == null) return result;

        var entitiesWithEvents = context.ChangeTracker.Entries<IHasDomainEvents>()
            .Where(e => e.Entity.DomainEvents.Any())
            .Select(e => e.Entity)
            .ToList();

        if (!entitiesWithEvents.Any()) return result;

        var outboxMessages = new List<OutboxMessage>();

        foreach (var entity in entitiesWithEvents)
        {
            foreach (var domainEvent in entity.DomainEvents)
            {
                outboxMessages.Add(new OutboxMessage
                {
                    EventType = domainEvent.GetType().AssemblyQualifiedName ?? domainEvent.GetType().Name,
                    PayloadJson = JsonSerializer.Serialize(domainEvent, domainEvent.GetType()),
                    CreatedAt = DateTime.UtcNow
                });
            }

            entity.ClearDomainEvents();
        }

        await context.Set<OutboxMessage>().AddRangeAsync(outboxMessages, cancellationToken);

        return result;
    }
}

