using System.Data;
using Dapper;

namespace EnterpriseBpm.Workflow.Infrastructure.Queries;

public record SubmissionSummaryRow(
    Guid Id,
    string WorkflowInstanceId,
    string FormKey,
    Guid SubmittedByUserId,
    string CurrentStatus,
    int Priority,
    DateTime SubmittedAt,
    string RequesterName,
    string DepartmentName
);

/// <summary>
/// Truy vấn danh sách phiếu trình ký tải cao bằng Dapper kết hợp Index tối ưu
/// </summary>
public class HighThroughputSubmissionQuery
{
    private readonly IDbConnection _db;

    public HighThroughputSubmissionQuery(IDbConnection db)
    {
        _db = db;
    }

    public async Task<IReadOnlyList<SubmissionSummaryRow>> GetDepartmentSubmissionsAsync(
        Guid departmentId,
        string? status,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        const string sql = @"
            SELECT 
                s.[Id],
                s.[WorkflowInstanceId],
                s.[FormKey],
                s.[SubmittedByUserId],
                s.[CurrentStatus],
                s.[Priority],
                s.[SubmittedAt],
                e.[FullName] AS RequesterName,
                o.[Name] AS DepartmentName
            FROM [workflow].[WorkflowSubmissions] s WITH (NOLOCK)
            INNER JOIN [org].[Employees] e WITH (NOLOCK) ON s.[SubmittedByUserId] = e.[Id]
            INNER JOIN [org].[OrgUnits] o WITH (NOLOCK) ON s.[RequesterOrgUnitId] = o.[Id]
            WHERE s.[RequesterOrgUnitId] = @DepartmentId
              AND (@Status IS NULL OR s.[CurrentStatus] = @Status)
              AND s.[DeletedAt] IS NULL
            ORDER BY s.[SubmittedAt] DESC
            OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY;";

        var command = new CommandDefinition(
            sql,
            new
            {
                DepartmentId = departmentId,
                Status = string.IsNullOrWhiteSpace(status) ? null : status,
                Offset = (page - 1) * pageSize,
                PageSize = pageSize
            },
            cancellationToken: cancellationToken);

        var results = await _db.QueryAsync<SubmissionSummaryRow>(command);
        return results.AsList();
    }
}

