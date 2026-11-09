using EnterpriseBpm.EventBus.Events;

namespace EnterpriseBpm.InternalNews.Domain.Events;

public record NewsPublishedEvent(
    Guid ArticleId,
    string Title,
    string Summary,
    string Category,
    bool IsUrgent
) : BaseIntegrationEvent;

public record NewsArchivedEvent(
    Guid ArticleId,
    string Title,
    string Category,
    DateTime ArchivedAt
) : BaseIntegrationEvent;

