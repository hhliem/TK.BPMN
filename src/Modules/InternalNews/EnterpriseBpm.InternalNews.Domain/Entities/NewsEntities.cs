using EnterpriseBpm.InternalNews.Domain.Enums;
using EnterpriseBpm.SharedKernel;
using EnterpriseBpm.SharedKernel.Contracts;

namespace EnterpriseBpm.InternalNews.Domain.Entities;

public class NewsArticle : BaseEntity, IAuditable
{
    public string Title { get; set; } = string.Empty;
    public string Summary { get; set; } = string.Empty;
    public string ContentHtml { get; set; } = string.Empty;
    public string? CoverImageUrl { get; set; }
    public NewsCategory Category { get; set; } = NewsCategory.General;
    public Guid AuthorId { get; set; }
    public Guid? TargetOrgUnitId { get; set; }
    public NewsStatus Status { get; set; } = NewsStatus.Draft;
    public bool IsMandatoryRead { get; set; } = false;

    public DateTime? PublishedAt { get; set; }
    public DateTime? ScheduledPublishAt { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public DateTime? ArchivedAt { get; set; }

    // IAuditable
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public Guid? CreatedBy { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public Guid? UpdatedBy { get; set; }

    public ICollection<NewsReadReceipt> ReadReceipts { get; set; } = new List<NewsReadReceipt>();

    public NewsArticle() { }

    public NewsArticle(
        string title,
        string summary,
        string contentHtml,
        NewsCategory category,
        Guid authorId,
        bool isMandatoryRead = false,
        string? coverImageUrl = null,
        DateTime? scheduledPublishAt = null,
        DateTime? expiresAt = null,
        Guid? targetOrgUnitId = null)
    {
        Title = title;
        Summary = summary;
        ContentHtml = contentHtml;
        Category = category;
        AuthorId = authorId;
        CreatedBy = authorId;
        IsMandatoryRead = isMandatoryRead;
        CoverImageUrl = coverImageUrl;
        ScheduledPublishAt = scheduledPublishAt;
        ExpiresAt = expiresAt;
        TargetOrgUnitId = targetOrgUnitId;
        Status = NewsStatus.Draft;
        CreatedAt = DateTime.UtcNow;
    }

    public void Publish(Guid? actorUserId = null)
    {
        Status = NewsStatus.Published;
        PublishedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
        UpdatedBy = actorUserId;
    }

    public void Archive(Guid? actorUserId = null)
    {
        Status = NewsStatus.Archived;
        ArchivedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
        UpdatedBy = actorUserId;
    }
}

public class NewsReadReceipt
{
    public Guid ArticleId { get; set; }
    public Guid EmployeeId { get; set; }
    public DateTime ReadAt { get; set; } = DateTime.UtcNow;

    public NewsArticle? Article { get; set; }

    public NewsReadReceipt() { }

    public NewsReadReceipt(Guid articleId, Guid employeeId)
    {
        ArticleId = articleId;
        EmployeeId = employeeId;
        ReadAt = DateTime.UtcNow;
    }
}
