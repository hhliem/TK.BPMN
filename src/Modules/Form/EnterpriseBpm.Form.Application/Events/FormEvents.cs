using EnterpriseBpm.EventBus.Events;

namespace EnterpriseBpm.Form.Application.Events;

public record FormDefinitionUpdatedEvent(
    string FormKey,
    int OldVersion,
    int NewVersion
) : BaseIntegrationEvent;

