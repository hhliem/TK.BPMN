namespace EnterpriseBpm.SharedKernel.Contracts;

/// <summary>
/// Giao diện đại diện cho một Domain / Integration Event cơ bản (TSK-801)
/// </summary>
public interface IDomainEvent
{
    Guid EventId { get; }
    DateTime OccurredOn { get; }
}

/// <summary>
/// Giao diện cho các Entity hoặc Aggregate Root có khả năng phát sinh Domain Events (TSK-801)
/// </summary>
public interface IHasDomainEvents
{
    IReadOnlyList<IDomainEvent> DomainEvents { get; }
    void AddDomainEvent(IDomainEvent domainEvent);
    void ClearDomainEvents();
}

