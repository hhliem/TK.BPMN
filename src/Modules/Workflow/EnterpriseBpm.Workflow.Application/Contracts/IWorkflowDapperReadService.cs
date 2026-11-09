using EnterpriseBpm.Workflow.Application.DTOs;

namespace EnterpriseBpm.Workflow.Application.Contracts;

/// <summary>
/// Hợp đồng dịch vụ tối ưu hóa đường đọc CQRS qua Dapper / Direct ADO.NET (TSK-805)
/// Loại bỏ hoàn toàn Change Tracker overhead của EF Core, tối ưu hóa độ trễ đọc P99 < 30ms.
/// </summary>
public interface IWorkflowDapperReadService
{
    Task<PagedWorkflowResult<ApprovalTaskDto>> GetTasksForUserFastAsync(
        Guid userId,
        PendingTaskFilterDto filter,
        CancellationToken cancellationToken = default);

    Task<PagedWorkflowResult<WorkflowSubmissionDto>> GetSubmissionsFastAsync(
        int page,
        int pageSize,
        string? status = null,
        Guid? submittedByUserId = null,
        CancellationToken cancellationToken = default);
}

