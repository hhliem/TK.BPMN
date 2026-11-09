namespace EnterpriseBpm.Workflow.Domain.Exceptions;

public class WorkflowSubmissionNotFoundException : Exception
{
    public WorkflowSubmissionNotFoundException(string identifier)
        : base($"Không tìm thấy phiếu trình ký với định danh: {identifier}")
    {
    }
}

public class ApprovalTaskNotFoundException : Exception
{
    public ApprovalTaskNotFoundException(Guid taskId)
        : base($"Không tìm thấy nhiệm vụ phê duyệt với ID: {taskId}")
    {
    }
}

public class TaskAlreadyProcessedException : Exception
{
    public TaskAlreadyProcessedException(Guid taskId, string currentStatus)
        : base($"Nhiệm vụ {taskId} đã được xử lý trước đó với trạng thái: {currentStatus}. Không thể thực hiện lại.")
    {
    }
}

public class InvalidWorkflowOperationException : Exception
{
    public InvalidWorkflowOperationException(string message)
        : base(message)
    {
    }
}

public class DistributedLockAcquisitionException : Exception
{
    public DistributedLockAcquisitionException(string resourceKey)
        : base($"Tài nguyên {resourceKey} hiện đang được xử lý bởi yêu cầu khác. Vui lòng không thao tác lặp lại.")
    {
    }
}

