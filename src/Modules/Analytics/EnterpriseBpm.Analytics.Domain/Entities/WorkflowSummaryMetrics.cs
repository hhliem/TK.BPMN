using EnterpriseBpm.Analytics.Domain.Enums;
using EnterpriseBpm.SharedKernel;
using EnterpriseBpm.SharedKernel.Contracts;

namespace EnterpriseBpm.Analytics.Domain.Entities;

/// <summary>
/// Bảng chỉ số tổng hợp quy trình tiền tính toán phục vụ Big Data Dashboard (TSK-701, TSK-703)
/// Được tổng hợp định kỳ bởi Background Worker, loại bỏ việc quét bảng Transactional lớn.
/// </summary>
public class WorkflowSummaryMetrics : BaseEntity<Guid>, IAuditable
{
    public DateTime PeriodDate { get; private set; }
    public AggregationPeriodType PeriodType { get; private set; } = AggregationPeriodType.Daily;
    public string WorkflowDefinitionId { get; private set; } = default!;
    public string WorkflowTitle { get; private set; } = default!;
    public Guid? DepartmentId { get; private set; }
    public string? DepartmentName { get; private set; }

    // Chỉ số số lượng
    public int TotalSubmissions { get; private set; }
    public int CompletedCount { get; private set; }
    public int RejectedCount { get; private set; }
    public int CancelledCount { get; private set; }
    public int InFlightCount { get; private set; }

    // Chỉ số SLA
    public int WithinSlaCount { get; private set; }
    public int BreachedSlaCount { get; private set; }
    public double SlaComplianceRate { get; private set; } // % (0 - 100)

    // Thời gian xử lý Lead Time (tính bằng phút)
    public long TotalLeadTimeMinutes { get; private set; }
    public double AvgLeadTimeMinutes { get; private set; }

    // Chỉ số tài chính
    public decimal TotalApprovedAmount { get; private set; }

    public DateTime CalculatedAt { get; private set; } = DateTime.UtcNow;

    // IAuditable
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public Guid? CreatedBy { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public Guid? UpdatedBy { get; set; }

    protected WorkflowSummaryMetrics() { }

    public static WorkflowSummaryMetrics Create(
        DateTime periodDate,
        AggregationPeriodType periodType,
        string workflowDefinitionId,
        string workflowTitle,
        Guid? departmentId,
        string? departmentName,
        int totalSubmissions,
        int completedCount,
        int rejectedCount,
        int cancelledCount,
        int inFlightCount,
        int withinSlaCount,
        int breachedSlaCount,
        long totalLeadTimeMinutes,
        decimal totalApprovedAmount)
    {
        var totalResolved = withinSlaCount + breachedSlaCount;
        var complianceRate = totalResolved > 0
            ? Math.Round((double)withinSlaCount / totalResolved * 100, 2)
            : 100.0;

        var avgLeadTime = completedCount > 0
            ? Math.Round((double)totalLeadTimeMinutes / completedCount, 1)
            : 0.0;

        return new WorkflowSummaryMetrics
        {
            Id = Guid.NewGuid(),
            PeriodDate = periodDate.Date,
            PeriodType = periodType,
            WorkflowDefinitionId = workflowDefinitionId,
            WorkflowTitle = workflowTitle,
            DepartmentId = departmentId,
            DepartmentName = departmentName,
            TotalSubmissions = totalSubmissions,
            CompletedCount = completedCount,
            RejectedCount = rejectedCount,
            CancelledCount = cancelledCount,
            InFlightCount = inFlightCount,
            WithinSlaCount = withinSlaCount,
            BreachedSlaCount = breachedSlaCount,
            SlaComplianceRate = complianceRate,
            TotalLeadTimeMinutes = totalLeadTimeMinutes,
            AvgLeadTimeMinutes = avgLeadTime,
            TotalApprovedAmount = totalApprovedAmount,
            CalculatedAt = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow
        };
    }
}

