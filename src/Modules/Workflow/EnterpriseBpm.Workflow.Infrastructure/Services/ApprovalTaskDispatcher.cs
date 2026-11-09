using EnterpriseBpm.Org.Application.Contracts;
using EnterpriseBpm.Workflow.Application.Contracts;
using EnterpriseBpm.Workflow.Application.Events;
using EnterpriseBpm.Workflow.Domain.Entities;
using EnterpriseBpm.Workflow.Domain.Exceptions;
using EnterpriseBpm.Workflow.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace EnterpriseBpm.Workflow.Infrastructure.Services;

public class ApprovalTaskDispatcher : IApprovalTaskDispatcher
{
    private readonly WorkflowDbContext _dbContext;
    private readonly IOrgHierarchyService _orgHierarchyService;
    private readonly IDelegationResolver _delegationResolver;
    private readonly IMediator _mediator;
    private readonly ILogger<ApprovalTaskDispatcher> _logger;

    public ApprovalTaskDispatcher(
        WorkflowDbContext dbContext,
        IOrgHierarchyService orgHierarchyService,
        IDelegationResolver delegationResolver,
        IMediator mediator,
        ILogger<ApprovalTaskDispatcher> logger)
    {
        _dbContext = dbContext;
        _orgHierarchyService = orgHierarchyService;
        _delegationResolver = delegationResolver;
        _mediator = mediator;
        _logger = logger;
    }

    public async Task<Guid> DispatchApprovalTaskAsync(
        string workflowInstanceId,
        string activityId,
        string stepName,
        int stepOrder,
        string formKey,
        string? targetRole,
        int slaHours,
        bool allowDelegation,
        Guid? directAssigneeId = null,
        CancellationToken cancellationToken = default)
    {
        var submission = await _dbContext.WorkflowSubmissions
            .FirstOrDefaultAsync(s => s.WorkflowInstanceId == workflowInstanceId, cancellationToken)
            ?? throw new WorkflowSubmissionNotFoundException(workflowInstanceId);

        Guid configuredApproverId;

        if (directAssigneeId.HasValue && directAssigneeId.Value != Guid.Empty)
        {
            configuredApproverId = directAssigneeId.Value;
        }
        else if (!string.IsNullOrWhiteSpace(targetRole))
        {
            var candidates = await _orgHierarchyService.GetApproversByRoleAsync(
                targetRole, 
                submission.RequesterOrgUnitId, 
                cancellationToken);

            configuredApproverId = candidates.FirstOrDefault();
            if (configuredApproverId == Guid.Empty)
            {
                // Fallback: Tìm Quản lý trực tiếp của người nộp phiếu
                var managerId = await _orgHierarchyService.GetDirectManagerIdAsync(submission.SubmittedByUserId, cancellationToken);
                configuredApproverId = managerId ?? throw new InvalidWorkflowOperationException(
                    $"Không thể phân giải nhân sự duyệt cho vai trò '{targetRole}' tại đơn vị {submission.RequesterOrgUnitId}");
            }
        }
        else
        {
            var managerId = await _orgHierarchyService.GetDirectManagerIdAsync(submission.SubmittedByUserId, cancellationToken);
            configuredApproverId = managerId ?? throw new InvalidWorkflowOperationException(
                "Không có cấu hình người duyệt trực tiếp hoặc vai trò và không tìm thấy quản lý trực tiếp.");
        }

        Guid actualApproverId = configuredApproverId;
        Guid? delegatedFromUserId = null;

        if (allowDelegation)
        {
            actualApproverId = await _delegationResolver.ResolveActualApproverAsync(
                configuredApproverId,
                formKey,
                cancellationToken);

            if (actualApproverId != configuredApproverId)
            {
                delegatedFromUserId = configuredApproverId;
                _logger.LogInformation(
                    "Phát hiện ủy quyền phê duyệt: Nhiệm vụ tại Instance {InstanceId} được chuyển từ {FromUser} sang {ToUser}",
                    workflowInstanceId, configuredApproverId, actualApproverId);
            }
        }

        var dueDate = DateTime.UtcNow.AddHours(slaHours > 0 ? slaHours : 24);

        var task = ApprovalTask.Create(
            workflowInstanceId: workflowInstanceId,
            activityId: activityId,
            assignedToUserId: actualApproverId,
            targetRole: targetRole,
            delegatedFromUserId: delegatedFromUserId,
            dueDate: dueDate);

        // Đính kèm Domain Event vào Task (Transactional Outbox Pattern - TSK-801)
        task.AddDomainEvent(new TaskAssignedEvent(
            TaskId: task.Id,
            WorkflowInstanceId: workflowInstanceId,
            FormKey: formKey,
            AssignedToUserId: actualApproverId,
            DueDate: dueDate
        ));

        await _dbContext.ApprovalTasks.AddAsync(task, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);

        // Vẫn gọi publish in-memory để phản hồi tức thì nếu worker chưa chạy
        try
        {
            await _mediator.Publish(new TaskAssignedEvent(
                TaskId: task.Id,
                WorkflowInstanceId: workflowInstanceId,
                FormKey: formKey,
                AssignedToUserId: actualApproverId,
                DueDate: dueDate
            ), cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Lỗi publish in-memory, sự kiện đã được lưu an toàn trong Transactional Outbox.");
        }

        _logger.LogInformation(
            "Đã phân công nhiệm vụ {TaskId} cho nhân sự {UserId} với hạn xử lý {DueDate}",
            task.Id, actualApproverId, dueDate);

        return task.Id;
    }
}

