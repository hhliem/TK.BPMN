using EnterpriseBpm.Analytics.Domain.Enums;
using EnterpriseBpm.SharedKernel;
using EnterpriseBpm.SharedKernel.Contracts;

namespace EnterpriseBpm.Analytics.Domain.Entities;

/// <summary>
/// Bảng phân tích chi tiết hiệu suất từng bước duyệt & phát hiện nút thắt cổ chai (TSK-701, TSK-704)
/// </summary>
public class StepSlaAnalytics : BaseEntity<Guid>, IAuditable
{
    public string WorkflowDefinitionId { get; private set; } = default!;
    public string WorkflowTitle { get; private set; } = default!;
    public string ActivityId { get; private set; } = default!;
    public string StepName { get; private set; } = default!;
    public string? TargetRole { get; private set; }

    public int TotalTasksProcessed { get; private set; }
    public long TotalWaitTimeMinutes { get; private set; }
    public double AvgWaitTimeMinutes { get; private set; }
    public long TotalExecutionTimeMinutes { get; private set; }
    public double AvgExecutionTimeMinutes { get; private set; }

    public double SlaAllowedHours { get; private set; } = 24.0;
    public int SlaBreachCount { get; private set; }
    public double SlaBreachPercentage { get; private set; } // %

    public bool IsCurrentBottleneck { get; private set; } = false;
    public BottleneckSeverity BottleneckSeverity { get; private set; } = BottleneckSeverity.None;

    public DateTime CalculatedAt { get; private set; } = DateTime.UtcNow;

    // IAuditable
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public Guid? CreatedBy { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public Guid? UpdatedBy { get; set; }

    protected StepSlaAnalytics() { }

    public static StepSlaAnalytics Create(
        string workflowDefinitionId,
        string workflowTitle,
        string activityId,
        string stepName,
        string? targetRole,
        int totalTasksProcessed,
        long totalWaitTimeMinutes,
        long totalExecutionTimeMinutes,
        double slaAllowedHours,
        int slaBreachCount)
    {
        var avgWait = totalTasksProcessed > 0
            ? Math.Round((double)totalWaitTimeMinutes / totalTasksProcessed, 1)
            : 0.0;

        var avgExec = totalTasksProcessed > 0
            ? Math.Round((double)totalExecutionTimeMinutes / totalTasksProcessed, 1)
            : 0.0;

        var breachPct = totalTasksProcessed > 0
            ? Math.Round((double)slaBreachCount / totalTasksProcessed * 100, 2)
            : 0.0;

        // Tiêu chí phát hiện Bottleneck (TSK-704):
        // 1. Tỷ lệ vi phạm SLA >= 25% HOẶC
        // 2. Thời gian chờ trung bình vượt quá 150% thời hạn SLA cho phép
        var slaAllowedMinutes = slaAllowedHours * 60;
        var isBottleneck = breachPct >= 25.0 || (slaAllowedMinutes > 0 && avgWait > slaAllowedMinutes * 1.5);

        var severity = BottleneckSeverity.None;
        if (isBottleneck)
        {
            if (breachPct >= 75.0 || avgWait > slaAllowedMinutes * 2.5)
                severity = BottleneckSeverity.Critical;
            else if (breachPct >= 50.0 || avgWait > slaAllowedMinutes * 2.0)
                severity = BottleneckSeverity.High;
            else
                severity = BottleneckSeverity.Medium;
        }

        return new StepSlaAnalytics
        {
            Id = Guid.NewGuid(),
            WorkflowDefinitionId = workflowDefinitionId,
            WorkflowTitle = workflowTitle,
            ActivityId = activityId,
            StepName = stepName,
            TargetRole = targetRole,
            TotalTasksProcessed = totalTasksProcessed,
            TotalWaitTimeMinutes = totalWaitTimeMinutes,
            AvgWaitTimeMinutes = avgWait,
            TotalExecutionTimeMinutes = totalExecutionTimeMinutes,
            AvgExecutionTimeMinutes = avgExec,
            SlaAllowedHours = slaAllowedHours,
            SlaBreachCount = slaBreachCount,
            SlaBreachPercentage = breachPct,
            IsCurrentBottleneck = isBottleneck,
            BottleneckSeverity = severity,
            CalculatedAt = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow
        };
    }
}

