using System.Text.RegularExpressions;
using EnterpriseBpm.InternalNews.Application.Contracts;
using EnterpriseBpm.InternalNews.Domain.Entities;
using EnterpriseBpm.InternalNews.Domain.Enums;
using EnterpriseBpm.InternalNews.Domain.Events;
using EnterpriseBpm.InternalNews.Domain.Exceptions;
using EnterpriseBpm.InternalNews.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace EnterpriseBpm.InternalNews.Infrastructure.Services;

public class NewsService : INewsService
{
    private readonly NewsDbContext _dbContext;
    private readonly IMediator _mediator;
    private readonly ILogger<NewsService> _logger;

    public NewsService(
        NewsDbContext dbContext,
        IMediator mediator,
        ILogger<NewsService> logger)
    {
        _dbContext = dbContext;
        _mediator = mediator;
        _logger = logger;
    }

    public async Task<IReadOnlyList<NewsArticleSummaryDto>> GetArticlesAsync(
        string? category = null,
        string? status = null,
        int pageNumber = 1,
        int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var query = _dbContext.NewsArticles.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(status) && Enum.TryParse<NewsStatus>(status, true, out var st))
        {
            query = query.Where(a => a.Status == st);
        }
        else
        {
            query = query.Where(a => a.Status == NewsStatus.Published);
        }

        if (!string.IsNullOrWhiteSpace(category) && Enum.TryParse<NewsCategory>(category, true, out var cat))
        {
            query = query.Where(a => a.Category == cat);
        }

        var articles = await query
            .OrderByDescending(a => a.PublishedAt ?? a.CreatedAt)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .Select(a => new NewsArticleSummaryDto(
                a.Id,
                a.Title,
                a.Summary,
                a.CoverImageUrl,
                a.Category.ToString(),
                a.Status.ToString(),
                a.IsMandatoryRead,
                a.PublishedAt,
                a.ScheduledPublishAt,
                a.CreatedAt,
                a.ReadReceipts.Count
            ))
            .ToListAsync(cancellationToken);

        return articles;
    }

    public async Task<NewsArticleDetailDto> GetArticleByIdAsync(Guid articleId, Guid? currentUserId = null, CancellationToken cancellationToken = default)
    {
        var article = await _dbContext.NewsArticles
            .Include(a => a.ReadReceipts)
            .AsNoTracking()
            .FirstOrDefaultAsync(a => a.Id == articleId, cancellationToken)
            ?? throw new NewsArticleNotFoundException(articleId);

        var hasRead = currentUserId.HasValue && article.ReadReceipts.Any(r => r.EmployeeId == currentUserId.Value);
        var readAt = currentUserId.HasValue ? article.ReadReceipts.FirstOrDefault(r => r.EmployeeId == currentUserId.Value)?.ReadAt : null;

        return new NewsArticleDetailDto(
            article.Id,
            article.Title,
            article.Summary,
            article.ContentHtml,
            article.CoverImageUrl,
            article.Category.ToString(),
            article.Status.ToString(),
            article.IsMandatoryRead,
            hasRead,
            readAt,
            article.PublishedAt,
            article.ScheduledPublishAt,
            article.ExpiresAt,
            article.CreatedAt,
            article.AuthorId,
            article.ReadReceipts.Count
        );
    }

    public async Task<NewsArticleDetailDto> CreateArticleAsync(CreateNewsArticleDto dto, Guid authorId, CancellationToken cancellationToken = default)
    {
        var category = Enum.TryParse<NewsCategory>(dto.Category, true, out var cat) ? cat : NewsCategory.General;
        var sanitizedHtml = SanitizeHtml(dto.ContentHtml);

        var article = new NewsArticle(
            title: dto.Title,
            summary: dto.Summary,
            contentHtml: sanitizedHtml,
            category: category,
            authorId: authorId,
            isMandatoryRead: dto.IsMandatoryRead,
            coverImageUrl: dto.CoverImageUrl,
            scheduledPublishAt: dto.ScheduledPublishAt,
            expiresAt: dto.ExpiresAt,
            targetOrgUnitId: dto.TargetOrgUnitId
        );

        _dbContext.NewsArticles.Add(article);
        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Tạo bài viết tin tức mới '{Title}' (ID: {ArticleId})", article.Title, article.Id);

        return await GetArticleByIdAsync(article.Id, authorId, cancellationToken);
    }

    public async Task<NewsArticleDetailDto> UpdateArticleAsync(Guid articleId, UpdateNewsArticleDto dto, Guid updaterId, CancellationToken cancellationToken = default)
    {
        var article = await _dbContext.NewsArticles.FirstOrDefaultAsync(a => a.Id == articleId, cancellationToken)
            ?? throw new NewsArticleNotFoundException(articleId);

        article.Title = dto.Title;
        article.Summary = dto.Summary;
        article.ContentHtml = SanitizeHtml(dto.ContentHtml);
        if (Enum.TryParse<NewsCategory>(dto.Category, true, out var cat))
        {
            article.Category = cat;
        }
        article.IsMandatoryRead = dto.IsMandatoryRead;
        article.CoverImageUrl = dto.CoverImageUrl;
        article.ScheduledPublishAt = dto.ScheduledPublishAt;
        article.ExpiresAt = dto.ExpiresAt;
        article.TargetOrgUnitId = dto.TargetOrgUnitId;
        article.UpdatedAt = DateTime.UtcNow;
        article.UpdatedBy = updaterId;

        await _dbContext.SaveChangesAsync(cancellationToken);
        return await GetArticleByIdAsync(article.Id, updaterId, cancellationToken);
    }

    public async Task PublishArticleAsync(Guid articleId, Guid publisherId, CancellationToken cancellationToken = default)
    {
        var article = await _dbContext.NewsArticles.FirstOrDefaultAsync(a => a.Id == articleId, cancellationToken)
            ?? throw new NewsArticleNotFoundException(articleId);

        article.Publish(publisherId);
        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Xuất bản bài viết '{Title}' thành công. Phát sinh NewsPublishedEvent.", article.Title);

        await _mediator.Publish(new NewsPublishedEvent(
            ArticleId: article.Id,
            Title: article.Title,
            Summary: article.Summary,
            Category: article.Category.ToString(),
            IsUrgent: article.Category == NewsCategory.Urgent
        ), cancellationToken);
    }

    public async Task ArchiveArticleAsync(Guid articleId, Guid archiverId, CancellationToken cancellationToken = default)
    {
        var article = await _dbContext.NewsArticles.FirstOrDefaultAsync(a => a.Id == articleId, cancellationToken)
            ?? throw new NewsArticleNotFoundException(articleId);

        article.Archive(archiverId);
        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Lưu trữ bài viết '{Title}' thành công. Phát sinh NewsArchivedEvent.", article.Title);

        await _mediator.Publish(new NewsArchivedEvent(
            ArticleId: article.Id,
            Title: article.Title,
            Category: article.Category.ToString(),
            ArchivedAt: article.ArchivedAt ?? DateTime.UtcNow
        ), cancellationToken);
    }

    public async Task AcknowledgeReadAsync(Guid articleId, Guid employeeId, CancellationToken cancellationToken = default)
    {
        var article = await _dbContext.NewsArticles.FirstOrDefaultAsync(a => a.Id == articleId, cancellationToken)
            ?? throw new NewsArticleNotFoundException(articleId);

        var existing = await _dbContext.NewsReadReceipts
            .FirstOrDefaultAsync(r => r.ArticleId == articleId && r.EmployeeId == employeeId, cancellationToken);

        if (existing == null)
        {
            _dbContext.NewsReadReceipts.Add(new NewsReadReceipt(articleId, employeeId));
            await _dbContext.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("Nhân viên {EmployeeId} đã xác nhận đọc bài viết '{Title}'", employeeId, article.Title);
        }
    }

    public async Task<NewsComplianceStatsDto> GetComplianceStatsAsync(Guid articleId, CancellationToken cancellationToken = default)
    {
        var article = await _dbContext.NewsArticles
            .Include(a => a.ReadReceipts)
            .AsNoTracking()
            .FirstOrDefaultAsync(a => a.Id == articleId, cancellationToken)
            ?? throw new NewsArticleNotFoundException(articleId);

        var totalAck = article.ReadReceipts.Count;
        var totalTarget = 100; // Giả lập quy mô phòng ban / công ty chuẩn
        var rate = totalTarget > 0 ? Math.Round((double)totalAck / totalTarget * 100, 2) : 100.0;

        return new NewsComplianceStatsDto(
            article.Id,
            article.Title,
            article.IsMandatoryRead,
            totalAck,
            totalTarget,
            rate
        );
    }

    private static string SanitizeHtml(string html)
    {
        if (string.IsNullOrWhiteSpace(html)) return string.Empty;
        // Loại bỏ triệt để các thẻ <script>, on*= event handlers độc hại
        var sanitized = Regex.Replace(html, @"<script[\s\S]*?>[\s\S]*?<\/script>", string.Empty, RegexOptions.IgnoreCase);
        sanitized = Regex.Replace(sanitized, @"\bon\w+\s*=\s*(""[^""]*""|'[^']*'|[^\s>]+)", string.Empty, RegexOptions.IgnoreCase);
        sanitized = Regex.Replace(sanitized, @"javascript:", string.Empty, RegexOptions.IgnoreCase);
        return sanitized;
    }
}

