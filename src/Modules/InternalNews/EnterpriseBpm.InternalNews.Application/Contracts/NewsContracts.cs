namespace EnterpriseBpm.InternalNews.Application.Contracts;

public record NewsArticleSummaryDto(
    Guid Id,
    string Title,
    string Summary,
    string? CoverImageUrl,
    string Category,
    string Status,
    bool IsMandatoryRead,
    DateTime? PublishedAt,
    DateTime? ScheduledPublishAt,
    DateTime CreatedAt,
    int TotalReads
);

public record NewsArticleDetailDto(
    Guid Id,
    string Title,
    string Summary,
    string ContentHtml,
    string? CoverImageUrl,
    string Category,
    string Status,
    bool IsMandatoryRead,
    bool HasRead,
    DateTime? ReadAt,
    DateTime? PublishedAt,
    DateTime? ScheduledPublishAt,
    DateTime? ExpiresAt,
    DateTime CreatedAt,
    Guid AuthorId,
    int TotalReads
);

public record CreateNewsArticleDto(
    string Title,
    string Summary,
    string ContentHtml,
    string Category,
    bool IsMandatoryRead = false,
    string? CoverImageUrl = null,
    DateTime? ScheduledPublishAt = null,
    DateTime? ExpiresAt = null,
    Guid? TargetOrgUnitId = null
);

public record UpdateNewsArticleDto(
    string Title,
    string Summary,
    string ContentHtml,
    string Category,
    bool IsMandatoryRead,
    string? CoverImageUrl,
    DateTime? ScheduledPublishAt,
    DateTime? ExpiresAt,
    Guid? TargetOrgUnitId
);

public record NewsComplianceStatsDto(
    Guid ArticleId,
    string Title,
    bool IsMandatoryRead,
    int TotalAcknowledged,
    int TotalTargetEmployees,
    double ComplianceRate
);

public interface INewsService
{
    Task<IReadOnlyList<NewsArticleSummaryDto>> GetArticlesAsync(string? category = null, string? status = null, int pageNumber = 1, int pageSize = 20, CancellationToken cancellationToken = default);
    Task<NewsArticleDetailDto> GetArticleByIdAsync(Guid articleId, Guid? currentUserId = null, CancellationToken cancellationToken = default);
    Task<NewsArticleDetailDto> CreateArticleAsync(CreateNewsArticleDto dto, Guid authorId, CancellationToken cancellationToken = default);
    Task<NewsArticleDetailDto> UpdateArticleAsync(Guid articleId, UpdateNewsArticleDto dto, Guid updaterId, CancellationToken cancellationToken = default);
    Task PublishArticleAsync(Guid articleId, Guid publisherId, CancellationToken cancellationToken = default);
    Task ArchiveArticleAsync(Guid articleId, Guid archiverId, CancellationToken cancellationToken = default);
    Task AcknowledgeReadAsync(Guid articleId, Guid employeeId, CancellationToken cancellationToken = default);
    Task<NewsComplianceStatsDto> GetComplianceStatsAsync(Guid articleId, CancellationToken cancellationToken = default);
}

