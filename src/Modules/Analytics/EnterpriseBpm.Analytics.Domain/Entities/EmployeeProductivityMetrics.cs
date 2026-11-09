using EnterpriseBpm.SharedKernel;
using EnterpriseBpm.SharedKernel.Contracts;

namespace EnterpriseBpm.Analytics.Domain.Entities;

/// <summary>
/// Bảng chỉ số năng suất nhân sự (Employee Scorecard - TSK-701, TSK-705)
/// Theo dõi khối lượng công việc, tỷ lệ tuân thủ SLA và tỷ lệ phê duyệt đạt ngay lần đầu (First-Time-Right).
/// </summary>
public class EmployeeProductivityMetrics : BaseEntity<Guid>, IAuditable
{
    public Guid UserId { get; private set; }
    public string UserName { get; private set; } = default!;
    public string? UserEmail { get; private set; }
    public Guid? DepartmentId { get; private set; }
    public string? DepartmentName { get; private set; }
    public string YearMonth { get; private set; } = default!; // Format: YYYY-MM (Ví dụ: "2026-11")

    public int TotalAssignedTasks { get; private set; }
    public int CompletedTasks { get; private set; }
    public int PendingBacklog { get; private set; }

    public int CompletedOnTime { get; private set; }
    public int CompletedOverdue { get; private set; }
    public double SlaComplianceRate { get; private set; } // %

    public int FirstTimeRightCount { get; private set; }
    public double FirstTimeRightRate { get; private set; } // %

    public double AvgProcessingTimeMinutes { get; private set; }
    public string ScoreRank { get; private set; } = "A"; // Xuất sắc: A+, Giỏi: A, Khá: B, Cần cải thiện: C

    public DateTime CalculatedAt { get; private set; } = DateTime.UtcNow;

    // IAuditable
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public Guid? CreatedBy { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public Guid? UpdatedBy { get; set; }

    protected EmployeeProductivityMetrics() { }

    public static EmployeeProductivityMetrics Create(
        Guid userId,
        string userName,
        string? userEmail,
        Guid? departmentId,
        string? departmentName,
        string yearMonth,
        int totalAssignedTasks,
        int completedTasks,
        int pendingBacklog,
        int completedOnTime,
        int completedOverdue,
        int firstTimeRightCount,
        double avgProcessingTimeMinutes)
    {
        var slaRate = completedTasks > 0
            ? Math.Round((double)completedOnTime / completedTasks * 100, 2)
            : 100.0;

        var ftrRate = completedTasks > 0
            ? Math.Round((double)firstTimeRightCount / completedTasks * 100, 2)
            : 100.0;

        // Xếp loại thi đua (Scorecard Rank)
        string rank = "B";
        if (slaRate >= 95.0 && ftrRate >= 90.0) rank = "A+";
        else if (slaRate >= 85.0 && ftrRate >= 80.0) rank = "A";
        else if (slaRate >= 70.0) rank = "B";
        else rank = "C";

        return new EmployeeProductivityMetrics
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            UserName = userName,
            UserEmail = userEmail,
            DepartmentId = departmentId,
            DepartmentName = departmentName,
            YearMonth = yearMonth,
            TotalAssignedTasks = totalAssignedTasks,
            CompletedTasks = completedTasks,
            PendingBacklog = pendingBacklog,
            CompletedOnTime = completedOnTime,
            CompletedOverdue = completedOverdue,
            SlaComplianceRate = slaRate,
            FirstTimeRightCount = firstTimeRightCount,
            FirstTimeRightRate = ftrRate,
            AvgProcessingTimeMinutes = Math.Round(avgProcessingTimeMinutes, 1),
            ScoreRank = rank,
            CalculatedAt = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow
        };
    }
}

