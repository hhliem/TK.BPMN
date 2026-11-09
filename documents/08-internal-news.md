# Module 08: InternalNews (Bản tin & Truyền thông Nội bộ Doanh nghiệp)

## 1. Tổng quan & Phạm vi Bounded Context
Module `InternalNews` phục vụ truyền thông chính sách, quy chế và tin tức hoạt động nội bộ:
1. Cho phép Ban Lãnh đạo, HR xuất bản thông báo và bản tin công ty.
2. Tích hợp phân loại tin (Tin khẩn, Chính sách mới, Hoạt động đoàn thể).
3. Khi xuất bản bản tin quan trọng, tự động phát sinh MediatR Event để Module Notification gửi thông báo tức thời tới toàn thể nhân sự.
4. Ghi nhận nhật ký đọc tin (Read Acknowledgement) đối với các thông cáo bắt buộc.
5. Hỗ trợ lên lịch xuất bản tự động (Scheduled Publishing) và tự động lưu trữ (Auto-Archive) bài viết hết hạn.

### Ranh giới kiến trúc
- **Database Schema riêng biệt:** `[news]` trên SQL Server.
- **Giao tiếp:** Bắn `NewsPublishedEvent` ra hệ thống khi bài viết chuyển sang trạng thái `Published`. Bắn `NewsArchivedEvent` khi bài viết được lưu trữ.

---

## 2. Thiết kế Cơ sở dữ liệu (SQL Server - Schema [news])

```sql
CREATE SCHEMA [news];
GO

-- 1. Bảng Bài viết / Bản tin Nội bộ
CREATE TABLE [news].[NewsArticles] (
    [Id] UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    [Title] NVARCHAR(255) NOT NULL,
    [Summary] NVARCHAR(500) NOT NULL,
    [ContentHtml] NVARCHAR(MAX) NOT NULL,
    [CoverImageUrl] NVARCHAR(500) NULL,
    [Category] NVARCHAR(50) NOT NULL DEFAULT 'General', -- General, Policy, Urgent
    [AuthorId] UNIQUEIDENTIFIER NOT NULL,
    [TargetOrgUnitId] UNIQUEIDENTIFIER NULL, -- NULL: Toàn công ty; Hoặc giới hạn phòng ban
    [Status] NVARCHAR(50) NOT NULL DEFAULT 'Draft', -- Draft, Published, Archived
    [IsMandatoryRead] BIT NOT NULL DEFAULT 0, -- Yêu cầu nhân viên phải xác nhận đã đọc
    [PublishedAt] DATETIME2 NULL,
    [ScheduledPublishAt] DATETIME2 NULL,
    [ExpiresAt] DATETIME2 NULL,
    [CreatedAt] DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    [CreatedBy] UNIQUEIDENTIFIER NULL,
    [UpdatedAt] DATETIME2 NULL,
    [UpdatedBy] UNIQUEIDENTIFIER NULL,
    [ArchivedAt] DATETIME2 NULL
);
CREATE INDEX [IX_News_PublishedLookup] 
ON [news].[NewsArticles]([Status], [PublishedAt] DESC);
GO

-- 2. Bảng Xác nhận Đã đọc Tin (Read Receipts)
CREATE TABLE [news].[NewsReadReceipts] (
    [ArticleId] UNIQUEIDENTIFIER NOT NULL,
    [EmployeeId] UNIQUEIDENTIFIER NOT NULL,
    [ReadAt] DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    PRIMARY KEY ([ArticleId], [EmployeeId]),
    CONSTRAINT [FK_NewsReadReceipts_Article] FOREIGN KEY ([ArticleId]) REFERENCES [news].[NewsArticles]([Id]) ON DELETE CASCADE
);
GO
```

---

## 3. Tích hợp Xuất bản & Phát tán Sự kiện (MediatR Integration)

```csharp
public class PublishNewsCommandHandler : IRequestHandler<PublishNewsCommand, Unit>
{
    private readonly INewsRepository _newsRepo;
    private readonly IMediator _mediator;

    public PublishNewsCommandHandler(INewsRepository newsRepo, IMediator mediator)
    {
        _newsRepo = newsRepo;
        _mediator = mediator;
    }

    public async Task<Unit> Handle(PublishNewsCommand request, CancellationToken cancellationToken)
    {
        var article = await _newsRepo.GetByIdAsync(request.ArticleId, cancellationToken);
        if (article == null) throw new NotFoundException("Không tìm thấy bài viết");

        article.Status = "Published";
        article.PublishedAt = DateTime.UtcNow;

        await _newsRepo.SaveChangesAsync(cancellationToken);

        // Bắn Integration Event để Notification Module bắt lấy và gửi Email/Zalo/Teams
        await _mediator.Publish(new NewsPublishedEvent(
            ArticleId: article.Id,
            Title: article.Title,
            Summary: article.Summary,
            Category: article.Category,
            IsUrgent: article.Category == "Urgent"
        ), cancellationToken);

        return Unit.Value;
    }
}
```

---

## 4. Lưu trữ Bài viết & Sự kiện NewsArchivedEvent

### 4.1. Integration Event

```csharp
public record NewsArchivedEvent(
    Guid ArticleId,
    string Title,
    string Category,
    DateTime ArchivedAt
) : BaseIntegrationEvent;
```

### 4.2. ArchiveNewsCommandHandler

```csharp
public class ArchiveNewsCommandHandler : IRequestHandler<ArchiveNewsCommand, Unit>
{
    private readonly INewsRepository _newsRepo;
    private readonly IMediator _mediator;

    public async Task<Unit> Handle(ArchiveNewsCommand request, CancellationToken cancellationToken)
    {
        var article = await _newsRepo.GetByIdAsync(request.ArticleId, cancellationToken);
        if (article == null) throw new NotFoundException("Không tìm thấy bài viết");

        article.Status = "Archived";
        article.ArchivedAt = DateTime.UtcNow;
        await _newsRepo.SaveChangesAsync(cancellationToken);

        // Phát event để AiCopilot cập nhật RAG knowledge base
        await _mediator.Publish(new NewsArchivedEvent(
            ArticleId: article.Id,
            Title: article.Title,
            Category: article.Category,
            ArchivedAt: article.ArchivedAt.Value
        ), cancellationToken);

        return Unit.Value;
    }
}
```

---

## 5. Scheduled Publishing & Auto-Archive Background Worker

```csharp
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
        while (!stoppingToken.IsCancellationRequested)
        {
            using var scope = _serviceProvider.CreateScope();
            var newsRepo = scope.ServiceProvider.GetRequiredService<INewsRepository>();
            var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

            var scheduledArticles = await newsRepo.GetScheduledForPublishAsync(DateTime.UtcNow, stoppingToken);
            foreach (var article in scheduledArticles)
            {
                article.Status = "Published";
                article.PublishedAt = DateTime.UtcNow;
                await newsRepo.SaveChangesAsync(stoppingToken);

                await mediator.Publish(new NewsPublishedEvent(
                    ArticleId: article.Id,
                    Title: article.Title,
                    Summary: article.Summary,
                    Category: article.Category,
                    IsUrgent: article.Category == "Urgent"
                ), stoppingToken);
            }

            // Auto-archive expired articles
            var expiredArticles = await newsRepo.GetExpiredArticlesAsync(DateTime.UtcNow, stoppingToken);
            foreach (var article in expiredArticles)
            {
                article.Status = "Archived";
                article.ArchivedAt = DateTime.UtcNow;
                await newsRepo.SaveChangesAsync(stoppingToken);

                await mediator.Publish(new NewsArchivedEvent(
                    ArticleId: article.Id,
                    Title: article.Title,
                    Category: article.Category,
                    ArchivedAt: article.ArchivedAt.Value
                ), stoppingToken);
            }

            await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
        }
    }
}
```

---

## 6. Điểm kiểm soát trọng yếu (Critical Control Points)

1. **Kiểm soát Bảo mật Nội dung HTML (XSS Protection):**
   - **Rủi ro:** Biên tập viên sao chép nội dung từ website bên ngoài dán vào trình soạn thảo Rich Text (CKEditor/TinyMCE) vô tình mang theo mã độc JavaScript (`<script>` tags).
   - **Kiểm soát:** Tầng Application bắt buộc dùng thư viện `Ganss.Xss.HtmlSanitizer` để làm sạch toàn bộ thẻ HTML trước khi cập nhật cột `ContentHtml` vào cơ sở dữ liệu.
2. **Quyền Hạn Xuất bản Phân tầng (Publishing Authorization):**
   - **Rủi ro:** Nhân viên cấp dưới tự ý đăng tin lên bảng tin chung toàn tập đoàn.
   - **Kiểm soát:** Áp dụng Policy `[Authorize(Policy = "CanPublishCompanyNews")]`. Chỉ những tài khoản thuộc Ban Truyền thông Nội bộ, Giám đốc Nhân sự (HRD) hoặc Ban Giám đốc mới có quyền đổi trạng thái bài viết thành `Published`.
3. **Scheduled Publishing Idempotency:**
   - **Rủi ro:** Background Worker chạy nhiều instance hoặc bị restart giữa chừng, dẫn đến cùng một bài viết bị publish 2 lần, gửi trùng thông báo cho toàn bộ nhân sự.
   - **Kiểm soát:** Kiểm tra trạng thái trước khi publish để tránh publish 2 lần cùng bài viết. Query `GetScheduledForPublishAsync` phải filter `WHERE Status = 'Draft' AND ScheduledPublishAt <= @now` để đảm bảo chỉ xử lý bài viết chưa được publish. Sử dụng Optimistic Concurrency (`RowVersion`) để phát hiện xung đột khi nhiều instance cùng cập nhật.
4. **Read Receipt Compliance:**
   - **Rủi ro:** Bài viết `IsMandatoryRead = true` nhưng nhiều nhân viên không đọc hoặc không xác nhận, dẫn đến vi phạm tuân thủ chính sách nội bộ mà không ai biết.
   - **Kiểm soát:** Đối với bài viết `IsMandatoryRead = true`, hệ thống phải có chức năng nhắc nhở tự động sau 3 ngày nếu nhân viên chưa xác nhận đọc. Background Worker kiểm tra `NewsReadReceipts` và bắn `ReminderNotificationEvent` cho các nhân viên thiếu xác nhận. Báo cáo tỷ lệ đọc được gửi cho HR và Ban Lãnh đạo hàng tuần.
