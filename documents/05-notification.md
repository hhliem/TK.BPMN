# Module 05: Notification (Cổng Thông báo Tập trung Đa nền tảng)

## 1. Tổng quan & Phạm vi Bounded Context
Module `Notification` đóng vai trò là Notification Gateway trung tâm cho toàn bộ tập đoàn. Chịu trách nhiệm:
1. Tiếp nhận yêu cầu gửi thông báo từ tất cả các module khác thông qua MediatR Integration Events.
2. Quản lý hệ thống Template đa ngôn ngữ bằng Template Engine (Fluid/Scriban).
3. Phát tin đi các kênh: Email O365 (qua Microsoft Graph Mail API), Microsoft Teams Webhook, Zalo ZNS, SMS Gateway.
4. Cơ chế hàng đợi ngầm (In-memory Channels hoặc RabbitMQ) với chính sách tự động thử lại (Retry) khi gặp sự cố mạng.

### Ranh giới kiến trúc
- **Database Schema riêng biệt:** `[notification]` trên SQL Server.
- **Cơ chế:** Event-Driven thuần túy (lắng nghe các sự kiện `WorkflowApprovedEvent`, `WorkflowRejectedEvent`, `WorkflowCancelledEvent`, `TaskAssignedEvent`, `NewsPublishedEvent`, `EmployeeDeactivatedEvent`).

---

## 2. Thiết kế Cơ sở dữ liệu (SQL Server - Schema [notification])

```sql
CREATE SCHEMA [notification];
GO

-- 1. Bảng Mẫu Thông báo Động
CREATE TABLE [notification].[NotificationTemplates] (
    [Id] UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    [TemplateCode] NVARCHAR(100) NOT NULL UNIQUE, -- "TPL_TASK_ASSIGNED", "TPL_WORKFLOW_APPROVED"
    [Channel] NVARCHAR(50) NOT NULL, -- Email, Teams, Zalo, Sms
    [SubjectTemplate] NVARCHAR(255) NULL,
    [BodyTemplate] NVARCHAR(MAX) NOT NULL, -- Chứa cú pháp Fluid: {{ RequesterName }}, {{ FormTitle }}
    [IsActive] BIT NOT NULL DEFAULT 1,
    [UpdatedAt] DATETIME2 NOT NULL DEFAULT GETUTCDATE()
);
GO

-- 2. Bảng Nhật ký & Trạng thái Gửi tin
CREATE TABLE [notification].[NotificationLogs] (
    [Id] UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    [TemplateCode] NVARCHAR(100) NOT NULL,
    [RecipientEmail] NVARCHAR(255) NULL,
    [RecipientPhone] NVARCHAR(50) NULL,
    [Channel] NVARCHAR(50) NOT NULL,
    [Status] NVARCHAR(50) NOT NULL DEFAULT 'Pending', -- Pending, Sent, Failed
    [RetryCount] INT NOT NULL DEFAULT 0,
    [LastError] NVARCHAR(MAX) NULL,
    [SourceEventId] UNIQUEIDENTIFIER NULL, -- ID sự kiện gốc để chống trùng lặp
    [SentAt] DATETIME2 NULL,
    [CreatedAt] DATETIME2 NOT NULL DEFAULT GETUTCDATE()
);
CREATE INDEX [IX_NotificationLogs_Retry] 
ON [notification].[NotificationLogs]([Status], [RetryCount]);

-- Index chống trùng lặp thông báo (Deduplication)
CREATE UNIQUE INDEX [IX_NotificationLogs_Dedup]
ON [notification].[NotificationLogs]([SourceEventId], [TemplateCode], [RecipientEmail])
WHERE [SourceEventId] IS NOT NULL;
GO
```

### Seed Data - Notification Templates

```sql
-- Template thông báo cơ bản
INSERT INTO [notification].[NotificationTemplates] (TemplateCode, Channel, SubjectTemplate, BodyTemplate, IsActive) VALUES
('TPL_WORKFLOW_APPROVED', 'Email', N'[Đã duyệt] Phiếu {{ FormKey }} - {{ WorkflowInstanceId }}', N'<p>Kính gửi,</p><p>Phiếu trình ký <strong>{{ FormKey }}</strong> đã được phê duyệt thành công.</p>', 1),
('TPL_WORKFLOW_REJECTED', 'Email', N'[Từ chối] Phiếu {{ FormKey }} - {{ WorkflowInstanceId }}', N'<p>Kính gửi,</p><p>Phiếu trình ký <strong>{{ FormKey }}</strong> đã bị từ chối bởi <strong>{{ RejectedByName }}</strong>.</p><p>Lý do: {{ RejectionReason }}</p>', 1),
('TPL_WORKFLOW_CANCELLED', 'Email', N'[Hủy] Phiếu {{ FormKey }} - {{ WorkflowInstanceId }}', N'<p>Phiếu trình ký <strong>{{ FormKey }}</strong> đã bị hủy bởi <strong>{{ CancelledByName }}</strong>.</p><p>Lý do: {{ CancellationReason }}</p>', 1),
('TPL_EMPLOYEE_DEACTIVATED', 'Email', N'[Thông báo] Nhân sự {{ EmployeeName }} ngừng hoạt động', N'<p>Nhân sự <strong>{{ EmployeeName }}</strong> thuộc {{ OrgUnitName }} đã ngừng hoạt động từ ngày {{ DeactivationDate }}.</p>', 1),
('TPL_TASK_SLA_BREACHED', 'Email', N'[KHẨN] Nhiệm vụ phê duyệt đã quá hạn SLA', N'<p>Nhiệm vụ phê duyệt cho phiếu <strong>{{ FormKey }}</strong> đã vượt quá thời hạn SLA quy định.</p><p>Vui lòng xử lý ngay.</p>', 1);
GO
```

---

## 3. Triển khai Xử lý Sự kiện & Gửi Email O365

### 3.1. MediatR Handler - Đơn được Duyệt (WorkflowApprovedEvent)
```csharp
public class WorkflowApprovedNotificationHandler : INotificationHandler<WorkflowApprovedEvent>
{
    private readonly INotificationDispatcher _dispatcher;

    public WorkflowApprovedNotificationHandler(INotificationDispatcher dispatcher)
    {
        _dispatcher = dispatcher;
    }

    public async Task Handle(WorkflowApprovedEvent notification, CancellationToken cancellationToken)
    {
        // Đẩy vào hàng đợi thông báo ngầm
        await _dispatcher.EnqueueNotificationAsync(new NotificationDispatchItem(
            TemplateCode: "TPL_WORKFLOW_APPROVED",
            Channel: "Email",
            RecipientUserId: notification.SubmittedByUserId,
            PayloadParameters: new Dictionary<string, object>
            {
                { "WorkflowInstanceId", notification.WorkflowInstanceId },
                { "FormKey", notification.FormKey },
                { "ApprovedDate", DateTime.UtcNow.ToString("dd/MM/yyyy HH:mm") }
            }
        ), cancellationToken);
    }
}
```

### 3.2. MediatR Handler - Phiếu bị Từ chối (WorkflowRejectedEvent)
```csharp
public class WorkflowRejectedNotificationHandler : INotificationHandler<WorkflowRejectedEvent>
{
    private readonly INotificationDispatcher _dispatcher;

    public async Task Handle(WorkflowRejectedEvent notification, CancellationToken cancellationToken)
    {
        await _dispatcher.EnqueueNotificationAsync(new NotificationDispatchItem(
            TemplateCode: "TPL_WORKFLOW_REJECTED",
            Channel: "Email",
            RecipientUserId: notification.SubmittedByUserId,
            PayloadParameters: new Dictionary<string, object>
            {
                { "WorkflowInstanceId", notification.WorkflowInstanceId },
                { "FormKey", notification.FormKey },
                { "RejectedByName", notification.RejectedByName },
                { "RejectionReason", notification.Comment ?? "Không có lý do" },
                { "RejectedDate", DateTime.UtcNow.ToString("dd/MM/yyyy HH:mm") }
            }
        ), cancellationToken);
    }
}
```

### 3.3. MediatR Handler - Phiếu bị Hủy (WorkflowCancelledEvent)
```csharp
public class WorkflowCancelledNotificationHandler : INotificationHandler<WorkflowCancelledEvent>
{
    private readonly INotificationDispatcher _dispatcher;

    public async Task Handle(WorkflowCancelledEvent notification, CancellationToken cancellationToken)
    {
        // Thông báo cho tất cả người liên quan (người nộp + người đang chờ duyệt)
        foreach (var userId in notification.AffectedUserIds)
        {
            await _dispatcher.EnqueueNotificationAsync(new NotificationDispatchItem(
                TemplateCode: "TPL_WORKFLOW_CANCELLED",
                Channel: "Email",
                RecipientUserId: userId,
                PayloadParameters: new Dictionary<string, object>
                {
                    { "WorkflowInstanceId", notification.WorkflowInstanceId },
                    { "FormKey", notification.FormKey },
                    { "CancelledByName", notification.CancelledByName },
                    { "CancellationReason", notification.Reason }
                }
            ), cancellationToken);
        }
    }
}
```

### 3.4. MediatR Handler - Nhân sự Nghỉ việc (EmployeeDeactivatedEvent)
```csharp
public class EmployeeDeactivatedNotificationHandler : INotificationHandler<EmployeeDeactivatedEvent>
{
    private readonly INotificationDispatcher _dispatcher;

    public async Task Handle(EmployeeDeactivatedEvent notification, CancellationToken cancellationToken)
    {
        // Thông báo cho quản lý trực tiếp
        await _dispatcher.EnqueueNotificationAsync(new NotificationDispatchItem(
            TemplateCode: "TPL_EMPLOYEE_DEACTIVATED",
            Channel: "Email",
            RecipientUserId: notification.DirectManagerId,
            PayloadParameters: new Dictionary<string, object>
            {
                { "EmployeeName", notification.EmployeeName },
                { "DeactivationDate", notification.DeactivationDate.ToString("dd/MM/yyyy") },
                { "OrgUnitName", notification.OrgUnitName }
            }
        ), cancellationToken);
    }
}
```

### 3.5. Triển khai Gửi Email qua Microsoft Graph API
```csharp
public class O365GraphMailProvider : IMailProvider
{
    private readonly GraphServiceClient _graphClient;

    public O365GraphMailProvider(GraphServiceClient graphClient)
    {
        _graphClient = graphClient;
    }

    public async Task SendEmailAsync(string recipientEmail, string subject, string htmlBody, CancellationToken cancellationToken)
    {
        var message = new Message
        {
            Subject = subject,
            Body = new ItemBody
            {
                ContentType = BodyType.Html,
                Content = htmlBody
            },
            ToRecipients = new List<Recipient>
            {
                new() { EmailAddress = new EmailAddress { Address = recipientEmail } }
            }
        };

        // Gửi qua Mailbox chung của Doanh nghiệp (Enterprise Service Mailbox)
        await _graphClient.Users["workflow-notification@enterprise.com"]
            .SendMail(message, SaveToSentItems: false)
            .PostAsync(cancellationToken: cancellationToken);
    }
}
```

---

## 4. Điểm kiểm soát trọng yếu (Critical Control Points)

1. **Cơ chế Thử lại Tự động (Exponential Backoff Resilience):**
   - **Rủi ro:** Khi Graph API hoặc Gateway ZNS/SMS bảo trì, thông báo bị rớt làm người duyệt không biết có đơn đang chờ.
   - **Kiểm soát:** Bộ xử lý Background Queue phải cấu hình retry ngầm 3 lần theo khoảng thời gian lũy tiến ($1\text{ phút}$, $5\text{ phút}$, $15\text{ phút}$). Nếu vẫn thất bại, đổi trạng thái log thành `Failed` và phát cảnh báo lên Dashboard giám sát.
2. **Khử nhiễm Dữ liệu Template (HTML Sanitization):**
   - **Rủi ro:** Nội dung người dùng nhập trên Form trình ký (ví dụ: trường Ý kiến) có thể chứa mã HTML độc hại, khi render vào Template Email sẽ phá vỡ giao diện hoặc tấn công người đọc.
   - **Kiểm soát:** Mọi giá trị biến động nhúng vào Fluid/Scriban template phải chạy qua bộ mã hóa `HtmlEncoder.Default.Encode()` trước khi render.
3. **Chống Tràn Hàng Đợi (Queue Backpressure):**
   - **Rủi ro:** Hàng loạt thông báo gửi đồng thời (ví dụ: công bố tin tức toàn tập đoàn) làm quá tải tài nguyên server.
   - **Kiểm soát:** Sử dụng `System.Threading.Channels.Channel` có giới hạn dung lượng bộ đệm (Bounded Capacity), kích hoạt chiến lược `Wait` khi queue đầy để đảm bảo an toàn bộ nhớ.
4. **Notification Deduplication (Chống Trùng lặp Thông báo):**
   - **Rủi ro:** Cùng 1 event bị publish 2 lần do retry logic hoặc lỗi at-least-once delivery → người dùng nhận 2 email giống hệt nhau, gây nhầm lẫn và mất uy tín hệ thống.
   - **Kiểm soát:** Lưu `SourceEventId` (ID của Integration Event gốc) vào bảng `NotificationLogs` và kiểm tra trùng lặp qua unique index `[IX_NotificationLogs_Dedup]` trước khi enqueue. Nếu phát hiện đã tồn tại bản ghi với cùng `SourceEventId + TemplateCode + RecipientEmail`, bỏ qua và ghi log cảnh báo.
