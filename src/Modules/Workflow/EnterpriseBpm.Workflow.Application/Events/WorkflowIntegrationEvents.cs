using EnterpriseBpm.EventBus.Events;

namespace EnterpriseBpm.Workflow.Application.Events;

/// <summary>
/// Phát khi một nhiệm vụ phê duyệt mới được phân công cho nhân sự.
/// </summary>
public record TaskAssignedEvent(
    Guid TaskId,
    string WorkflowInstanceId,
    string FormKey,
    Guid AssignedToUserId,
    DateTime DueDate
) : BaseIntegrationEvent;

/// <summary>
/// Phát khi một nhiệm vụ phê duyệt bị trễ hạn SLA.
/// </summary>
public record TaskSlaBreachedEvent(
    Guid TaskId,
    string WorkflowInstanceId,
    string FormKey,
    Guid RequesterOrgUnitId,
    Guid AssignedToUserId,
    DateTime DueDate
) : BaseIntegrationEvent;

/// <summary>
/// Phát khi quy trình được phê duyệt thành công toàn trình.
/// </summary>
public record WorkflowApprovedEvent(
    string WorkflowInstanceId,
    string FormKey,
    Guid SubmittedByUserId,
    Guid ApprovedByUserId,
    DateTime ApprovedAt
) : BaseIntegrationEvent;

/// <summary>
/// Phát khi quy trình bị người duyệt từ chối.
/// </summary>
public record WorkflowRejectedEvent(
    string WorkflowInstanceId,
    string FormKey,
    Guid SubmittedByUserId,
    Guid RejectedByUserId,
    string RejectedByName,
    string? Comment,
    DateTime RejectedAt
) : BaseIntegrationEvent;

/// <summary>
/// Phát khi người nộp đơn hoặc quản trị viên hủy phiếu trình ký.
/// </summary>
public record WorkflowCancelledEvent(
    string WorkflowInstanceId,
    string FormKey,
    Guid CancelledByUserId,
    string CancelledByName,
    string Reason,
    IReadOnlyList<Guid> AffectedUserIds
) : BaseIntegrationEvent;

/// <summary>
/// Phát khi quy trình kết thúc (Approved, Rejected hoặc Cancelled) phục vụ Module Analytics / Audit.
/// </summary>
public record WorkflowCompletedEvent(
    string WorkflowInstanceId,
    string FormKey,
    Guid RequesterOrgUnitId,
    string FinalStatus,
    DateTime SubmittedAt,
    DateTime CompletedAt
) : BaseIntegrationEvent;

