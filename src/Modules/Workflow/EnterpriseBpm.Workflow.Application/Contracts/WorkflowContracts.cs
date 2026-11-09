using EnterpriseBpm.Workflow.Application.DTOs;
using EnterpriseBpm.Workflow.Domain.Enums;

namespace EnterpriseBpm.Workflow.Application.Contracts;

public interface IWorkflowSubmissionService
{
    Task<WorkflowSubmissionDto> SubmitWorkflowAsync(
        CreateWorkflowSubmissionRequest request,
        Guid submittedByUserId,
        CancellationToken cancellationToken = default);

    Task<WorkflowSubmissionDto?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    Task<WorkflowSubmissionDto?> GetByInstanceIdAsync(
        string workflowInstanceId,
        CancellationToken cancellationToken = default);

    Task CancelSubmissionAsync(
        Guid submissionId,
        Guid cancelledByUserId,
        string reason,
        CancellationToken cancellationToken = default);
}

public interface IApprovalTaskService
{
    Task<PagedWorkflowResult<ApprovalTaskDto>> GetTasksForUserAsync(
        Guid userId,
        PendingTaskFilterDto filter,
        CancellationToken cancellationToken = default);

    Task<ApprovalTaskDto?> GetTaskByIdAsync(
        Guid taskId,
        CancellationToken cancellationToken = default);

    Task ProcessDecisionAsync(
        Guid taskId,
        SubmitApprovalDecisionRequest request,
        Guid currentUserId,
        CancellationToken cancellationToken = default);
}

public interface IApprovalTaskDispatcher
{
    Task<Guid> DispatchApprovalTaskAsync(
        string workflowInstanceId,
        string activityId,
        string stepName,
        int stepOrder,
        string formKey,
        string? targetRole,
        int slaHours,
        bool allowDelegation,
        Guid? directAssigneeId = null,
        CancellationToken cancellationToken = default);
}

public interface IApprovalHistoryService
{
    Task RecordHistoryAsync(
        string workflowInstanceId,
        int stepOrder,
        string stepName,
        Guid actorUserId,
        ApprovalDecision decision,
        string? comment,
        Guid? onBehalfOfUserId = null,
        Guid? delegationId = null,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ApprovalHistoryDto>> GetHistoryByInstanceAsync(
        string workflowInstanceId,
        CancellationToken cancellationToken = default);
}

public interface IDistributedLockService
{
    Task<IAsyncDisposable?> AcquireLockAsync(
        string resourceKey,
        TimeSpan timeout,
        CancellationToken cancellationToken = default);
}

public interface IWorkflowRuntimeEngine
{
    Task<string> StartWorkflowAsync(
        string workflowDefinitionId,
        IDictionary<string, object> input,
        CancellationToken cancellationToken = default);

    Task ResumeBookmarkAsync(
        string workflowInstanceId,
        string bookmarkName,
        ApprovalResultPayload payload,
        CancellationToken cancellationToken = default);
}

