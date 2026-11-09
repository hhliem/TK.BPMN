# Module 10: Analytics & Enterprise Reporting (Phân hệ Báo cáo Toàn hệ thống, Performance KPI & Quản trị SLA)

## 1. Tổng quan & Phạm vi Bounded Context
Module `Analytics & Enterprise Reporting` là phân hệ báo cáo thông minh và phân tích dữ liệu tập trung cho toàn bộ nền tảng Enterprise BPMN Platform. Phân hệ được thiết kế để cung cấp cái nhìn toàn cảnh (360-degree visibility) về hiệu suất vận hành, tính tuân thủ quy trình, và dòng tiền doanh nghiệp dành cho Ban Giám đốc, Lãnh đạo Khối/Phòng và Bộ phận Kiểm soát Nội bộ:

1. **Quản trị SLA Hiệu suất Cao (Advanced SLA Performance Engine):**
   - Đo lường SLA chính xác theo **Giờ làm việc thực tế (Business Working Hours)**: Tự động loại trừ các khoảng thời gian ngoài giờ làm việc (sau 17:30 và trước 08:00), các ngày cuối tuần (Thứ 7, Chủ Nhật) và các ngày Nghỉ lễ/Tết theo lịch doanh nghiệp.
   - Đo lường chi tiết SLA từng công đoạn (Step-level SLA) và SLA toàn trình từ khi nộp đến khi hoàn tất (End-to-End Lead Time).
   - Cơ chế cảnh báo sớm: Tự động phát tín hiệu cảnh báo khi task đạt $80\%$ thời hạn quy định.
2. **Phát hiện Nút thắt Cổ chai Quy trình (Process Bottleneck & Lead-Time Analysis):**
   - Tự động phát hiện các bước duyệt bị ứ đọng nhiều nhất, các phòng ban hoặc nhân sự có tỷ lệ trễ hạn cao nhất.
   - Cung cấp biểu đồ nhiệt (Heatmap) thể hiện thời gian xử lý trung bình theo từng loại đơn và từng bước duyệt.
3. **Thước đo Năng suất Nhân sự (Employee Workload & Performance Scorecard):**
   - Thống kê khối lượng công việc tồn đọng (Task Backlog) của từng nhân sự theo thời gian thực.
   - Đo lường tỷ lệ hoàn thành đúng hạn (SLA Compliance Rate), tỷ lệ duyệt ngay không qua chỉnh sửa (**First-Time-Right Rate**), và thời gian trung vị để xử lý một tác vụ.
4. **Báo cáo Dòng tiền & Ngân sách Trình ký (Financial Expenditure & Budget BI):**
   - Tổng hợp số tiền đề xuất, số tiền đã duyệt, và số tiền bị từ chối theo Dự án, Trung tâm Chi phí (Cost Center) và Phòng ban.
   - Cảnh báo các khoản chi bất thường hoặc tiệm cận hạn mức ngân sách định mức.
5. **Công cụ Tạo Báo cáo Động (Dynamic Custom Report Builder):**
   - Cho phép người dùng kéo thả các trường dữ liệu trên Form.io và bảng nghiệp vụ để tự thiết kế báo cáo theo nhu cầu quản trị riêng.
   - Hỗ trợ lưu trữ cấu hình mẫu báo cáo (Report Templates) và phân quyền ai được xem báo cáo đó.
6. **Lập lịch & Tự động Phân phối Báo cáo (Scheduled Automated Distribution):**
   - Tự động kết xuất báo cáo định kỳ (Hàng ngày, 08:00 sáng Thứ Hai hàng tuần, hoặc ngày đầu tháng) dạng Excel (.xlsx) / PDF và gửi thẳng vào Email O365 hoặc kênh Microsoft Teams của Ban Giám đốc.

### Ranh giới kiến trúc (Architectural Boundaries)
- **Database Schema riêng biệt:** `[analytics]` trên SQL Server.
- **Tách biệt Tải Vận hành (OLAP vs. OLTP):** 100% các câu hỏi phân tích, báo cáo tổng hợp và biểu đồ Dashboard chỉ được phép đọc từ các bảng số liệu đã gom sẵn (Materialized Metrics) trong schema `[analytics]`. Tuyệt đối không quét trực tiếp bảng giao dịch `[workflow].*` để tránh làm giảm tốc độ xử lý trình ký.
- **Giao tiếp Sự kiện:** Lắng nghe các Integration Events: `WorkflowCompletedEvent`, `TaskSlaBreachedEvent`, `TaskAssignedEvent`, `WorkflowApprovedEvent`, `WorkflowRejectedEvent`.

---

## 2. Thiết kế Cơ sở dữ liệu (SQL Server - Schema [analytics])

```sql
CREATE SCHEMA [analytics];
GO

-- 1. Bảng Cấu hình Lịch Làm việc Doanh nghiệp (Phục vụ tính SLA Working Hours)
CREATE TABLE [analytics].[WorkingCalendars] (
    [Id] UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    [Year] INT NOT NULL,
    [WorkDayStartHour] TIME NOT NULL DEFAULT '08:00:00',
    [WorkDayEndHour] TIME NOT NULL DEFAULT '17:30:00',
    [LunchBreakStartHour] TIME NOT NULL DEFAULT '12:00:00',
    [LunchBreakEndHour] TIME NOT NULL DEFAULT '13:30:00',
    [HolidayDate] DATE NOT NULL,
    [HolidayName] NVARCHAR(255) NOT NULL,
    [IsWorkDay] BIT NOT NULL DEFAULT 0 -- 0: Ngày nghỉ lễ, 1: Ngày làm bù
);
CREATE UNIQUE INDEX [IX_WorkingCalendars_Date] 
ON [analytics].[WorkingCalendars]([Year], [HolidayDate]);
GO

-- 2. Bảng Tổng hợp Chỉ số Trình ký Hàng ngày (Daily Workflow Metrics)
CREATE TABLE [analytics].[WorkflowSummaryMetrics] (
    [Id] UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    [ReportDate] DATE NOT NULL,
    [DepartmentId] UNIQUEIDENTIFIER NOT NULL,
    [DepartmentName] NVARCHAR(255) NOT NULL,
    [FormKey] NVARCHAR(100) NOT NULL,
    [TotalSubmitted] INT NOT NULL DEFAULT 0,
    [TotalApproved] INT NOT NULL DEFAULT 0,
    [TotalRejected] INT NOT NULL DEFAULT 0,
    [TotalCancelled] INT NOT NULL DEFAULT 0,
    [TotalInProgress] INT NOT NULL DEFAULT 0,
    [AverageProcessingHours] FLOAT NOT NULL DEFAULT 0, -- Tính theo Business Working Hours
    [MedianProcessingHours] FLOAT NOT NULL DEFAULT 0,
    [SlaBreachCount] INT NOT NULL DEFAULT 0,
    [SlaComplianceRate] FLOAT NOT NULL DEFAULT 100.0,
    [TotalFinancialAmount] DECIMAL(18, 2) NOT NULL DEFAULT 0, -- Tổng giá trị tài chính duyệt
    [UpdatedAt] DATETIME2 NOT NULL DEFAULT GETUTCDATE()
);
CREATE UNIQUE INDEX [IX_WorkflowSummaryMetrics_Composite]
ON [analytics].[WorkflowSummaryMetrics]([ReportDate], [DepartmentId], [FormKey]);
CREATE INDEX [IX_WorkflowSummaryMetrics_Lookup]
ON [analytics].[WorkflowSummaryMetrics]([ReportDate], [DepartmentId]) 
INCLUDE ([TotalSubmitted], [TotalApproved], [SlaComplianceRate], [TotalFinancialAmount]);
GO

-- 3. Bảng Phân tích Chi tiết SLA & Nút thắt Cổ chai theo Bước Duyệt (Step-Level SLA & Bottlenecks)
CREATE TABLE [analytics].[StepSlaAnalytics] (
    [Id] UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    [FormKey] NVARCHAR(100) NOT NULL,
    [StepName] NVARCHAR(255) NOT NULL,
    [StepOrder] INT NOT NULL,
    [DepartmentId] UNIQUEIDENTIFIER NOT NULL,
    [TargetSlaHours] INT NOT NULL,
    [TotalTasksProcessed] INT NOT NULL DEFAULT 0,
    [TotalBreachedTasks] INT NOT NULL DEFAULT 0,
    [AvgWaitHours] FLOAT NOT NULL DEFAULT 0,        -- Thời gian nằm chờ trong hàng đợi
    [AvgExecutionHours] FLOAT NOT NULL DEFAULT 0,   -- Thời gian từ khi mở đơn đến khi bấm nút
    [FirstTimeRightCount] INT NOT NULL DEFAULT 0,   -- Số lần duyệt ngay không cần trả lại sửa
    [IsCurrentBottleneck] BIT NOT NULL DEFAULT 0,   -- Đánh dấu bước đang là điểm nghẽn của quy trình
    [UpdatedAt] DATETIME2 NOT NULL DEFAULT GETUTCDATE()
);
CREATE UNIQUE INDEX [IX_StepSla_Composite] 
ON [analytics].[StepSlaAnalytics]([FormKey], [StepName], [DepartmentId]);
GO

-- 4. Bảng Thước đo Hiệu suất Nhân sự (Employee Productivity Scorecards)
CREATE TABLE [analytics].[EmployeeProductivityMetrics] (
    [Id] UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    [Year] INT NOT NULL,
    [Month] INT NOT NULL,
    [EmployeeId] UNIQUEIDENTIFIER NOT NULL,
    [FullName] NVARCHAR(255) NOT NULL,
    [OrgUnitId] UNIQUEIDENTIFIER NOT NULL,
    [DepartmentName] NVARCHAR(255) NOT NULL,
    [TotalAssignedTasks] INT NOT NULL DEFAULT 0,
    [TotalCompletedTasks] INT NOT NULL DEFAULT 0,
    [PendingBacklogTasks] INT NOT NULL DEFAULT 0,
    [AvgProcessingHours] FLOAT NOT NULL DEFAULT 0,
    [SlaBreachCount] INT NOT NULL DEFAULT 0,
    [SlaComplianceRate] FLOAT NOT NULL DEFAULT 100.0,
    [FirstTimeRightRate] FLOAT NOT NULL DEFAULT 100.0,
    [UpdatedAt] DATETIME2 NOT NULL DEFAULT GETUTCDATE()
);
CREATE UNIQUE INDEX [IX_EmployeeProductivity_Composite] 
ON [analytics].[EmployeeProductivityMetrics]([Year], [Month], [EmployeeId]);
GO

-- 5. Bảng Cấu hình Mẫu Báo cáo Động (Dynamic Custom Report Templates)
CREATE TABLE [analytics].[CustomReportTemplates] (
    [Id] UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    [ReportCode] NVARCHAR(100) NOT NULL UNIQUE,
    [Title] NVARCHAR(255) NOT NULL,
    [Description] NVARCHAR(500) NULL,
    [DataSourceKey] NVARCHAR(50) NOT NULL, -- "WorkflowSubmissions", "SlaPerformance", "Financials"
    [SelectedColumnsJson] NVARCHAR(MAX) NOT NULL, -- JSON mảng: [{"field": "formKey", "title": "Loại đơn"}, ...]
    [FilterCriteriaJson] NVARCHAR(MAX) NULL,     -- JSON điều kiện lọc: [{"field": "priority", "op": "eq", "value": 3}]
    [GroupByColumnsJson] NVARCHAR(MAX) NULL,     -- JSON gom nhóm
    [AggregationJson] NVARCHAR(MAX) NULL,        -- JSON hàm tính: [{"field": "amount", "func": "SUM"}]
    [ChartType] NVARCHAR(50) NULL,               -- "Bar", "Line", "Pie", "TableOnly"
    [CreatedByUserId] UNIQUEIDENTIFIER NOT NULL,
    [IsPublic] BIT NOT NULL DEFAULT 0,           -- 1: Toàn doanh nghiệp xem được, 0: Chỉ người tạo
    [CreatedAt] DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    [UpdatedAt] DATETIME2 NOT NULL DEFAULT GETUTCDATE()
);
GO

-- 6. Bảng Đăng ký Lên lịch Gửi Báo cáo Tự động (Scheduled Report Subscriptions)
CREATE TABLE [analytics].[ScheduledReportSubscriptions] (
    [Id] UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    [ReportTemplateId] UNIQUEIDENTIFIER NOT NULL,
    [ScheduleCron] NVARCHAR(100) NOT NULL,       -- Ví dụ: "0 8 * * 1" (8:00 sáng Thứ 2)
    [ExportFormat] NVARCHAR(20) NOT NULL DEFAULT 'xlsx', -- "xlsx", "pdf"
    [RecipientEmails] NVARCHAR(MAX) NOT NULL,    -- Danh sách email nhận cách nhau bởi dấu chấm phẩy
    [TeamsWebhookUrl] NVARCHAR(1000) NULL,       -- Webhook gửi trực tiếp vào kênh Teams
    [IsActive] BIT NOT NULL DEFAULT 1,
    [LastRunAt] DATETIME2 NULL,
    [NextRunAt] DATETIME2 NULL,
    CONSTRAINT [FK_ScheduledReports_Template] FOREIGN KEY ([ReportTemplateId]) 
        REFERENCES [analytics].[CustomReportTemplates]([Id]) ON DELETE CASCADE
);
GO
```

---

## 3. Thuật toán Đo lường SLA theo Giờ Làm việc Thực tế (Business Hours Engine)

```csharp
namespace EnterpriseBpm.Analytics.Services;

public interface ISlaCalculationEngine
{
    /// <summary>
    /// Tính thời gian xử lý thực tế (theo giờ làm việc), tự động bỏ qua ngoài giờ, thứ 7, chủ nhật và ngày lễ
    /// </summary>
    Task<double> CalculateBusinessWorkingHoursAsync(
        DateTime startTimeUtc, 
        DateTime endTimeUtc, 
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Tính toán thời hạn SLA kết thúc (Deadline) dựa trên số giờ SLA và lịch làm việc của công ty
    /// </summary>
    Task<DateTime> CalculateDueDateTimeAsync(
        DateTime startTimeUtc, 
        int slaHours, 
        CancellationToken cancellationToken = default);
}

public class BusinessHoursSlaEngine : ISlaCalculationEngine
{
    private readonly IWorkingCalendarRepository _calendarRepo;

    public BusinessHoursSlaEngine(IWorkingCalendarRepository calendarRepo)
    {
        _calendarRepo = calendarRepo;
    }

    public async Task<double> CalculateBusinessWorkingHoursAsync(
        DateTime startTimeUtc, DateTime endTimeUtc, CancellationToken cancellationToken)
    {
        if (endTimeUtc <= startTimeUtc) return 0;

        // Chuyển sang giờ địa phương (ICT - UTC+7)
        var localStart = startTimeUtc.AddHours(7);
        var localEnd = endTimeUtc.AddHours(7);

        var holidays = await _calendarRepo.GetHolidaysBetweenDatesAsync(
            DateOnly.FromDateTime(localStart), DateOnly.FromDateTime(localEnd), cancellationToken);

        double totalBusinessMinutes = 0;
        var current = localStart;

        var workStart = new TimeOnly(8, 0);
        var workEnd = new TimeOnly(17, 30);
        var lunchStart = new TimeOnly(12, 0);
        var lunchEnd = new TimeOnly(13, 30);

        while (current < localEnd)
        {
            var isWeekend = current.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday;
            var isHoliday = holidays.Contains(DateOnly.FromDateTime(current));

            // Chỉ tính nếu là ngày làm việc hợp lệ
            if (!isWeekend && !isHoliday)
            {
                var currentTime = TimeOnly.FromDateTime(current);
                if (currentTime >= workStart && currentTime < workEnd)
                {
                    // Trừ giờ nghỉ trưa
                    if (!(currentTime >= lunchStart && currentTime < lunchEnd))
                    {
                        totalBusinessMinutes += 1;
                    }
                }
            }

            current = current.AddMinutes(1);
        }

        return Math.Round(totalBusinessMinutes / 60.0, 2);
    }

    public async Task<DateTime> CalculateDueDateTimeAsync(
        DateTime startTimeUtc, int slaHours, CancellationToken cancellationToken)
    {
        // Tương tự, cộng dồn từng block thời gian làm việc để xác định mốc hạn chót chính xác
        var targetMinutes = slaHours * 60;
        var current = startTimeUtc.AddHours(7);
        int addedMinutes = 0;

        while (addedMinutes < targetMinutes)
        {
            current = current.AddMinutes(1);
            var isWeekend = current.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday;
            var currentTime = TimeOnly.FromDateTime(current);

            if (!isWeekend && currentTime >= new TimeOnly(8, 0) && currentTime < new TimeOnly(17, 30))
            {
                if (!(currentTime >= new TimeOnly(12, 0) && currentTime < new TimeOnly(13, 30)))
                {
                    addedMinutes++;
                }
            }
        }

        return current.AddHours(-7); // Trả về UTC
    }
}
```

---

## 4. API Báo cáo & Phân tích Toàn hệ thống (Executive BI & Reports API)

```csharp
namespace EnterpriseBpm.Analytics.Controllers;

[ApiController]
[Route("api/analytics/reporting")]
[Authorize]
public class EnterpriseReportingController : ControllerBase
{
    private readonly IEnterpriseReportingService _reportingService;
    private readonly ICustomReportEngine _reportEngine;

    public EnterpriseReportingController(
        IEnterpriseReportingService reportingService, 
        ICustomReportEngine reportEngine)
    {
        _reportingService = reportingService;
        _reportEngine = reportEngine;
    }

    /// <summary>
    /// Báo cáo Hiệu suất SLA toàn hệ thống & Phát hiện Nút thắt cổ chai
    /// </summary>
    [HttpGet("sla-performance")]
    public async Task<IActionResult> GetSystemSlaPerformance(
        [FromQuery] DateTime from, 
        [FromQuery] DateTime to, 
        [FromQuery] Guid? departmentId,
        CancellationToken cancellationToken)
    {
        var result = await _reportingService.GetSlaPerformanceAnalysisAsync(from, to, departmentId, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Bảng đo Năng suất Nhân sự (Scorecard cá nhân & phòng ban)
    /// </summary>
    [HttpGet("employee-scorecards")]
    public async Task<IActionResult> GetEmployeeProductivity(
        [FromQuery] int year, 
        [FromQuery] int month, 
        [FromQuery] Guid? departmentId,
        CancellationToken cancellationToken)
    {
        var result = await _reportingService.GetEmployeeScorecardsAsync(year, month, departmentId, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Báo cáo Tổng hợp Dòng tiền & Ngân sách Trình ký
    /// </summary>
    [HttpGet("financial-summary")]
    public async Task<IActionResult> GetFinancialSummary(
        [FromQuery] int year, 
        [FromQuery] int? month,
        CancellationToken cancellationToken)
    {
        var result = await _reportingService.GetFinancialSummaryAsync(year, month, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Thực thi Báo cáo Động theo Mẫu tùy chỉnh
    /// </summary>
    [HttpPost("custom-reports/{reportCode}/execute")]
    public async Task<IActionResult> ExecuteCustomReport(
        string reportCode, 
        [FromBody] DynamicReportFilterDto filters, 
        CancellationToken cancellationToken)
    {
        var data = await _reportEngine.ExecuteReportAsync(reportCode, filters, cancellationToken);
        return Ok(data);
    }

    /// <summary>
    /// Xuất Báo cáo Động ra File Excel (.xlsx) định dạng chuẩn doanh nghiệp
    /// </summary>
    [HttpPost("custom-reports/{reportCode}/export-excel")]
    public async Task<IActionResult> ExportCustomReportExcel(
        string reportCode, 
        [FromBody] DynamicReportFilterDto filters, 
        CancellationToken cancellationToken)
    {
        var excelBytes = await _reportEngine.ExportToExcelAsync(reportCode, filters, cancellationToken);
        return File(excelBytes, 
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", 
            $"Report_{reportCode}_{DateTime.UtcNow:yyyyMMdd_HHmm}.xlsx");
    }
}
```

---

## 5. Background Worker Lập lịch & Phân phối Báo cáo Tự động

```csharp
namespace EnterpriseBpm.Analytics.BackgroundJobs;

public class ScheduledReportDistributionWorker : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<ScheduledReportDistributionWorker> _logger;

    public ScheduledReportDistributionWorker(
        IServiceProvider serviceProvider, 
        ILogger<ScheduledReportDistributionWorker> logger)
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
                var subscriptionRepo = scope.ServiceProvider.GetRequiredService<IReportSubscriptionRepository>();
                var reportEngine = scope.ServiceProvider.GetRequiredService<ICustomReportEngine>();
                var notificationPublisher = scope.ServiceProvider.GetRequiredService<IMediator>();

                var dueSubscriptions = await subscriptionRepo.GetDueSubscriptionsAsync(DateTime.UtcNow, stoppingToken);
                foreach (var sub in dueSubscriptions)
                {
                    // 1. Sinh file báo cáo (Excel/PDF)
                    var fileBytes = await reportEngine.ExportReportByTemplateIdAsync(
                        sub.ReportTemplateId, sub.ExportFormat, stoppingToken);

                    // 2. Gửi qua Module Notification (Email O365 / Teams Webhook)
                    await notificationPublisher.Publish(new ScheduledReportGeneratedEvent(
                        SubscriptionId: sub.Id,
                        ReportTitle: sub.ReportTemplate.Title,
                        FileContent: fileBytes,
                        FileExtension: sub.ExportFormat,
                        RecipientEmails: sub.RecipientEmails.Split(';'),
                        TeamsWebhookUrl: sub.TeamsWebhookUrl
                    ), stoppingToken);

                    sub.LastRunAt = DateTime.UtcNow;
                    await subscriptionRepo.UpdateAsync(sub, stoppingToken);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi xảy ra trong quá trình quét và gửi báo cáo tự động theo lịch.");
            }

            // Quét định kỳ mỗi 5 phút
            await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
        }
    }
}
```

---

## 6. Cấu trúc Dự án & Phân tầng Mã nguồn (Clean Architecture)

```
EnterpriseBpm.Analytics/
├── EnterpriseBpm.Analytics.Domain/
│   ├── Entities/ (WorkingCalendar.cs, WorkflowSummaryMetric.cs, StepSlaAnalytics.cs, EmployeeProductivityMetric.cs, CustomReportTemplate.cs)
│   └── ValueObjects/ (BusinessTimeSpan.cs, DynamicReportCriteria.cs)
├── EnterpriseBpm.Analytics.Application/
│   ├── Contracts/ (ISlaCalculationEngine.cs, IEnterpriseReportingService.cs, ICustomReportEngine.cs)
│   ├── Queries/ (GetSlaPerformanceQuery.cs, GetEmployeeScorecardsQuery.cs, ExecuteCustomReportQuery.cs)
│   ├── EventHandlers/ (WorkflowCompletedMetricsHandler.cs, TaskSlaBreachedHandler.cs)
│   └── Builders/ (DynamicQueryBuilder.cs, ExcelReportStyler.cs)
├── EnterpriseBpm.Analytics.Infrastructure/
│   ├── Persistence/ (AnalyticsDbContext.cs)
│   ├── Services/ (BusinessHoursSlaEngine.cs, ClosedXmlExportService.cs)
│   └── BackgroundJobs/ (MetricsAggregationBackgroundWorker.cs, ScheduledReportDistributionWorker.cs)
└── EnterpriseBpm.Analytics.Presentation/
    └── Controllers/ (EnterpriseReportingController.cs, CustomReportTemplatesController.cs)
```

---

## 7. Điểm kiểm soát trọng yếu (Critical Control Points)

1. **Tính Toàn vẹn của Thuật toán SLA theo Giờ Hành chính (Working Hours Precision):**
   - **Rủi ro:** Khi đơn nộp vào lúc 17:00 chiều Thứ 6 với SLA 4 giờ, nếu cộng thô 4 giờ đơn sẽ bị báo trễ hạn vào 21:00 tối Thứ 6 thay vì 11:30 trưa Thứ 2 tuần kế tiếp, gây phạt oan uổng KPI nhân viên.
   - **Kiểm soát:** 100% phép tính SLA phải đi qua `BusinessHoursSlaEngine` với bảng dữ liệu lịch làm việc `[analytics].[WorkingCalendars]` được bộ phận HR cập nhật lịch nghỉ lễ chính thức đầu mỗi năm.
2. **Ngăn chặn SQL Injection trong Bộ tạo Báo cáo Động (Dynamic Query Sanitization):**
   - **Rủi ro:** Người dùng lợi dụng tính năng cấu hình trường dữ liệu và bộ lọc động để chèn mã độc SQL (`1=1; DROP TABLE...`).
   - **Kiểm soát:** Bộ `DynamicQueryBuilder` chỉ chấp nhận danh sách cột và bảng nằm trong **Whitelist Data Dictionaries** được định nghĩa cứng bằng Enum/Class C#. Tham số điều kiện lọc bắt buộc được biên dịch thành Parameterized Queries trong Dapper / EF Core, tuyệt đối không nối chuỗi thô.
3. **Cách ly Dữ liệu Báo cáo Giám đốc (Strict Executive Scoping):**
   - **Rủi ro:** Nhân viên phòng ban truy cập API `financial-summary` hoặc `employee-scorecards` để xem lương thưởng hoặc số liệu tài chính của phòng ban khác.
   - **Kiểm soát:** Tích hợp trực tiếp với Module 11: `AccessGovernance`. Kiểm tra quyền `report:financial:view` và `report:kpi:view`. Tự động áp dụng `IDataScopeEvaluator` để giới hạn phạm vi số liệu theo đúng quyền hạn của người xem.
