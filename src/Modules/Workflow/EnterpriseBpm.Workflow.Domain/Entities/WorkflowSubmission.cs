using EnterpriseBpm.SharedKernel;
using EnterpriseBpm.SharedKernel.Contracts;
using EnterpriseBpm.Workflow.Domain.Enums;
using EnterpriseBpm.Workflow.Domain.Exceptions;

namespace EnterpriseBpm.Workflow.Domain.Entities;

public class WorkflowSubmission : BaseEntity<Guid>, IAuditable, ISoftDeletable, IHasDomainEvents
{
    public string WorkflowInstanceId { get; private set; } = default!;
    public string WorkflowDefinitionId { get; private set; } = default!;
    public string FormKey { get; private set; } = default!;
    public Guid SubmittedByUserId { get; private set; }
    public Guid RequesterOrgUnitId { get; private set; }
    public string FormDataJson { get; private set; } = default!;
    public WorkflowStatus CurrentStatus { get; private set; } = WorkflowStatus.Draft;
    public WorkflowPriority Priority { get; private set; } = WorkflowPriority.Normal;
    public DateTime SubmittedAt { get; private set; } = DateTime.UtcNow;
    public DateTime? CompletedAt { get; private set; }

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

    // IAuditable
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public Guid? CreatedBy { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public Guid? UpdatedBy { get; set; }

    // ISoftDeletable
    public DateTime? DeletedAt { get; set; }
    public Guid? DeletedBy { get; set; }

    // Navigation
    private readonly List<ApprovalTask> _tasks = new();
    public IReadOnlyCollection<ApprovalTask> Tasks => _tasks.AsReadOnly();

    protected WorkflowSubmission() { }

    public static WorkflowSubmission Create(
        string workflowInstanceId,
        string workflowDefinitionId,
        string formKey,
        Guid submittedByUserId,
        Guid requesterOrgUnitId,
        string formDataJson,
        WorkflowPriority priority = WorkflowPriority.Normal)
    {
        if (string.IsNullOrWhiteSpace(workflowInstanceId))
            throw new ArgumentException("WorkflowInstanceId không được để trống.", nameof(workflowInstanceId));
        if (string.IsNullOrWhiteSpace(workflowDefinitionId))
            throw new ArgumentException("WorkflowDefinitionId không được để trống.", nameof(workflowDefinitionId));
        if (string.IsNullOrWhiteSpace(formKey))
            throw new ArgumentException("FormKey không được để trống.", nameof(formKey));

        return new WorkflowSubmission
        {
            Id = Guid.NewGuid(),
            WorkflowInstanceId = workflowInstanceId,
            WorkflowDefinitionId = workflowDefinitionId,
            FormKey = formKey,
            SubmittedByUserId = submittedByUserId,
            RequesterOrgUnitId = requesterOrgUnitId,
            FormDataJson = formDataJson,
            CurrentStatus = WorkflowStatus.InProgress,
            Priority = priority,
            SubmittedAt = DateTime.UtcNow
        };
    }

    public void MarkApproved()
    {
        if (CurrentStatus == WorkflowStatus.Cancelled)
            throw new InvalidWorkflowOperationException("Không thể phê duyệt phiếu đã bị hủy.");

        CurrentStatus = WorkflowStatus.Approved;
        CompletedAt = DateTime.UtcNow;
    }

    public void MarkRejected()
    {
        if (CurrentStatus == WorkflowStatus.Cancelled)
            throw new InvalidWorkflowOperationException("Không thể từ chối phiếu đã bị hủy.");

        CurrentStatus = WorkflowStatus.Rejected;
        CompletedAt = DateTime.UtcNow;
    }

    public void Cancel(Guid cancelledByUserId)
    {
        if (CurrentStatus == WorkflowStatus.Approved || CurrentStatus == WorkflowStatus.Rejected)
            throw new InvalidWorkflowOperationException("Không thể hủy phiếu đã hoàn tất.");

        CurrentStatus = WorkflowStatus.Cancelled;
        CompletedAt = DateTime.UtcNow;
        UpdatedBy = cancelledByUserId;
        UpdatedAt = DateTime.UtcNow;
    }

    public void UpdateFormData(string newFormDataJson)
    {
        if (CurrentStatus != WorkflowStatus.Draft && CurrentStatus != WorkflowStatus.InProgress)
            throw new InvalidWorkflowOperationException("Chỉ có thể cập nhật dữ liệu khi phiếu đang mở.");

        FormDataJson = newFormDataJson;
        UpdatedAt = DateTime.UtcNow;
    }

    public void SetPriority(WorkflowPriority newPriority)
    {
        Priority = newPriority;
        UpdatedAt = DateTime.UtcNow;
    }
}

