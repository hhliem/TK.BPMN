using EnterpriseBpm.SharedKernel.Contracts;
using MediatR;

namespace EnterpriseBpm.EventBus.Events;

/// <summary>
/// Hợp đồng Sự kiện Tích hợp liên module kế thừa từ MediatR INotification và IDomainEvent (TSK-801)
/// </summary>
public interface IIntegrationEvent : IDomainEvent, INotification
{
}

