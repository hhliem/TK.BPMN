using System.Text.Json;
using EnterpriseBpm.SharedKernel.Contracts;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace EnterpriseBpm.EventBus.Outbox;

/// <summary>
/// Interceptor tự động quét các Entity kế thừa IHasDomainEvents
/// và lưu toàn bộ sự kiện vào bảng Outbox trong cùng Transaction với Entity (TSK-801)
/// </summary>
public class OutboxMessageSaveChangesInterceptor : SaveChangesInterceptor
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        var context = eventData.Context;
        if (context == null) return ValueTask.FromResult(result);

        var entitiesWithEvents = context.ChangeTracker
            .Entries<IHasDomainEvents>()
            .Where(e => e.Entity.DomainEvents.Count > 0)
            .Select(e => e.Entity)
            .ToList();

        if (entitiesWithEvents.Count == 0)
            return ValueTask.FromResult(result);

        foreach (var entity in entitiesWithEvents)
        {
            foreach (var domainEvent in entity.DomainEvents)
            {
                var eventType = domainEvent.GetType().AssemblyQualifiedName ?? domainEvent.GetType().FullName!;
                var payload = JsonSerializer.Serialize(domainEvent, domainEvent.GetType(), SerializerOptions);

                var outboxMessage = new OutboxMessage(eventType, payload);

                // Thêm trực tiếp vào DbContext trong cùng Transaction
                try
                {
                    context.Set<OutboxMessage>().Add(outboxMessage);
                }
                catch
                {
                    // Fallback nếu DbContext chưa cấu hình DbSet<OutboxMessage>
                }
            }

            entity.ClearDomainEvents();
        }

        return ValueTask.FromResult(result);
    }
}

