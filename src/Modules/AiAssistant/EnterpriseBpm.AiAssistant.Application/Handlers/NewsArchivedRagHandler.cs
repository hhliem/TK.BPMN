using EnterpriseBpm.AiAssistant.Application.Contracts;
using EnterpriseBpm.InternalNews.Domain.Events;
using MediatR;
using Microsoft.Extensions.Logging;

namespace EnterpriseBpm.AiAssistant.Application.Handlers;

/// <summary>
/// Lắng nghe sự kiện NewsArchivedEvent từ module InternalNews.
/// Tự động gỡ bỏ vectors tương ứng trong Qdrant để tránh AI trả về tin tức lỗi thời (TSK-615).
/// </summary>
public class NewsArchivedRagHandler : INotificationHandler<NewsArchivedEvent>
{
    private readonly IQdrantVectorService _qdrantService;
    private readonly ILogger<NewsArchivedRagHandler> _logger;

    public NewsArchivedRagHandler(
        IQdrantVectorService qdrantService,
        ILogger<NewsArchivedRagHandler> logger)
    {
        _qdrantService = qdrantService;
        _logger = logger;
    }

    public async Task Handle(NewsArchivedEvent notification, CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "NewsArchivedRagHandler: Bài tin {ArticleId} đã được lưu trữ. Bắt đầu gỡ bỏ vector khỏi Qdrant...",
            notification.ArticleId);

        try
        {
            await _qdrantService.DeletePointsByFilterAsync(
                collectionName: "company_policies",
                key: "sourceId",
                value: notification.ArticleId.ToString(),
                cancellationToken: cancellationToken);

            _logger.LogInformation("Đã hoàn tất gỡ bỏ vector cho bài tin lưu trữ {ArticleId}.", notification.ArticleId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi gỡ bỏ vector cho bài tin {ArticleId} khỏi Qdrant", notification.ArticleId);
        }
    }
}

