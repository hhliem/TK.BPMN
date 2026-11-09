using EnterpriseBpm.Analytics.Application.DTOs;
using EnterpriseBpm.Analytics.Domain.Entities;
using EnterpriseBpm.Analytics.Domain.Enums;

namespace EnterpriseBpm.Analytics.Application.Contracts;

/// <summary>
/// Động cơ tính toán thời hạn SLA theo giờ làm việc hành chính chuẩn (TSK-702)
/// </summary>
public interface ISlaCalculationEngine
{
    DateTime CalculateDueDate(DateTime submittedAt, double slaHours, WorkingCalendar? calendar = null);
    double CalculateBusinessHoursElapsed(DateTime start, DateTime end, WorkingCalendar? calendar = null);
    bool IsWorkingTime(DateTime dateTime, WorkingCalendar? calendar = null);
    DateTime GetNextWorkingTime(DateTime dateTime, WorkingCalendar? calendar = null);
}

/// <summary>
/// Dịch vụ phân tích và phát hiện điểm nghẽn quy trình (TSK-704)
/// </summary>
public interface IBottleneckAnalysisService
{
    Task<IReadOnlyList<StepSlaAnalyticsDto>> GetBottlenecksAsync(string? workflowDefinitionId = null, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<StepSlaAnalyticsDto>> AnalyzeAndRecalculateBottlenecksAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// Dịch vụ bảng đo năng suất nhân sự theo đơn vị/tháng (TSK-705)
/// </summary>
public interface IEmployeeScorecardService
{
    Task<IReadOnlyList<EmployeeProductivityDto>> GetDepartmentScorecardsAsync(Guid? departmentId, string yearMonth, CancellationToken cancellationToken = default);
    Task<EmployeeProductivityDto?> GetEmployeeScorecardAsync(Guid userId, string yearMonth, CancellationToken cancellationToken = default);
}

/// <summary>
/// Dịch vụ phân tích tài chính dòng tiền chi phí trình ký (TSK-706)
/// </summary>
public interface IFinancialAnalyticsService
{
    Task<FinancialAnalyticsSummaryDto> GetExpenditureSummaryAsync(DateTime? fromDate = null, DateTime? toDate = null, CancellationToken cancellationToken = default);
}

/// <summary>
/// Dịch vụ Dashboard BI điều hành cho Ban Giám Đốc (TSK-703, TSK-710)
/// </summary>
public interface IExecutiveDashboardService
{
    Task<ExecutiveDashboardMetricsDto> GetDashboardMetricsAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// Dịch vụ tạo báo cáo tùy biến động và xuất file (TSK-707, TSK-708)
/// </summary>
public interface IDynamicReportService
{
    IReadOnlyList<DynamicReportColumnDto> GetAvailableColumns(DataSourceType dataSource);
    Task<DynamicReportResultDto> ExecuteReportAsync(ExecuteReportRequestDto request, CancellationToken cancellationToken = default);
    Task<byte[]> ExportReportFileAsync(ExecuteReportRequestDto request, ReportExportFormat format, CancellationToken cancellationToken = default);
    Task<Guid> SaveReportTemplateAsync(SaveReportTemplateRequestDto request, Guid userId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<CustomReportTemplateDto>> GetSavedTemplatesAsync(Guid? userId = null, CancellationToken cancellationToken = default);
    Task<CustomReportTemplateDto?> GetTemplateByIdAsync(Guid id, CancellationToken cancellationToken = default);
}

/// <summary>
/// Dịch vụ quản lý lập lịch xuất báo cáo định kỳ (TSK-709)
/// </summary>
public interface IReportSchedulerService
{
    Task<Guid> CreateSubscriptionAsync(CreateSubscriptionRequestDto request, Guid userId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ScheduledReportSubscriptionDto>> GetSubscriptionsAsync(CancellationToken cancellationToken = default);
    Task ExecuteSubscriptionAsync(Guid subscriptionId, CancellationToken cancellationToken = default);
    Task ProcessDueSubscriptionsAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// Dịch vụ tổng hợp chỉ số tiền tính toán phục vụ Big Data (TSK-703)
/// </summary>
public interface IMetricsAggregationService
{
    Task AggregateWorkflowMetricsAsync(DateTime targetDate, CancellationToken cancellationToken = default);
    Task AggregateStepSlaMetricsAsync(CancellationToken cancellationToken = default);
    Task AggregateEmployeeProductivityAsync(string yearMonth, CancellationToken cancellationToken = default);
}

/// <summary>
/// Repository Dapper đọc Big Data hiệu năng cao (Skill: High-Performance Tuning)
/// </summary>
public interface IAnalyticsDapperRepository
{
    Task<DynamicReportResultDto> ExecuteDynamicQueryAsync(
        string rawSql,
        Dictionary<string, object> parameters,
        IReadOnlyList<DynamicReportColumnDto> columns,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);

    Task<IEnumerable<Dictionary<string, object?>>> StreamRowsAsync(
        string rawSql,
        Dictionary<string, object> parameters,
        CancellationToken cancellationToken = default);
}

