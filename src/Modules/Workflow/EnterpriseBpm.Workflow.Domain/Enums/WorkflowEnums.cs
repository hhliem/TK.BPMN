namespace EnterpriseBpm.Workflow.Domain.Enums;

public enum WorkflowStatus
{
    Draft = 0,
    InProgress = 1,
    Approved = 2,
    Rejected = 3,
    Cancelled = 4
}

public enum WorkflowPriority
{
    Normal = 1,
    High = 2,
    Urgent = 3
}

public enum ApprovalTaskStatus
{
    Pending = 0,
    Completed = 1,
    Cancelled = 2,
    Reassigned = 3
}

public enum ApprovalDecision
{
    Approved = 1,
    Rejected = 2,
    RequestChanges = 3,
    Forwarded = 4,
    Cancelled = 5
}

