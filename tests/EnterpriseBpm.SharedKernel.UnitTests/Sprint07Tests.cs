using System.Text;
using EnterpriseBpm.Analytics.Application.Contracts;
using EnterpriseBpm.Analytics.Application.DTOs;
using EnterpriseBpm.Analytics.Application.Services;
using EnterpriseBpm.Analytics.Domain.Entities;
using EnterpriseBpm.Analytics.Domain.Enums;
using EnterpriseBpm.Analytics.Infrastructure.Exporters;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace EnterpriseBpm.SharedKernel.UnitTests;

/// <summary>
/// Bộ kiểm thử Unit Tests cho Sprint 07:
/// Analytics & Reporting Engine, Business Hours SLA Calculation,
/// Bottleneck Detection, Employee Productivity Scorecard,
/// Dynamic Parameterized SQL Query Builder, and Fast OpenXML Excel Export.
/// (TSK-715, TSK-716, TSK-717)
/// </summary>
public class Sprint07Tests
{
    private readonly ISlaCalculationEngine _slaEngine;
    private readonly WorkingCalendar _vietnamCalendar;

    public Sprint07Tests()
    {
        _slaEngine = new BusinessHoursSlaEngine(NullLogger<BusinessHoursSlaEngine>.Instance);
        _vietnamCalendar = WorkingCalendar.CreateDefaultVietnamCalendar();
    }

    // =========================================================================
    // TSK-715: Business Hours SLA Engine Tests (Precision, Lunch & Weekend Skips)
    // =========================================================================

    [Fact]
    public void SlaEngine_Friday1630_With4HoursSla_ShouldExpireMonday1100AM_GherkinScenario()
    {
        // Gherkin AC:
        // Given a workflow submitted on Friday at 16:30
        // And the step has a 4-hour SLA
        // And standard business hours are 08:00-12:00, 13:30-17:30 (Mon-Fri)
        // When the due date is calculated
        // Then it expires at exactly 11:00 AM on the following Monday

        // Arrange: Thứ 6, ngày 13/11/2026 lúc 16:30
        var submittedAt = new DateTime(2026, 11, 13, 16, 30, 0, DateTimeKind.Unspecified);
        Assert.Equal(DayOfWeek.Friday, submittedAt.DayOfWeek);

        // Act: Tính hạn chót với SLA = 4 giờ (240 phút)
        var dueDate = _slaEngine.CalculateDueDate(submittedAt, 4.0, _vietnamCalendar);

        // Assert:
        // Thứ 6: từ 16:30 đến 17:30 = tiêu thụ 1 giờ (60 phút). Còn 3 giờ (180 phút).
        // Thứ 7 & Chủ Nhật: Ngày nghỉ -> Bỏ qua.
        // Thứ 2 (16/11/2026): Bắt đầu ca sáng lúc 08:00.
        // 08:00 + 3 giờ = 11:00 AM (nằm trọn trong ca sáng 08:00 - 12:00).
        Assert.Equal(DayOfWeek.Monday, dueDate.DayOfWeek);
        Assert.Equal(2026, dueDate.Year);
        Assert.Equal(11, dueDate.Month);
        Assert.Equal(16, dueDate.Day);
        Assert.Equal(11, dueDate.Hour);
        Assert.Equal(0, dueDate.Minute);
    }

    [Fact]
    public void SlaEngine_SubmittedAt1100AM_With2HoursSla_ShouldSkipLunchBreak()
    {
        // Arrange: Thứ 4, lúc 11:00 AM, SLA = 2 giờ (120 phút)
        var submittedAt = new DateTime(2026, 11, 11, 11, 0, 0, DateTimeKind.Unspecified);

        // Act
        var dueDate = _slaEngine.CalculateDueDate(submittedAt, 2.0, _vietnamCalendar);

        // Assert:
        // Ca sáng từ 11:00 đến 12:00 = 1 giờ (60 phút). Còn 1 giờ (60 phút).
        // Nghỉ trưa từ 12:00 đến 13:30 -> Bỏ qua.
        // Đầu giờ chiều lúc 13:30 + 1 giờ = 14:30 PM.
        Assert.Equal(DayOfWeek.Wednesday, dueDate.DayOfWeek);
        Assert.Equal(14, dueDate.Hour);
        Assert.Equal(30, dueDate.Minute);
    }

    [Fact]
    public void SlaEngine_SubmittedOnWeekend_ShouldStartFromMondayMorning()
    {
        // Arrange: Thứ 7 (14/11/2026) lúc 15:00, SLA = 2 giờ
        var submittedAt = new DateTime(2026, 11, 14, 15, 0, 0, DateTimeKind.Unspecified);
        Assert.Equal(DayOfWeek.Saturday, submittedAt.DayOfWeek);

        // Act
        var dueDate = _slaEngine.CalculateDueDate(submittedAt, 2.0, _vietnamCalendar);

        // Assert: Bắt đầu tính từ Thứ 2 lúc 08:00 -> Hạn chót là 10:00 AM Thứ 2
        Assert.Equal(DayOfWeek.Monday, dueDate.DayOfWeek);
        Assert.Equal(16, dueDate.Day);
        Assert.Equal(10, dueDate.Hour);
        Assert.Equal(0, dueDate.Minute);
    }

    [Fact]
    public void SlaEngine_CalculateElapsedHours_ShouldOnlyCountBusinessWorkingHours()
    {
        // Arrange: Từ Thứ 6 lúc 16:30 đến Thứ 2 lúc 09:30
        var start = new DateTime(2026, 11, 13, 16, 30, 0, DateTimeKind.Unspecified); // Thứ 6
        var end = new DateTime(2026, 11, 16, 9, 30, 0, DateTimeKind.Unspecified);   // Thứ 2

        // Act
        var elapsedHours = _slaEngine.CalculateBusinessHoursElapsed(start, end, _vietnamCalendar);

        // Assert:
        // Chiều thứ 6: 16:30 -> 17:30 = 60 phút (1.0 giờ)
        // T7, CN: 0 giờ
        // Sáng thứ 2: 08:00 -> 09:30 = 90 phút (1.5 giờ)
        // Tổng: 1.0 + 1.5 = 2.5 giờ
        Assert.Equal(2.5, elapsedHours);
    }

    [Fact]
    public void SlaEngine_IsWorkingTime_ShouldAccuratelyIdentifyWorkingSlots()
    {
        // Arrange & Act & Assert
        // 08:30 sáng Thứ 2 -> Giờ làm việc hợp lệ
        var mondayMorning = new DateTime(2026, 11, 16, 8, 30, 0);
        Assert.True(_slaEngine.IsWorkingTime(mondayMorning, _vietnamCalendar));

        // 12:30 trưa Thứ 2 -> Giờ nghỉ trưa
        var mondayLunch = new DateTime(2026, 11, 16, 12, 30, 0);
        Assert.False(_slaEngine.IsWorkingTime(mondayLunch, _vietnamCalendar));

        // 14:00 chiều Thứ 2 -> Giờ làm việc hợp lệ
        var mondayAfternoon = new DateTime(2026, 11, 16, 14, 0, 0);
        Assert.True(_slaEngine.IsWorkingTime(mondayAfternoon, _vietnamCalendar));

        // 18:00 tối Thứ 2 -> Hết giờ làm việc
        var mondayEvening = new DateTime(2026, 11, 16, 18, 0, 0);
        Assert.False(_slaEngine.IsWorkingTime(mondayEvening, _vietnamCalendar));

        // Chủ Nhật 10:00 sáng -> Cuối tuần
        var sunday = new DateTime(2026, 11, 15, 10, 0, 0);
        Assert.False(_slaEngine.IsWorkingTime(sunday, _vietnamCalendar));
    }

    // =========================================================================
    // TSK-716: Bottleneck Detection & Employee Productivity Scorecard
    // =========================================================================

    [Fact]
    public void BottleneckDetection_WhenWaitTimeExceeds150PercentSla_ShouldFlagBottleneck()
    {
        // Arrange: SLA = 24 giờ (1440 phút). 10 tasks processed.
        // Tổng thời gian chờ = 24,000 phút -> Trung bình = 2,400 phút (~ 166% SLA)
        var analytics = StepSlaAnalytics.Create(
            workflowDefinitionId: "WF-PURCHASE-01",
            workflowTitle: "Quy trình Mua sắm Thiết bị",
            activityId: "Act_Finance_Review",
            stepName: "Kế toán trưởng Thẩm định Ngân sách",
            targetRole: "CHIEF_ACCOUNTANT",
            totalTasksProcessed: 10,
            totalWaitTimeMinutes: 24000,
            totalExecutionTimeMinutes: 3000,
            slaAllowedHours: 24.0,
            slaBreachCount: 2 // 20% vi phạm
        );

        // Assert: Do AvgWaitTime (2400m) > 1.5 * SlaAllowed (2160m)
        Assert.True(analytics.IsCurrentBottleneck);
        Assert.True(analytics.BottleneckSeverity >= BottleneckSeverity.Medium);
        Assert.Equal(2400.0, analytics.AvgWaitTimeMinutes);
    }

    [Fact]
    public void BottleneckDetection_WhenSlaBreachRateGte75Percent_ShouldFlagCritical()
    {
        // Arrange: 100 tasks, 80 vi phạm (80% breach rate)
        var analytics = StepSlaAnalytics.Create(
            workflowDefinitionId: "WF-PAYMENT-02",
            workflowTitle: "Quy trình Thanh toán Quốc tế",
            activityId: "Act_Legal_Review",
            stepName: "Ban Pháp chế Thẩm định",
            targetRole: "LEGAL_OFFICER",
            totalTasksProcessed: 100,
            totalWaitTimeMinutes: 10000,
            totalExecutionTimeMinutes: 5000,
            slaAllowedHours: 8.0,
            slaBreachCount: 80
        );

        // Assert
        Assert.True(analytics.IsCurrentBottleneck);
        Assert.Equal(BottleneckSeverity.Critical, analytics.BottleneckSeverity);
        Assert.Equal(80.0, analytics.SlaBreachPercentage);
    }

    [Fact]
    public void EmployeeProductivity_HighPerformance_ShouldYieldRankAPlus()
    {
        // Arrange: Nhân sự xử lý 50 tasks, 48 đúng hạn (96%), 46 đạt ngay lần đầu (92%)
        var scorecard = EmployeeProductivityMetrics.Create(
            userId: Guid.NewGuid(),
            userName: "Nguyễn Văn Trưởng Phòng",
            userEmail: "truongphong@enterprise.com",
            departmentId: Guid.NewGuid(),
            departmentName: "Phòng Tài Chính Kế Toán",
            yearMonth: "2026-11",
            totalAssignedTasks: 55,
            completedTasks: 50,
            pendingBacklog: 5,
            completedOnTime: 48,
            completedOverdue: 2,
            firstTimeRightCount: 46,
            avgProcessingTimeMinutes: 45.5
        );

        // Assert: 96% SLA compliance và 92% First-Time-Right -> Xếp hạng A+
        Assert.Equal(96.0, scorecard.SlaComplianceRate);
        Assert.Equal(92.0, scorecard.FirstTimeRightRate);
        Assert.Equal("A+", scorecard.ScoreRank);
        Assert.Equal(5, scorecard.PendingBacklog);
    }

    [Fact]
    public void EmployeeProductivity_LowSlaCompliance_ShouldYieldRankC()
    {
        // Arrange: Nhân sự xử lý 30 tasks, chỉ 18 đúng hạn (60%)
        var scorecard = EmployeeProductivityMetrics.Create(
            userId: Guid.NewGuid(),
            userName: "Trần Nhân Viên",
            userEmail: "nhanvien@enterprise.com",
            departmentId: Guid.NewGuid(),
            departmentName: "Phòng Hành Chính",
            yearMonth: "2026-11",
            totalAssignedTasks: 40,
            completedTasks: 30,
            pendingBacklog: 10,
            completedOnTime: 18,
            completedOverdue: 12,
            firstTimeRightCount: 20,
            avgProcessingTimeMinutes: 120.0
        );

        // Assert: 60% SLA compliance (< 70%) -> Xếp hạng C
        Assert.Equal(60.0, scorecard.SlaComplianceRate);
        Assert.Equal("C", scorecard.ScoreRank);
    }

    // =========================================================================
    // TSK-717: Dynamic Query Builder & OpenXML Fast Excel Export
    // =========================================================================

    [Fact]
    public void DynamicQueryBuilder_WithValidFilters_ShouldProduceParameterizedSql()
    {
        // Arrange
        var builder = new DynamicQueryBuilderEngine();
        var request = new ExecuteReportRequestDto(
            DataSource: DataSourceType.WorkflowSubmissions,
            SelectedColumns: new List<string> { "WorkflowInstanceId", "FormKey", "CurrentStatus", "SubmittedAt" },
            Filters: new List<DynamicReportFilterDto>
            {
                new("CurrentStatus", "=", "Approved"),
                new("FormKey", "contains", "PROC")
            },
            SortColumn: "SubmittedAt",
            IsAscending: false,
            Page: 1,
            PageSize: 25
        );

        // Act
        var result = builder.BuildQuery(request);

        // Assert: Kiểm tra SQL syntax, tham số hóa và bảo đảm an ninh
        Assert.NotNull(result.Sql);
        Assert.Contains("SELECT [WorkflowInstanceId], [FormKey], [CurrentStatus], [SubmittedAt]", result.Sql);
        Assert.Contains("FROM [workflow].[WorkflowSubmissions]", result.Sql);
        Assert.Contains("WHERE", result.Sql);
        Assert.Contains("[CurrentStatus] = @p0", result.Sql);
        Assert.Contains("[FormKey] LIKE @p1", result.Sql);
        Assert.Contains("ORDER BY [SubmittedAt] DESC", result.Sql);
        Assert.Contains("OFFSET @offset ROWS FETCH NEXT @limit ROWS ONLY", result.Sql);

        // Parameters
        Assert.Equal("Approved", result.Parameters["@p0"]);
        Assert.Equal("%PROC%", result.Parameters["@p1"]);
        Assert.Equal(0, result.Parameters["@offset"]);
        Assert.Equal(25, result.Parameters["@limit"]);
    }

    [Fact]
    public void DynamicQueryBuilder_SqlInjectionAttempt_InColumnName_ShouldBeSafelyIgnored_AndNotAppearingInSql()
    {
        // Arrange
        var builder = new DynamicQueryBuilderEngine();
        var attackRequest = new ExecuteReportRequestDto(
            DataSource: DataSourceType.WorkflowSubmissions,
            SelectedColumns: new List<string> { "FormKey; DROP TABLE [workflow].[Submissions];--" },
            Page: 1,
            PageSize: 20
        );

        // Act
        var result = builder.BuildQuery(attackRequest);

        // Assert: Cột chứa mã độc bị loại bỏ hoàn toàn khỏi câu truy vấn, không hề xuất hiện trong SQL
        Assert.DoesNotContain("DROP TABLE", result.Sql);
        Assert.DoesNotContain(";--", result.Sql);
        Assert.Contains("SELECT [Id], [WorkflowInstanceId]", result.Sql); // Fallback to safe whitelisted columns
    }

    [Fact]
    public void DynamicQueryBuilder_SqlInjectionAttempt_InFilterValue_ShouldBeParameterizedSafely()
    {
        // Arrange
        var builder = new DynamicQueryBuilderEngine();
        var attackRequest = new ExecuteReportRequestDto(
            DataSource: DataSourceType.WorkflowSubmissions,
            SelectedColumns: new List<string> { "WorkflowInstanceId", "CurrentStatus" },
            Filters: new List<DynamicReportFilterDto>
            {
                new("CurrentStatus", "=", "' OR '1'='1' --")
            },
            Page: 1,
            PageSize: 10
        );

        // Act
        var result = builder.BuildQuery(attackRequest);

        // Assert: Chuỗi payload độc hại nằm gọn trong parameter @p0, không thể phá vỡ câu lệnh SQL
        Assert.Contains("[CurrentStatus] = @p0", result.Sql);
        Assert.DoesNotContain("' OR '1'='1'", result.Sql);
        Assert.Equal("' OR '1'='1' --", result.Parameters["@p0"]);
    }

    [Fact]
    public void FastExcelExporter_ShouldProduceValidOpenXmlZipPackage()
    {
        // Arrange
        var columns = new List<DynamicReportColumnDto>
        {
            new("Code", "Mã Hồ Sơ", "string"),
            new("Amount", "Số Tiền (VNĐ)", "currency"),
            new("CreatedAt", "Ngày Tạo", "date")
        };

        var rows = new List<Dictionary<string, object?>>
        {
            new()
            {
                ["Code"] = "PR-2026-001",
                ["Amount"] = 150000000.0,
                ["CreatedAt"] = new DateTime(2026, 11, 1, 9, 30, 0)
            },
            new()
            {
                ["Code"] = "PR-2026-002",
                ["Amount"] = 45000000.0,
                ["CreatedAt"] = new DateTime(2026, 11, 2, 14, 15, 0)
            }
        };

        // Act
        var excelBytes = FastExcelExporter.GenerateExcel("Báo Cáo Giải Ngân Dự Án", columns, rows);

        // Assert: File Excel OpenXML chuẩn phải là file ZIP hợp lệ với Magic Bytes 'PK' (0x50, 0x4B)
        Assert.NotNull(excelBytes);
        Assert.True(excelBytes.Length > 1000); // File XLSX hoàn chỉnh thường từ vài KB
        Assert.Equal(0x50, excelBytes[0]); // 'P'
        Assert.Equal(0x4B, excelBytes[1]); // 'K'
        Assert.Equal(0x03, excelBytes[2]);
        Assert.Equal(0x04, excelBytes[3]);
    }

    [Fact]
    public void FastExcelExporter_GenerateCsv_ShouldIncludeUtf8BomAndHandleCommas()
    {
        // Arrange
        var columns = new List<DynamicReportColumnDto>
        {
            new("Name", "Họ Tên", "string"),
            new("Description", "Mô Tả", "string")
        };

        var rows = new List<Dictionary<string, object?>>
        {
            new()
            {
                ["Name"] = "Nguyễn Văn A",
                ["Description"] = "Dự án A, Giai đoạn 1 (Ưu tiên cao)"
            }
        };

        // Act
        var csvBytes = FastExcelExporter.GenerateCsv(columns, rows);
        var csvText = Encoding.UTF8.GetString(csvBytes);

        // Assert: Bắt đầu bằng BOM (\uFEFF) và chuỗi có dấu phẩy được bọc trong ngoặc kép
        Assert.StartsWith("\uFEFF", csvText);
        Assert.Contains("\"Dự án A, Giai đoạn 1 (Ưu tiên cao)\"", csvText);
    }
}
