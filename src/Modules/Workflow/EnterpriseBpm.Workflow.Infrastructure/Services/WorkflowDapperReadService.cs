using System.Data;
using System.Data.Common;
using EnterpriseBpm.Workflow.Application.Contracts;
using EnterpriseBpm.Workflow.Application.DTOs;
using EnterpriseBpm.Workflow.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace EnterpriseBpm.Workflow.Infrastructure.Services;

public class WorkflowDapperReadService : IWorkflowDapperReadService
{
    private readonly WorkflowDbContext _dbContext;
    private readonly ILogger<WorkflowDapperReadService> _logger;

    public WorkflowDapperReadService(WorkflowDbContext dbContext, ILogger<WorkflowDapperReadService> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<PagedWorkflowResult<ApprovalTaskDto>> GetTasksForUserFastAsync(
        Guid userId,
        PendingTaskFilterDto filter,
        CancellationToken cancellationToken = default)
    {
        var connection = _dbContext.Database.GetDbConnection();
        if (connection.State != ConnectionState.Open)
        {
            await connection.OpenAsync(cancellationToken);
        }

        var offset = (filter.Page - 1) * filter.PageSize;
        var statusFilterSql = !string.IsNullOrWhiteSpace(filter.Status) ? "AND [Status] = @Status" : string.Empty;

        var countSql = $@"
SELECT COUNT(1)
FROM [workflow].[ApprovalTasks]
WHERE [AssignedToUserId] = @UserId {statusFilterSql};
";

        var dataSql = $@"
SELECT 
    [Id], [WorkflowInstanceId], [ActivityId], [TargetRole],
    [AssignedToUserId], [DelegatedFromUserId], [Status], [Decision],
    [Comment], [DueDate], [IsEscalated], [CompletedAt], [CreatedAt]
FROM [workflow].[ApprovalTasks]
WHERE [AssignedToUserId] = @UserId {statusFilterSql}
ORDER BY 
    CASE WHEN [DueDate] IS NOT NULL THEN 0 ELSE 1 END,
    [DueDate] ASC,
    [CreatedAt] DESC
OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY;
";

        // 1. Đếm tổng số
        int totalCount = 0;
        await using (var countCmd = connection.CreateCommand())
        {
            countCmd.CommandText = countSql;
            AddParameter(countCmd, "@UserId", userId);
            if (!string.IsNullOrWhiteSpace(filter.Status))
            {
                AddParameter(countCmd, "@Status", filter.Status);
            }

            var scalar = await countCmd.ExecuteScalarAsync(cancellationToken);
            if (scalar != null && scalar != DBNull.Value)
            {
                totalCount = Convert.ToInt32(scalar);
            }
        }

        // 2. Đọc danh sách bản ghi tuần tự (Streaming Read)
        var items = new List<ApprovalTaskDto>();
        await using (var dataCmd = connection.CreateCommand())
        {
            dataCmd.CommandText = dataSql;
            AddParameter(dataCmd, "@UserId", userId);
            AddParameter(dataCmd, "@Offset", offset);
            AddParameter(dataCmd, "@PageSize", filter.PageSize);
            if (!string.IsNullOrWhiteSpace(filter.Status))
            {
                AddParameter(dataCmd, "@Status", filter.Status);
            }

            await using var reader = await dataCmd.ExecuteReaderAsync(CommandBehavior.SequentialAccess, cancellationToken);
            while (await reader.ReadAsync(cancellationToken))
            {
                var id = reader.GetGuid(0);
                var wfInstanceId = reader.GetString(1);
                var actId = reader.GetString(2);
                var role = reader.IsDBNull(3) ? null : reader.GetString(3);
                var assignedTo = reader.GetGuid(4);
                var delegatedFrom = reader.IsDBNull(5) ? (Guid?)null : reader.GetGuid(5);
                var status = reader.GetString(6);
                var decision = reader.IsDBNull(7) ? null : reader.GetString(7);
                var comment = reader.IsDBNull(8) ? null : reader.GetString(8);
                var dueDate = reader.IsDBNull(9) ? (DateTime?)null : reader.GetDateTime(9);
                var isEscalated = reader.GetBoolean(10);
                var completedAt = reader.IsDBNull(11) ? (DateTime?)null : reader.GetDateTime(11);
                var createdAt = reader.GetDateTime(12);

                items.Add(new ApprovalTaskDto(
                    id, wfInstanceId, actId, role, assignedTo,
                    delegatedFrom, status, decision, comment, dueDate,
                    isEscalated, completedAt, createdAt
                ));
            }
        }

        return new PagedWorkflowResult<ApprovalTaskDto>(items, totalCount, filter.Page, filter.PageSize);
    }

    public async Task<PagedWorkflowResult<WorkflowSubmissionDto>> GetSubmissionsFastAsync(
        int page,
        int pageSize,
        string? status = null,
        Guid? submittedByUserId = null,
        CancellationToken cancellationToken = default)
    {
        var connection = _dbContext.Database.GetDbConnection();
        if (connection.State != ConnectionState.Open)
        {
            await connection.OpenAsync(cancellationToken);
        }

        var offset = (page - 1) * pageSize;
        var whereClauses = new List<string>();
        if (!string.IsNullOrWhiteSpace(status)) whereClauses.Add("[CurrentStatus] = @Status");
        if (submittedByUserId.HasValue) whereClauses.Add("[SubmittedByUserId] = @SubmittedByUserId");

        var whereSql = whereClauses.Count > 0 ? "WHERE " + string.Join(" AND ", whereClauses) : string.Empty;

        var countSql = $"SELECT COUNT(1) FROM [workflow].[WorkflowSubmissions] {whereSql};";
        var dataSql = $@"
SELECT 
    [Id], [WorkflowInstanceId], [WorkflowDefinitionId], [FormKey],
    [SubmittedByUserId], [RequesterOrgUnitId], [FormDataJson],
    [CurrentStatus], [Priority], [SubmittedAt], [CompletedAt]
FROM [workflow].[WorkflowSubmissions]
{whereSql}
ORDER BY [SubmittedAt] DESC
OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY;
";

        int totalCount = 0;
        await using (var countCmd = connection.CreateCommand())
        {
            countCmd.CommandText = countSql;
            if (!string.IsNullOrWhiteSpace(status)) AddParameter(countCmd, "@Status", status);
            if (submittedByUserId.HasValue) AddParameter(countCmd, "@SubmittedByUserId", submittedByUserId.Value);

            var scalar = await countCmd.ExecuteScalarAsync(cancellationToken);
            if (scalar != null && scalar != DBNull.Value) totalCount = Convert.ToInt32(scalar);
        }

        var items = new List<WorkflowSubmissionDto>();
        await using (var dataCmd = connection.CreateCommand())
        {
            dataCmd.CommandText = dataSql;
            AddParameter(dataCmd, "@Offset", offset);
            AddParameter(dataCmd, "@PageSize", pageSize);
            if (!string.IsNullOrWhiteSpace(status)) AddParameter(dataCmd, "@Status", status);
            if (submittedByUserId.HasValue) AddParameter(dataCmd, "@SubmittedByUserId", submittedByUserId.Value);

            await using var reader = await dataCmd.ExecuteReaderAsync(CommandBehavior.SequentialAccess, cancellationToken);
            while (await reader.ReadAsync(cancellationToken))
            {
                var id = reader.GetGuid(0);
                var wfInstanceId = reader.GetString(1);
                var wfDefId = reader.GetString(2);
                var formKey = reader.GetString(3);
                var submitter = reader.GetGuid(4);
                var orgUnit = reader.GetGuid(5);
                var formJson = reader.GetString(6);
                var curStatus = reader.GetString(7);
                var priority = reader.GetInt32(8);
                var submittedAt = reader.GetDateTime(9);
                var completedAt = reader.IsDBNull(10) ? (DateTime?)null : reader.GetDateTime(10);

                items.Add(new WorkflowSubmissionDto(
                    id, wfInstanceId, wfDefId, formKey, submitter,
                    orgUnit, formJson, curStatus, priority, submittedAt, completedAt
                ));
            }
        }

        return new PagedWorkflowResult<WorkflowSubmissionDto>(items, totalCount, page, pageSize);
    }

    private static void AddParameter(DbCommand cmd, string name, object value)
    {
        var p = cmd.CreateParameter();
        p.ParameterName = name;
        p.Value = value;
        cmd.Parameters.Add(p);
    }
}
