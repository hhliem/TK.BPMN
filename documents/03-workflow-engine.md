# Module 03: WorkflowEngine (Elsa Workflows v3 & Quản trị Trình ký)

## 1. Tổng quan & Phạm vi Bounded Context

Module `WorkflowEngine` là trái tim điều phối của toàn bộ hệ thống Enterprise BPMN Platform. Sử dụng nền tảng **Elsa Workflows v3** mã nguồn mở để:

1. **Định nghĩa & Thực thi Quy trình:** Xây dựng và vận hành các luồng quy trình nghiệp vụ phức tạp dạng Activity-based (Tuần tự, Phân nhánh, Song song, Vòng lặp).
2. **Visual Designer Trực quan:** Nhúng Elsa Studio Designer trực tiếp trên giao diện quản trị doanh nghiệp.
3. **Quản lý Tác vụ Trình ký Doanh nghiệp:** Cung cấp Custom Activity chuyên biệt cho phê duyệt, tạo Bookmark tạm dừng và đánh thức (Resume) luồng quy trình khi có hành động duyệt.
4. **Lịch sử Phê duyệt Bất biến (Immutable Approval History):** Ghi nhận toàn bộ hành trình phê duyệt từng bước, hỗ trợ ký thay và kiểm toán chữ ký số.
5. **Quản lý Cam kết Chất lượng Dịch vụ (SLA):** Tự động phát hiện trễ hạn, cảnh báo vi phạm SLA và kích hoạt quy trình leo thang (Escalation).

### Ranh giới kiến trúc (Architectural Boundaries)

- **Database Schema riêng biệt:** `[workflow]` trên SQL Server (chứa các bảng Elsa v3 và bảng nghiệp vụ trình ký chuyên biệt).
- **Không phụ thuộc ngược:** Giao tiếp với Module Org qua Public Contract (`IDelegationResolver`, `IOrgHierarchyService`) và Document qua `IDocumentStorageService`.
- **Giao tiếp Sự kiện (Event-Driven):** Phát các Integration Events (`WorkflowApprovedEvent`, `WorkflowRejectedEvent`, `WorkflowCancelledEvent`, `WorkflowCompletedEvent`, `TaskAssignedEvent`, `TaskSlaBreachedEvent`) qua MediatR.
- **Tiêu chuẩn Tuân thủ:** Triển khai `IAuditable` và `ISoftDeletable` kế thừa từ `SharedKernel`.

---

## 2. Thiết kế Cơ sở dữ liệu (SQL Server - Schema [workflow])

```sql
CREATE SCHEMA [workflow];
GO

-- 1. Bảng Nghiệp vụ Theo dõi Vòng đời Phiếu Trình ký
CREATE TABLE [workflow].[WorkflowSubmissions] (
    [Id] UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    [WorkflowInstanceId] NVARCHAR(100) NOT NULL UNIQUE, -- Mapping với ID thực thi của Elsa Workflows v3
    [WorkflowDefinitionId] NVARCHAR(100) NOT NULL,
    [FormKey] NVARCHAR(100) NOT NULL,
    [SubmittedByUserId] UNIQUEIDENTIFIER NOT NULL,
    [RequesterOrgUnitId] UNIQUEIDENTIFIER NOT NULL,
    [FormDataJson] NVARCHAR(MAX) NOT NULL, -- Payload dữ liệu người dùng nộp từ Form.io
    [CurrentStatus] NVARCHAR(50) NOT NULL DEFAULT 'Draft', -- Draft, InProgress, Approved, Rejected, Cancelled
    [Priority] INT NOT NULL DEFAULT 1, -- 1: Normal, 2: High, 3: Urgent
    [SubmittedAt] DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    [CompletedAt] DATETIME2 NULL,
    
    -- Các trường Audit & Soft-Delete từ SharedKernel
    [CreatedAt] DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    [CreatedBy] UNIQUEIDENTIFIER NULL,
    [UpdatedAt] DATETIME2 NULL,
    [UpdatedBy] UNIQUEIDENTIFIER NULL,
    [DeletedAt] DATETIME2 NULL,
    [DeletedBy] UNIQUEIDENTIFIER NULL
);
CREATE INDEX [IX_WorkflowSubmissions_Status] 
ON [workflow].[WorkflowSubmissions]([CurrentStatus], [SubmittedByUserId]);
CREATE INDEX [IX_WorkflowSubmissions_OrgUnit] 
ON [workflow].[WorkflowSubmissions]([RequesterOrgUnitId], [SubmittedAt] DESC);
GO

-- 2. Bảng Nhiệm vụ Phê duyệt Đang xử lý (Active Approval Tasks)
CREATE TABLE [workflow].[ApprovalTasks] (
    [Id] UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    [WorkflowInstanceId] NVARCHAR(100) NOT NULL,
    [ActivityId] NVARCHAR(100) NOT NULL, -- Node ID trong Elsa Designer
    [TargetRole] NVARCHAR(100) NULL,
    [AssignedToUserId] UNIQUEIDENTIFIER NOT NULL,
    [DelegatedFromUserId] UNIQUEIDENTIFIER NULL, -- Ghi nhận nếu đây là trường hợp ký thay
    [Status] NVARCHAR(50) NOT NULL DEFAULT 'Pending', -- Pending, Completed, Cancelled, Reassigned
    [Decision] NVARCHAR(50) NULL, -- Approved, Rejected, RequestChanges, Forwarded
    [Comment] NVARCHAR(MAX) NULL,
    [DueDate] DATETIME2 NULL, -- Thời hạn SLA xử lý
    [IsEscalated] BIT NOT NULL DEFAULT 0,
    [CompletedAt] DATETIME2 NULL,
    [CreatedAt] DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    CONSTRAINT [FK_ApprovalTasks_Submission] FOREIGN KEY ([WorkflowInstanceId]) 
        REFERENCES [workflow].[WorkflowSubmissions]([WorkflowInstanceId])
);
CREATE INDEX [IX_ApprovalTasks_UserPending] 
ON [workflow].[ApprovalTasks]([AssignedToUserId], [Status]) INCLUDE ([WorkflowInstanceId], [DueDate]);
CREATE INDEX [IX_ApprovalTasks_WorkflowInstance] 
ON [workflow].[ApprovalTasks]([WorkflowInstanceId], [Status]);
GO

-- 3. Bảng Lịch sử Phê duyệt Toàn trình (Immutable Approval History)
CREATE TABLE [workflow].[ApprovalHistory] (
    [Id] UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    [WorkflowInstanceId] NVARCHAR(100) NOT NULL,
    [StepOrder] INT NOT NULL, -- Thứ tự bước duyệt trong vòng đời phiếu
    [StepName] NVARCHAR(255) NOT NULL, -- Tên bước hiển thị (VD: "Trưởng phòng duyệt", "Kế toán trưởng ký")
    [ActorUserId] UNIQUEIDENTIFIER NOT NULL, -- Người trực tiếp thực hiện hành động
    [OnBehalfOfUserId] UNIQUEIDENTIFIER NULL, -- Người ủy quyền (nếu là trường hợp ký thay)
    [DelegationId] UNIQUEIDENTIFIER NULL, -- Mã ủy quyền tham chiếu sang [org].[WorkflowDelegations]
    [Decision] NVARCHAR(50) NOT NULL, -- Approved, Rejected, RequestChanges, Forwarded, Cancelled
    [Comment] NVARCHAR(MAX) NULL,
    [ActionTimestamp] DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    [DigitalSignatureHash] NVARCHAR(256) NULL -- SHA-256 Hash xác thực tính toàn vẹn của quyết định
);
CREATE INDEX [IX_ApprovalHistory_Workflow] 
ON [workflow].[ApprovalHistory]([WorkflowInstanceId], [StepOrder]);
CREATE INDEX [IX_ApprovalHistory_Actor] 
ON [workflow].[ApprovalHistory]([ActorUserId], [ActionTimestamp] DESC);
GO
```

---

## 3. Custom Activity Phê duyệt Trình ký (C# Elsa v3)

```csharp
namespace EnterpriseBpm.Workflow.Activities;

[Activity(
    Namespace = "EnterpriseBpm.Workflow",
    Category = "Trình Ký Doanh Nghiệp",
    DisplayName = "Tác Vụ Phê Duyệt",
    Description = "Tạo nhiệm vụ phê duyệt cho nhân sự/vai trò, tạm dừng chờ tín hiệu và ghi nhận lịch sử bất biến",
    Kind = ActivityKind.Task)]
public class ApproveFormActivity : Activity<string>
{
    [Input(Description = "Mã FormKey của phiếu trình ký")]
    public Input<string> FormKey { get; set; } = default!;

    [Input(Description = "Tên hiển thị của bước duyệt (VD: Trưởng phòng xem xét)")]
    public Input<string> StepName { get; set; } = default!;

    [Input(Description = "Thứ tự bước duyệt trong quy trình")]
    public Input<int> StepOrder { get; set; } = 1;

    [Input(Description = "Vai trò xử lý (VD: LineManager, CFO, CEO)")]
    public Input<string> TargetRole { get; set; } = default!;

    [Input(Description = "Thời hạn xử lý (Giờ) - Phục vụ SLA")]
    public Input<int> SlaHours { get; set; } = 24;

    [Input(Description = "Cho phép ủy quyền hay không")]
    public Input<bool> AllowDelegation { get; set; } = true;

    protected override async ValueTask ExecuteAsync(ActivityExecutionContext context)
    {
        var workflowInstanceId = context.WorkflowExecutionContext.Id;
        var formKey = FormKey.Get(context);
        var targetRole = TargetRole.Get(context);
        var slaHours = SlaHours.Get(context);
        var stepName = StepName.Get(context) ?? "Phê duyệt";
        var stepOrder = StepOrder.Get(context);

        var taskDispatcher = context.GetRequiredService<IApprovalTaskDispatcher>();

        // 1. Phân giải danh tính người duyệt (xét Cơ cấu tổ chức & Ủy quyền hợp lệ)
        var assignedTaskId = await taskDispatcher.DispatchApprovalTaskAsync(
            workflowInstanceId: workflowInstanceId,
            activityId: context.Activity.Id,
            stepName: stepName,
            stepOrder: stepOrder,
            formKey: formKey,
            targetRole: targetRole,
            slaHours: slaHours,
            allowDelegation: AllowDelegation.Get(context),
            cancellationToken: context.CancellationToken);

        // 2. Tạo Bookmark để Elsa dừng quy trình chờ tín hiệu Resume từ API
        context.CreateBookmark(
            bookmarkName: $"Approval_{context.Activity.Id}",
            callback: OnApprovalResumedAsync,
            autoResume: true);
    }

    private async ValueTask OnApprovalResumedAsync(ActivityExecutionContext context)
    {
        var payload = context.GetInput<ApprovalResultPayload>("ApprovalResultPayload");
        var historyService = context.GetRequiredService<IApprovalHistoryService>();
        var mediator = context.GetRequiredService<IMediator>();

        // 1. Ghi nhận lịch sử phê duyệt bất biến (Immutable Approval Record)
        await historyService.RecordHistoryAsync(new ApprovalHistoryRecord(
            WorkflowInstanceId: context.WorkflowExecutionContext.Id,
            StepOrder: StepOrder.Get(context),
            StepName: StepName.Get(context),
            ActorUserId: payload.ActorUserId,
            OnBehalfOfUserId: payload.OnBehalfOfUserId,
            DelegationId: payload.DelegationId,
            Decision: payload.Decision,
            Comment: payload.Comment,
            ActionTimestamp: DateTime.UtcNow,
            DigitalSignatureHash: payload.DigitalSignatureHash
        ), context.CancellationToken);

        // 2. Cung cấp kết quả cho Elsa Engine rẽ nhánh
        context.SetResult(payload.Decision);
        await context.CompleteActivityWithOutcomesAsync(payload.Decision);
    }
}
```

---

## 4. Tích hợp Quản lý SLA & Escalation Background Worker

```csharp
namespace EnterpriseBpm.Workflow.Workers;

public class SlaEscalationBackgroundWorker : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<SlaEscalationBackgroundWorker> _logger;

    public SlaEscalationBackgroundWorker(
        IServiceProvider serviceProvider, 
        ILogger<SlaEscalationBackgroundWorker> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var taskRepo = scope.ServiceProvider.GetRequiredService<IApprovalTaskRepository>();
                var submissionRepo = scope.ServiceProvider.GetRequiredService<IWorkflowSubmissionRepository>();
                var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

                // Quét các task quá hạn chưa được xử lý và chưa bị leo thang
                var overdueTasks = await taskRepo.GetOverduePendingTasksAsync(stoppingToken);
                foreach (var task in overdueTasks)
                {
                    task.IsEscalated = true;
                    await taskRepo.UpdateAsync(task, stoppingToken);

                    var submission = await submissionRepo.GetByInstanceIdAsync(task.WorkflowInstanceId, stoppingToken);

                    // Phát sự kiện để Module Notification gửi tin cảnh báo khẩn và Module Analytics cập nhật vi phạm SLA
                    await mediator.Publish(new TaskSlaBreachedEvent(
                        TaskId: task.Id,
                        WorkflowInstanceId: task.WorkflowInstanceId,
                        FormKey: submission?.FormKey ?? "UNKNOWN",
                        RequesterOrgUnitId: submission?.RequesterOrgUnitId ?? Guid.Empty,
                        AssignedToUserId: task.AssignedToUserId,
                        DueDate: task.DueDate!.Value
                    ), stoppingToken);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi xảy ra trong quá trình quét SLA trễ hạn.");
            }

            await Task.Delay(TimeSpan.FromMinutes(15), stoppingToken); // Chu kỳ quét 15 phút
        }
    }
}
```

---

## 5. Danh mục Sự kiện Tích hợp (Integration Events)

### 5.1. Outgoing Events (Module Workflow phát đi)

```csharp
namespace EnterpriseBpm.Workflow.Events;

// Phát khi một tác vụ duyệt mới được phân công
public record TaskAssignedEvent(
    Guid TaskId,
    string WorkflowInstanceId,
    string FormKey,
    Guid AssignedToUserId,
    DateTime DueDate
) : BaseIntegrationEvent;

// Phát khi nhiệm vụ bị quá hạn SLA
public record TaskSlaBreachedEvent(
    Guid TaskId,
    string WorkflowInstanceId,
    string FormKey,
    Guid RequesterOrgUnitId,
    Guid AssignedToUserId,
    DateTime DueDate
) : BaseIntegrationEvent;

// Phát khi toàn bộ quy trình được phê duyệt thành công
public record WorkflowApprovedEvent(
    string WorkflowInstanceId,
    string FormKey,
    Guid SubmittedByUserId,
    Guid ApprovedByUserId,
    DateTime ApprovedAt
) : BaseIntegrationEvent;

// Phát khi quy trình bị người duyệt từ chối
public record WorkflowRejectedEvent(
    string WorkflowInstanceId,
    string FormKey,
    Guid SubmittedByUserId,
    Guid RejectedByUserId,
    string RejectedByName,
    string? Comment,
    DateTime RejectedAt
) : BaseIntegrationEvent;

// Phát khi người nộp đơn hoặc quản trị viên hủy phiếu
public record WorkflowCancelledEvent(
    string WorkflowInstanceId,
    string FormKey,
    Guid CancelledByUserId,
    string CancelledByName,
    string Reason,
    IReadOnlyList<Guid> AffectedUserIds
) : BaseIntegrationEvent;

// Phát khi quy trình kết thúc (dù Approved, Rejected hay Cancelled) phục vụ Module Analytics
public record WorkflowCompletedEvent(
    string WorkflowInstanceId,
    string FormKey,
    Guid RequesterOrgUnitId,
    string FinalStatus, // Approved, Rejected, Cancelled
    DateTime SubmittedAt,
    DateTime CompletedAt
) : BaseIntegrationEvent;
```

### 5.2. Incoming Event Handlers (Module Workflow lắng nghe sự kiện ngoài)

```csharp
namespace EnterpriseBpm.Workflow.Handlers;

// Lắng nghe khi nhân sự bị vô hiệu hóa / nghỉ việc -> Thu hồi hoặc điều chuyển các Task còn Pending
public class EmployeeDeactivatedWorkflowHandler : INotificationHandler<EmployeeDeactivatedEvent>
{
    private readonly IApprovalTaskRepository _taskRepo;
    private readonly IOrgHierarchyService _orgService;
    private readonly ILogger<EmployeeDeactivatedWorkflowHandler> _logger;

    public EmployeeDeactivatedWorkflowHandler(
        IApprovalTaskRepository taskRepo, 
        IOrgHierarchyService orgService,
        ILogger<EmployeeDeactivatedWorkflowHandler> logger)
    {
        _taskRepo = taskRepo;
        _orgService = orgService;
        _logger = logger;
    }

    public async Task Handle(EmployeeDeactivatedEvent notification, CancellationToken cancellationToken)
    {
        var pendingTasks = await _taskRepo.GetPendingTasksByUserAsync(notification.EmployeeId, cancellationToken);
        foreach (var task in pendingTasks)
        {
            // Tìm người quản lý kế nhiệm hoặc quản lý trực tiếp của nhân viên nghỉ việc để gán lại
            var managerId = await _orgService.GetDirectManagerIdAsync(notification.EmployeeId, cancellationToken);
            if (managerId.HasValue)
            {
                task.AssignedToUserId = managerId.Value;
                task.Status = "Pending";
                await _taskRepo.UpdateAsync(task, cancellationToken);
                _logger.LogWarning("Nhiệm vụ {TaskId} đã được điều chuyển tự động từ nhân sự thôi việc {OldUser} sang Quản lý {Manager}",
                    task.Id, notification.EmployeeId, managerId.Value);
            }
            else
            {
                task.Status = "Cancelled";
                await _taskRepo.UpdateAsync(task, cancellationToken);
                _logger.LogError("Nhiệm vụ {TaskId} bị hủy vì nhân sự {OldUser} thôi việc và không tìm thấy quản lý trực tiếp!", 
                    task.Id, notification.EmployeeId);
            }
        }
    }
}
```

---

## 6. Điểm kiểm soát trọng yếu (Critical Control Points)

1. **Tính Bất biến của Lịch sử Phê duyệt (Approval History Immutability):**
   - **Rủi ro:** Khi xảy ra tranh chấp hoặc kiểm toán nội bộ, kẻ xấu có thể can thiệp DB sửa đổi ý kiến duyệt hoặc thời gian duyệt.
   - **Kiểm soát:** Mỗi bản ghi trong `[workflow].[ApprovalHistory]` được bảo vệ bằng Database Trigger cấm `UPDATE`/`DELETE`. Đồng thời tạo trường `DigitalSignatureHash = SHA256(WorkflowInstanceId + StepOrder + ActorUserId + Decision + ActionTimestamp + Salt)` để phát hiện ngay lập tức bất kỳ sửa đổi trái phép nào.

2. **Khóa Phân tán Chống Duyệt Trùng lặp (Distributed Lock & Idempotency):**
   - **Rủi ro:** Người dùng nhấn nút Duyệt 2 lần liên tiếp do mạng lag hoặc gửi request đồng thời, khiến Elsa Resume 2 lần làm lỗi State Machine hoặc nhảy cóc tiến trình.
   - **Kiểm soát:** Áp dụng Redis Distributed Lock (`RedisDistributedLockService`) theo khóa `lock:approval_task:{taskId}` với TTL 10 giây kết hợp hàng đợi Semaphore nội bộ. Dịch vụ tận dụng hạ tầng Redis phân tán tập trung (`IDistributedCache` qua `StackExchange.Redis`) được cấp phát từ tầng Host, hỗ trợ kết nối máy chủ Redis riêng biệt (Docker, VM hoặc Cloud). Khi tiếp nhận request, kiểm tra trạng thái task nếu khác `Pending` thì lập tức ném lỗi `TaskAlreadyProcessedException`.

3. **Giao dịch Toàn vẹn Trạng thái (Atomic State Synchronization):**
   - **Rủi ro:** Elsa Bookmark hoàn tất nhưng cập nhật trạng thái bảng `WorkflowSubmissions` bị lỗi kết nối DB, dẫn đến thông tin hiển thị trên portal lệch với runtime engine.
   - **Kiểm soát:** Bọc việc cập nhật `WorkflowSubmissions.CurrentStatus`, ghi nhận `ApprovalHistory` và phát MediatR Event trong cùng một `IUnitOfWork` (SQL Server Transaction). Nếu có bất kỳ lỗi nào, rollback toàn bộ trạng thái.

4. **Xử lý Nhiệm vụ Mồ côi khi Nhân sự Nghỉ việc (Orphaned Task Prevention):**
   - **Rủi ro:** Người duyệt nghỉ việc đột xuất, các phiếu trình ký đang ở bước duyệt của người này bị kẹt vô thời hạn.
   - **Kiểm soát:** Module tiếp nhận `EmployeeDeactivatedEvent` từ Module OrgManagement và tự động điều chuyển toàn bộ Active Tasks sang Quản lý cấp trên hoặc Quản trị viên quy trình, đồng thời gửi thông báo cảnh báo.

5. **Bảo mật Webhook Trigger từ Hệ thống Ngoài:**
   - **Rủi ro:** Kẻ xấu dò trúng URL Webhook của Elsa và resume đơn hàng không qua kiểm chứng.
   - **Kiểm soát:** Mọi Webhook kích hoạt từ ERP/SAP phải được ký điện tử HMAC-SHA256 với Secret Key được cấu hình riêng trong hệ thống.
