using EnterpriseBpm.SharedKernel;
using EnterpriseBpm.SharedKernel.Contracts;
using EnterpriseBpm.Workflow.Domain.Enums;
using EnterpriseBpm.Workflow.Domain.Exceptions;

namespace EnterpriseBpm.Workflow.Domain.Entities;

public class ApprovalTask : BaseEntity<Guid>, IHasDomainEvents
{
    public string WorkflowInstanceId { get; private set; } = default!;
    public string ActivityId { get; private set; } = default!;
    public string? TargetRole { get; private set; }
    public Guid AssignedToUserId { get; private set; }
    public Guid? DelegatedFromUserId { get; private set; }
    public ApprovalTaskStatus Status { get; private set; } = ApprovalTaskStatus.Pending;
    public ApprovalDecision? Decision { get; private set; }
    public string? Comment { get; private set; }
    public DateTime? DueDate { get; private set; }
    public bool IsEscalated { get; private set; }
    public DateTime? CompletedAt { get; private set; }
    public DateTime CreatedAt { get; private set; } = DateTime.UtcNow;

    // Domain Events (Transactional Outbox Pattern - TSK-801)
    private readonly List<IDomainEvent> _domainEvents = new();
    public IReadOnlyList<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    public void AddDomainEvent(IDomainEvent domainEvent)
    {
        _domainEvents.Add(domainEvent);
    }

    public void ClearDomainEvents()
    {
        _domainEvents.Clear();
    }

    // Navigation
    public WorkflowSubmission? Submission { get; private set; }

    protected ApprovalTask() { }

    public static ApprovalTask Create(
        string workflowInstanceId,
        string activityId,
        Guid assignedToUserId,
        string? targetRole = null,
        Guid? delegatedFromUserId = null,
        DateTime? dueDate = null)
    {
        if (string.IsNullOrWhiteSpace(workflowInstanceId))
            throw new ArgumentException("WorkflowInstanceId không được để trống.", nameof(workflowInstanceId));
        if (string.IsNullOrWhiteSpace(activityId))
            throw new ArgumentException("ActivityId không được để trống.", nameof(activityId));

        return new ApprovalTask
        {
            Id = Guid.NewGuid(),
            WorkflowInstanceId = workflowInstanceId,
            ActivityId = activityId,
            AssignedToUserId = assignedToUserId,
            TargetRole = targetRole,
            DelegatedFromUserId = delegatedFromUserId,
            Status = ApprovalTaskStatus.Pending,
            DueDate = dueDate,
            IsEscalated = false,
            CreatedAt = DateTime.UtcNow
        };
    }

    public void Complete(ApprovalDecision decision, string? comment = null)
    {
        if (Status != ApprovalTaskStatus.Pending)
            throw new TaskAlreadyProcessedException(Id, Status.ToString());

        Decision = decision;
        Comment = comment;
        Status = ApprovalTaskStatus.Completed;
        CompletedAt = DateTime.UtcNow;
    }

    public void MarkEscalated()
    {
        IsEscalated = true;
    }

    public void Reassign(Guid newUserId, string? reason = null)
    {
        if (Status != ApprovalTaskStatus.Pending)
            throw new TaskAlreadyProcessedException(Id, Status.ToString());

        AssignedToUserId = newUserId;
        Comment = string.IsNullOrWhiteSpace(reason) 
            ? "Được điều chuyển nhân sự xử lý." 
            : $"Điều chuyển: {reason}";
    }

    public void Cancel()
    {
        if (Status != ApprovalTaskStatus.Pending)
            return;

        Status = ApprovalTaskStatus.Cancelled;
        CompletedAt = DateTime.UtcNow;
    }
}

