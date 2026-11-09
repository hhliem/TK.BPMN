using EnterpriseBpm.Workflow.Domain.Enums;

namespace EnterpriseBpm.Workflow.Application.DTOs;

public record CreateWorkflowSubmissionRequest(
    string WorkflowDefinitionId,
    string FormKey,
    Guid RequesterOrgUnitId,
    string FormDataJson,
    WorkflowPriority Priority = WorkflowPriority.Normal
);

public record WorkflowSubmissionDto(
    Guid Id,
    string WorkflowInstanceId,
    string WorkflowDefinitionId,
    string FormKey,
    Guid SubmittedByUserId,
    Guid RequesterOrgUnitId,
    string FormDataJson,
    string CurrentStatus,
    int Priority,
    DateTime SubmittedAt,
    DateTime? CompletedAt
);

public record ApprovalTaskDto(
    Guid Id,
    string WorkflowInstanceId,
    string ActivityId,
    string? TargetRole,
    Guid AssignedToUserId,
    Guid? DelegatedFromUserId,
    string Status,
    string? Decision,
    string? Comment,
    DateTime? DueDate,
    bool IsEscalated,
    DateTime? CompletedAt,
    DateTime CreatedAt
);

public record PendingTaskFilterDto(
    string? Status = "Pending",
    int Page = 1,
    int PageSize = 20
);

public record SubmitApprovalDecisionRequest(
    string Decision, // Approved, Rejected, RequestChanges, Forwarded
    string? Comment
);

public record ApprovalResultPayload(
    Guid ActorUserId,
    Guid? OnBehalfOfUserId,
    Guid? DelegationId,
    string Decision,
    string? Comment,
    string DigitalSignatureHash
);

public record ApprovalHistoryDto(
    Guid Id,
    string WorkflowInstanceId,
    int StepOrder,
    string StepName,
    Guid ActorUserId,
    Guid? OnBehalfOfUserId,
    Guid? DelegationId,
    string Decision,
    string? Comment,
    DateTime ActionTimestamp,
    string DigitalSignatureHash,
    bool IsValidSignature
);

public record PagedWorkflowResult<T>(
    IReadOnlyList<T> Items,
    int TotalCount,
    int Page,
    int PageSize
);

