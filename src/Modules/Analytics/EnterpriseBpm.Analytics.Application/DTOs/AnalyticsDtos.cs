using EnterpriseBpm.Analytics.Domain.Enums;

namespace EnterpriseBpm.Analytics.Application.DTOs;

public record ExecutiveDashboardMetricsDto(
    int TotalSubmissions,
    int InFlightCount,
    int CompletedCount,
    int BreachedSlaCount,
    double CorporateSlaComplianceRate,
    decimal TotalApprovedAmount,
    int ActiveBottlenecksCount,
    IReadOnlyList<DailySubmissionTrendDto> SubmissionTrends,
    IReadOnlyList<DepartmentExpenditureDto> DepartmentExpenditures,
    IReadOnlyList<WorkflowTypePerformanceDto> WorkflowPerformances,
    DateTime GeneratedAt
);

public record DailySubmissionTrendDto(
    string Date,
    int TotalSubmissions,
    int CompletedCount,
    int BreachedCount
);

public record DepartmentExpenditureDto(
    string DepartmentName,
    decimal TotalAmount,
    int SubmissionCount
);

public record WorkflowTypePerformanceDto(
    string WorkflowTitle,
    int SubmissionsCount,
    double AvgLeadTimeHours,
    double SlaComplianceRate
);

public record StepSlaAnalyticsDto(
    Guid Id,
    string WorkflowDefinitionId,
    string WorkflowTitle,
    string ActivityId,
    string StepName,
    string? TargetRole,
    int TotalTasksProcessed,
    double AvgWaitTimeMinutes,
    double AvgExecutionTimeMinutes,
    double SlaAllowedHours,
    int SlaBreachCount,
    double SlaBreachPercentage,
    bool IsCurrentBottleneck,
    BottleneckSeverity BottleneckSeverity,
    DateTime CalculatedAt
);

public record EmployeeProductivityDto(
    Guid UserId,
    string UserName,
    string? UserEmail,
    string? DepartmentName,
    string YearMonth,
    int TotalAssignedTasks,
    int CompletedTasks,
    int PendingBacklog,
    int CompletedOnTime,
    int CompletedOverdue,
    double SlaComplianceRate,
    int FirstTimeRightCount,
    double FirstTimeRightRate,
    double AvgProcessingTimeMinutes,
    string ScoreRank
);

public record FinancialAnalyticsSummaryDto(
    decimal TotalApprovedAmount,
    int TotalApprovedCount,
    decimal MaxSingleAmount,
    decimal AvgAmountPerSubmission,
    IReadOnlyList<CostCenterExpenditureDto> CostCenterBreakdown,
    IReadOnlyList<MonthlyExpenditureTrendDto> MonthlyTrends
);

public record CostCenterExpenditureDto(
    string CostCenterCode,
    string CostCenterName,
    decimal Amount,
    double Percentage
);

public record MonthlyExpenditureTrendDto(
    string Month,
    decimal ApprovedAmount,
    int Count
);

public record DynamicReportColumnDto(
    string Field,
    string Header,
    string DataType, // "string", "number", "date", "currency"
    bool IsGroupable = false
);

public record DynamicReportFilterDto(
    string Field,
    string Operator, // "=", "!=", ">", ">=", "<", "<=", "contains"
    string Value
);

public record ExecuteReportRequestDto(
    DataSourceType DataSource,
    IReadOnlyList<string> SelectedColumns,
    IReadOnlyList<DynamicReportFilterDto>? Filters = null,
    IReadOnlyList<string>? GroupingColumns = null,
    string? SortColumn = null,
    bool IsAscending = false,
    int Page = 1,
    int PageSize = 50
);

public record DynamicReportResultDto(
    IReadOnlyList<DynamicReportColumnDto> Columns,
    IReadOnlyList<Dictionary<string, object?>> Rows,
    int TotalCount,
    int Page,
    int PageSize,
    long ExecutionTimeMs
);

public record CustomReportTemplateDto(
    Guid Id,
    string ReportKey,
    string Title,
    string? Description,
    DataSourceType DataSource,
    IReadOnlyList<string> SelectedColumns,
    IReadOnlyList<DynamicReportFilterDto> Filters,
    IReadOnlyList<string> GroupingColumns,
    string? SortColumn,
    bool IsAscending,
    bool IsPublic,
    DateTime CreatedAt
);

public record SaveReportTemplateRequestDto(
    string Title,
    string? Description,
    DataSourceType DataSource,
    IReadOnlyList<string> SelectedColumns,
    IReadOnlyList<DynamicReportFilterDto>? Filters = null,
    IReadOnlyList<string>? GroupingColumns = null,
    string? SortColumn = null,
    bool IsAscending = false,
    bool IsPublic = true
);

public record ScheduledReportSubscriptionDto(
    Guid Id,
    Guid TemplateId,
    string TemplateTitle,
    string Title,
    string CronExpression,
    ReportFrequency Frequency,
    ReportExportFormat ExportFormat,
    IReadOnlyList<string> Recipients,
    bool IsActive,
    DateTime? LastRunAt,
    DateTime? NextRunAt,
    string? LastRunStatus
);

public record CreateSubscriptionRequestDto(
    Guid TemplateId,
    string Title,
    string CronExpression,
    ReportFrequency Frequency,
    ReportExportFormat ExportFormat,
    IReadOnlyList<string> Recipients
);

