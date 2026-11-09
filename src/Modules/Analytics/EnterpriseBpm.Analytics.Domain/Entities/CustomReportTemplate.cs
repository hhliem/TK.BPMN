using EnterpriseBpm.Analytics.Domain.Enums;
using EnterpriseBpm.SharedKernel;
using EnterpriseBpm.SharedKernel.Contracts;

namespace EnterpriseBpm.Analytics.Domain.Entities;

/// <summary>
/// Mẫu báo cáo tùy biến động do người dùng cấu hình (Dynamic Report Builder Template - TSK-701, TSK-707)
/// </summary>
public class CustomReportTemplate : BaseEntity<Guid>, IAuditable
{
    public string ReportKey { get; private set; } = default!;
    public string Title { get; private set; } = default!;
    public string? Description { get; private set; }
    public DataSourceType DataSource { get; private set; } = DataSourceType.WorkflowSubmissions;

    // Danh sách cột hiển thị (JSON: ["instanceId", "workflowTitle", "submittedAt", "totalAmount", "status"])
    public string SelectedColumnsJson { get; private set; } = "[]";

    // Tiêu chí bộ lọc (JSON: [{"field":"totalAmount","operator":">","value":"100000000"},{"field":"status","operator":"=","value":"Completed"}])
    public string FilterCriteriaJson { get; private set; } = "[]";

    // Cột gom nhóm (JSON: ["departmentName"])
    public string GroupingColumnsJson { get; private set; } = "[]";

    // Cột sắp xếp
    public string? SortColumn { get; private set; }
    public bool IsAscending { get; private set; } = false;

    public Guid CreatedByUserId { get; private set; }
    public bool IsPublic { get; private set; } = true;
    public bool IsActive { get; private set; } = true;

    // IAuditable
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public Guid? CreatedBy { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public Guid? UpdatedBy { get; set; }

    protected CustomReportTemplate() { }

    public static CustomReportTemplate Create(
        string reportKey,
        string title,
        string? description,
        DataSourceType dataSource,
        string selectedColumnsJson,
        string filterCriteriaJson,
        string groupingColumnsJson,
        string? sortColumn,
        bool isAscending,
        Guid createdByUserId,
        bool isPublic = true)
    {
        return new CustomReportTemplate
        {
            Id = Guid.NewGuid(),
            ReportKey = reportKey.Trim().ToLowerInvariant(),
            Title = title.Trim(),
            Description = description?.Trim(),
            DataSource = dataSource,
            SelectedColumnsJson = string.IsNullOrWhiteSpace(selectedColumnsJson) ? "[]" : selectedColumnsJson,
            FilterCriteriaJson = string.IsNullOrWhiteSpace(filterCriteriaJson) ? "[]" : filterCriteriaJson,
            GroupingColumnsJson = string.IsNullOrWhiteSpace(groupingColumnsJson) ? "[]" : groupingColumnsJson,
            SortColumn = sortColumn?.Trim(),
            IsAscending = isAscending,
            CreatedByUserId = createdByUserId,
            IsPublic = isPublic,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = createdByUserId
        };
    }
}

