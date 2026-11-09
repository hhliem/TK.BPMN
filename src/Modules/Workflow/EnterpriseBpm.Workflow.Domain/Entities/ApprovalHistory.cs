using EnterpriseBpm.SharedKernel;
using EnterpriseBpm.Workflow.Domain.Enums;

namespace EnterpriseBpm.Workflow.Domain.Entities;

/// <summary>
/// Thực thể lịch sử phê duyệt bất biến (Immutable Approval History).
/// Mỗi bản ghi đại diện cho 1 hành động duyệt/từ chối/ủy quyền và được bảo vệ bằng chữ ký số băm SHA-256.
/// Không cho phép sửa đổi hoặc xóa sau khi đã ghi nhận.
/// </summary>
public class ApprovalHistory : BaseEntity<Guid>
{
    public string WorkflowInstanceId { get; private set; } = default!;
    public int StepOrder { get; private set; }
    public string StepName { get; private set; } = default!;
    public Guid ActorUserId { get; private set; }
    public Guid? OnBehalfOfUserId { get; private set; }
    public Guid? DelegationId { get; private set; }
    public ApprovalDecision Decision { get; private set; }
    public string? Comment { get; private set; }
    public DateTime ActionTimestamp { get; private set; } = DateTime.UtcNow;
    public string DigitalSignatureHash { get; private set; } = default!;

    protected ApprovalHistory() { }

    public static ApprovalHistory Create(
        string workflowInstanceId,
        int stepOrder,
        string stepName,
        Guid actorUserId,
        ApprovalDecision decision,
        string digitalSignatureHash,
        string? comment = null,
        Guid? onBehalfOfUserId = null,
        Guid? delegationId = null,
        DateTime? actionTimestamp = null)
    {
        if (string.IsNullOrWhiteSpace(workflowInstanceId))
            throw new ArgumentException("WorkflowInstanceId không được để trống.", nameof(workflowInstanceId));
        if (string.IsNullOrWhiteSpace(stepName))
            throw new ArgumentException("StepName không được để trống.", nameof(stepName));
        if (string.IsNullOrWhiteSpace(digitalSignatureHash))
            throw new ArgumentException("DigitalSignatureHash không được để trống.", nameof(digitalSignatureHash));

        return new ApprovalHistory
        {
            Id = Guid.NewGuid(),
            WorkflowInstanceId = workflowInstanceId,
            StepOrder = stepOrder,
            StepName = stepName,
            ActorUserId = actorUserId,
            OnBehalfOfUserId = onBehalfOfUserId,
            DelegationId = delegationId,
            Decision = decision,
            Comment = comment,
            ActionTimestamp = actionTimestamp ?? DateTime.UtcNow,
            DigitalSignatureHash = digitalSignatureHash
        };
    }
}

