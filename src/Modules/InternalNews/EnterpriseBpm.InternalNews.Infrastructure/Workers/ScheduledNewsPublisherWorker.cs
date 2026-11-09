using EnterpriseBpm.InternalNews.Domain.Enums;
using EnterpriseBpm.InternalNews.Domain.Events;
using EnterpriseBpm.InternalNews.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace EnterpriseBpm.InternalNews.Infrastructure.Workers;

public class ScheduledNewsPublisherWorker : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<ScheduledNewsPublisherWorker> _logger;

    public ScheduledNewsPublisherWorker(
        IServiceProvider serviceProvider,
        ILogger<ScheduledNewsPublisherWorker> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("[Scheduled News Publisher Worker] Tiến trình quét bài viết hẹn giờ & tự động lưu trữ đã bắt đầu.");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessScheduledAndExpiredArticlesAsync(stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[Scheduled News Publisher Worker] Lỗi trong chu kỳ quét bài viết.");
            }

            // Quét định kỳ mỗi 1 phút (thay vì 5 phút để đáp ứng nhanh hơn trong môi trường phát triển & kiểm thử)
            await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
        }
    }

    public async Task ProcessScheduledAndExpiredArticlesAsync(CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<NewsDbContext>();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
        var now = DateTime.UtcNow;

        // 1. Tự động xuất bản các bài viết Draft đến giờ hẹn
        var scheduledArticles = await dbContext.NewsArticles
            .Where(a => a.Status == NewsStatus.Draft && a.ScheduledPublishAt.HasValue && a.ScheduledPublishAt.Value <= now)
            .ToListAsync(cancellationToken);

        foreach (var article in scheduledArticles)
        {
            article.Publish();
            _logger.LogInformation("[Scheduled News Publisher] Tự động xuất bản bài viết '{Title}' (ID: {ArticleId})", article.Title, article.Id);

            await mediator.Publish(new NewsPublishedEvent(
                ArticleId: article.Id,
                Title: article.Title,
                Summary: article.Summary,
                Category: article.Category.ToString(),
                IsUrgent: article.Category == NewsCategory.Urgent
            ), cancellationToken);
        }

        // 2. Tự động lưu trữ các bài viết Published đã hết hạn
        var expiredArticles = await dbContext.NewsArticles
            .Where(a => a.Status == NewsStatus.Published && a.ExpiresAt.HasValue && a.ExpiresAt.Value <= now)
            .ToListAsync(cancellationToken);

        foreach (var article in expiredArticles)
        {
            article.Archive();
            _logger.LogInformation("[Scheduled News Publisher] Tự động lưu trữ bài viết hết hạn '{Title}' (ID: {ArticleId})", article.Title, article.Id);

            await mediator.Publish(new NewsArchivedEvent(
                ArticleId: article.Id,
                Title: article.Title,
                Category: article.Category.ToString(),
                ArchivedAt: article.ArchivedAt ?? now
            ), cancellationToken);
        }

        if (scheduledArticles.Count > 0 || expiredArticles.Count > 0)
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
    }
}

