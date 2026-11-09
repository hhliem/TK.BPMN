using System.Text;
using EnterpriseBpm.Analytics.Application.DTOs;
using EnterpriseBpm.Analytics.Domain.Enums;

namespace EnterpriseBpm.Analytics.Application.Services;

/// <summary>
/// Động cơ biên dịch câu truy vấn động an toàn chống SQL Injection (TSK-707)
/// Phục vụ Dynamic Custom Report Builder, sinh câu lệnh SQL tham số hóa (Parameterized SQL).
/// </summary>
public class DynamicQueryBuilderEngine
{
    // Danh mục cột cho phép (Column Whitelisting) bảo đảm an ninh Big Data
    private static readonly Dictionary<DataSourceType, Dictionary<string, DynamicReportColumnDto>> ColumnWhitelists = new()
    {
        [DataSourceType.WorkflowSubmissions] = new(StringComparer.OrdinalIgnoreCase)
        {
            ["Id"] = new("Id", "Mã Hồ Sơ (GUID)", "string"),
            ["WorkflowInstanceId"] = new("WorkflowInstanceId", "Mã Phiếu Trình Ký", "string"),
            ["WorkflowDefinitionId"] = new("WorkflowDefinitionId", "Mã Định Nghĩa Quy Trình", "string"),
            ["FormKey"] = new("FormKey", "Mã Biểu Mẫu", "string", true),
            ["CurrentStatus"] = new("CurrentStatus", "Trạng Thái", "string", true),
            ["Priority"] = new("Priority", "Mức Ưu Tiên", "number"),
            ["SubmittedAt"] = new("SubmittedAt", "Thời Điểm Nộp", "date"),
            ["CompletedAt"] = new("CompletedAt", "Thời Điểm Hoàn Tất", "date")
        },
        [DataSourceType.ApprovalTasks] = new(StringComparer.OrdinalIgnoreCase)
        {
            ["Id"] = new("Id", "Mã Nhiệm Vụ", "string"),
            ["WorkflowInstanceId"] = new("WorkflowInstanceId", "Mã Phiếu Trình Ký", "string"),
            ["ActivityId"] = new("ActivityId", "Mã Bước Duyệt", "string", true),
            ["TargetRole"] = new("TargetRole", "Vai Trò Phê Duyệt", "string", true),
            ["Status"] = new("Status", "Trạng Thái Nhiệm Vụ", "string", true),
            ["DueDate"] = new("DueDate", "Hạn Chót SLA", "date"),
            ["IsEscalated"] = new("IsEscalated", "Đã Leo Thang", "string"),
            ["CreatedAt"] = new("CreatedAt", "Thời Điểm Giao Việc", "date"),
            ["CompletedAt"] = new("CompletedAt", "Thời Điểm Duyệt Xong", "date")
        },
        [DataSourceType.FinancialExpenditure] = new(StringComparer.OrdinalIgnoreCase)
        {
            ["WorkflowInstanceId"] = new("WorkflowInstanceId", "Mã Phiếu Trình Ký", "string"),
            ["WorkflowTitle"] = new("WorkflowTitle", "Loại Chi Phí / Quy Trình", "string", true),
            ["DepartmentName"] = new("DepartmentName", "Phòng Ban Đề Xuất", "string", true),
            ["TotalApprovedAmount"] = new("TotalApprovedAmount", "Số Tiền Duyệt Chi (VNĐ)", "currency"),
            ["PeriodDate"] = new("PeriodDate", "Ngày Duyệt Chi", "date")
        },
        [DataSourceType.EmployeeProductivity] = new(StringComparer.OrdinalIgnoreCase)
        {
            ["UserName"] = new("UserName", "Họ Tên Nhân Sự", "string"),
            ["DepartmentName"] = new("DepartmentName", "Phòng Ban", "string", true),
            ["YearMonth"] = new("YearMonth", "Tháng Báo Cáo", "string", true),
            ["TotalAssignedTasks"] = new("TotalAssignedTasks", "Tổng Task Giao", "number"),
            ["CompletedTasks"] = new("CompletedTasks", "Task Đã Xử Lý", "number"),
            ["PendingBacklog"] = new("PendingBacklog", "Task Tồn Đọng", "number"),
            ["SlaComplianceRate"] = new("SlaComplianceRate", "Tỷ Lệ Đúng Hạn SLA (%)", "number"),
            ["FirstTimeRightRate"] = new("FirstTimeRightRate", "Tỷ Lệ Đạt Lần Đầu (%)", "number"),
            ["ScoreRank"] = new("ScoreRank", "Xếp Hạng Thi Đua", "string", true)
        }
    };

    public IReadOnlyList<DynamicReportColumnDto> GetColumns(DataSourceType dataSource)
    {
        return ColumnWhitelists.TryGetValue(dataSource, out var map)
            ? map.Values.ToList()
            : Array.Empty<DynamicReportColumnDto>();
    }

    /// <summary>
    /// Biên dịch yêu cầu báo cáo thành câu lệnh SQL Parameterized an toàn
    /// </summary>
    public (string Sql, string CountSql, Dictionary<string, object> Parameters, IReadOnlyList<DynamicReportColumnDto> Columns) BuildQuery(ExecuteReportRequestDto request)
    {
        if (!ColumnWhitelists.TryGetValue(request.DataSource, out var allowedColumns))
        {
            throw new ArgumentException($"Nguồn dữ liệu không hợp lệ: {request.DataSource}");
        }

        // 1. Xác định bảng cơ sở
        var tableName = request.DataSource switch
        {
            DataSourceType.WorkflowSubmissions => "[workflow].[WorkflowSubmissions]",
            DataSourceType.ApprovalTasks => "[workflow].[ApprovalTasks]",
            DataSourceType.FinancialExpenditure => "[analytics].[WorkflowSummaryMetrics]",
            DataSourceType.EmployeeProductivity => "[analytics].[EmployeeProductivityMetrics]",
            _ => throw new ArgumentOutOfRangeException(nameof(request.DataSource))
        };

        // 2. Lựa chọn danh sách cột (chỉ lấy các cột nằm trong Whitelist)
        var selectedColumns = new List<DynamicReportColumnDto>();
        var selectClauses = new List<string>();

        if (request.SelectedColumns == null || request.SelectedColumns.Count == 0)
        {
            selectedColumns.AddRange(allowedColumns.Values);
            selectClauses.AddRange(allowedColumns.Keys.Select(k => $"[{k}]"));
        }
        else
        {
            foreach (var colName in request.SelectedColumns)
            {
                if (allowedColumns.TryGetValue(colName, out var colDef))
                {
                    selectedColumns.Add(colDef);
                    selectClauses.Add($"[{colDef.Field}]");
                }
            }

            if (selectClauses.Count == 0)
            {
                selectedColumns.AddRange(allowedColumns.Values);
                selectClauses.AddRange(allowedColumns.Keys.Select(k => $"[{k}]"));
            }
        }

        var parameters = new Dictionary<string, object>();
        var whereClauses = new List<string>();
        int paramIndex = 0;

        // 3. Tiêu chí bộ lọc (Parameterized Filters)
        if (request.Filters != null && request.Filters.Count > 0)
        {
            foreach (var f in request.Filters)
            {
                if (string.IsNullOrWhiteSpace(f.Field) || string.IsNullOrWhiteSpace(f.Value)) continue;
                if (!allowedColumns.TryGetValue(f.Field, out var colDef)) continue;

                var pName = $"@p{paramIndex++}";

                switch (f.Operator?.Trim())
                {
                    case "=":
                        whereClauses.Add($"[{colDef.Field}] = {pName}");
                        parameters[pName] = ParseValue(colDef.DataType, f.Value);
                        break;
                    case "!=":
                        whereClauses.Add($"[{colDef.Field}] != {pName}");
                        parameters[pName] = ParseValue(colDef.DataType, f.Value);
                        break;
                    case ">":
                        whereClauses.Add($"[{colDef.Field}] > {pName}");
                        parameters[pName] = ParseValue(colDef.DataType, f.Value);
                        break;
                    case ">=":
                        whereClauses.Add($"[{colDef.Field}] >= {pName}");
                        parameters[pName] = ParseValue(colDef.DataType, f.Value);
                        break;
                    case "<":
                        whereClauses.Add($"[{colDef.Field}] < {pName}");
                        parameters[pName] = ParseValue(colDef.DataType, f.Value);
                        break;
                    case "<=":
                        whereClauses.Add($"[{colDef.Field}] <= {pName}");
                        parameters[pName] = ParseValue(colDef.DataType, f.Value);
                        break;
                    case "contains":
                        whereClauses.Add($"[{colDef.Field}] LIKE {pName}");
                        parameters[pName] = $"%{f.Value}%";
                        break;
                    default:
                        whereClauses.Add($"[{colDef.Field}] = {pName}");
                        parameters[pName] = f.Value;
                        break;
                }
            }
        }

        var whereSql = whereClauses.Count > 0 ? "WHERE " + string.Join(" AND ", whereClauses) : string.Empty;

        // 4. Sắp xếp an toàn
        var sortCol = !string.IsNullOrWhiteSpace(request.SortColumn) && allowedColumns.ContainsKey(request.SortColumn)
            ? $"[{request.SortColumn}]"
            : selectClauses[0];

        var sortDirection = request.IsAscending ? "ASC" : "DESC";

        // 5. Phân trang SQL Server OFFSET...FETCH
        var page = Math.Max(1, request.Page);
        var pageSize = Math.Clamp(request.PageSize, 1, 5000); // Tối đa 5,000 dòng bảo đảm Memory Guard
        var offset = (page - 1) * pageSize;

        parameters["@offset"] = offset;
        parameters["@limit"] = pageSize;

        var dataSql = $@"
SELECT {string.Join(", ", selectClauses)}
FROM {tableName}
{whereSql}
ORDER BY {sortCol} {sortDirection}
OFFSET @offset ROWS FETCH NEXT @limit ROWS ONLY;
";

        var countSql = $@"
SELECT COUNT(1)
FROM {tableName}
{whereSql};
";

        return (dataSql, countSql, parameters, selectedColumns);
    }

    private static object ParseValue(string dataType, string rawValue)
    {
        if (dataType == "number")
        {
            if (long.TryParse(rawValue, out var lVal)) return lVal;
            if (double.TryParse(rawValue, out var dVal)) return dVal;
        }
        else if (dataType == "currency")
        {
            if (decimal.TryParse(rawValue, out var decVal)) return decVal;
        }
        else if (dataType == "date")
        {
            if (DateTime.TryParse(rawValue, out var dtVal)) return dtVal;
        }
        return rawValue;
    }
}

