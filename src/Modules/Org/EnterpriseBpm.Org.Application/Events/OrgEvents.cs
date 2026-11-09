using EnterpriseBpm.EventBus.Events;

namespace EnterpriseBpm.Org.Application.Events;

public record DelegationAssignedEvent(
    Guid DelegationId,
    Guid DelegatorId,
    Guid DelegateeId,
    DateTime FromDate,
    DateTime ToDate,
    string? ScopeFormKey
) : BaseIntegrationEvent;

public record EmployeeDeactivatedEvent(
    Guid EmployeeId,
    Guid OrgUnitId,
    string EmployeeCode,
    string FullName,
    DateTime DeactivatedAt,
    Guid DeactivatedBy
) : BaseIntegrationEvent;

public record OrgUnitRestructuredEvent(
    Guid OrgUnitId,
    Guid? OldParentId,
    Guid? NewParentId,
    string OrgUnitCode,
    string ActionType   // "Created", "Moved", "Deactivated"
) : BaseIntegrationEvent;
