using EnterpriseBpm.Workflow.Application.Contracts;
using EnterpriseBpm.Workflow.Application.DTOs;
using EnterpriseBpm.Workflow.Application.Events;
using EnterpriseBpm.Workflow.Domain.Entities;
using EnterpriseBpm.Workflow.Domain.Enums;
using EnterpriseBpm.Workflow.Domain.Exceptions;
using EnterpriseBpm.Workflow.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace EnterpriseBpm.Workflow.Infrastructure.Services;

public class WorkflowRuntimeEngine : IWorkflowRuntimeEngine
{
    private readonly WorkflowDbContext _dbContext;
    private readonly IMediator _mediator;
    private readonly ILogger<WorkflowRuntimeEngine> _logger;

    public WorkflowRuntimeEngine(
        WorkflowDbContext dbContext,
        IMediator mediator,
        ILogger<WorkflowRuntimeEngine> logger)
    {
        _dbContext = dbContext;
        _mediator = mediator;
        _logger = logger;
    }

    public async Task<string> StartWorkflowAsync(
        string workflowDefinitionId,
        IDictionary<string, object> input,
        CancellationToken cancellationToken = default)
    {
        var workflowInstanceId = $"wf_{Guid.NewGuid():N}";
        _logger.LogInformation(
            "Khởi tạo luồng quy trình thực thi mới: InstanceId={InstanceId}, DefinitionId={DefinitionId}",
            workflowInstanceId, workflowDefinitionId);

        return await Task.FromResult(workflowInstanceId);
    }

    public async Task ResumeBookmarkAsync(
        string workflowInstanceId,
        string bookmarkName,
        ApprovalResultPayload payload,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "Đang resume Bookmark {BookmarkName} cho Instance {InstanceId} với quyết định {Decision}",
            bookmarkName, workflowInstanceId, payload.Decision);

        var submission = await _dbContext.WorkflowSubmissions
            .FirstOrDefaultAsync(s => s.WorkflowInstanceId == workflowInstanceId, cancellationToken)
            ?? throw new WorkflowSubmissionNotFoundException(workflowInstanceId);

        var decision = Enum.Parse<ApprovalDecision>(payload.Decision);

        if (decision == ApprovalDecision.Approved)
        {
            // Kiểm tra xem còn pending tasks nào khác trong instance không
            var hasOtherPendingTasks = await _dbContext.ApprovalTasks
                .AnyAsync(t => t.WorkflowInstanceId == workflowInstanceId && t.Status == ApprovalTaskStatus.Pending, cancellationToken);

            if (!hasOtherPendingTasks)
            {
                submission.MarkApproved();
                await _dbContext.SaveChangesAsync(cancellationToken);

                await _mediator.Publish(new WorkflowApprovedEvent(
                    WorkflowInstanceId: workflowInstanceId,
                    FormKey: submission.FormKey,
                    SubmittedByUserId: submission.SubmittedByUserId,
                    ApprovedByUserId: payload.ActorUserId,
                    ApprovedAt: DateTime.UtcNow
                ), cancellationToken);

                await _mediator.Publish(new WorkflowCompletedEvent(
                    WorkflowInstanceId: workflowInstanceId,
                    FormKey: submission.FormKey,
                    RequesterOrgUnitId: submission.RequesterOrgUnitId,
                    FinalStatus: "Approved",
                    SubmittedAt: submission.SubmittedAt,
                    CompletedAt: DateTime.UtcNow
                ), cancellationToken);

                _logger.LogInformation("Quy trình Instance {InstanceId} đã được phê duyệt hoàn tất toàn trình.", workflowInstanceId);
            }
        }
        else if (decision == ApprovalDecision.Rejected)
        {
            submission.MarkRejected();

            // Hủy toàn bộ các tasks còn lại nếu quy trình bị từ chối
            var otherPendingTasks = await _dbContext.ApprovalTasks
                .Where(t => t.WorkflowInstanceId == workflowInstanceId && t.Status == ApprovalTaskStatus.Pending)
                .ToListAsync(cancellationToken);

            foreach (var t in otherPendingTasks)
            {
                t.Cancel();
            }

            await _dbContext.SaveChangesAsync(cancellationToken);

            await _mediator.Publish(new WorkflowRejectedEvent(
                WorkflowInstanceId: workflowInstanceId,
                FormKey: submission.FormKey,
                SubmittedByUserId: submission.SubmittedByUserId,
                RejectedByUserId: payload.ActorUserId,
                RejectedByName: $"User_{payload.ActorUserId}",
                Comment: payload.Comment,
                RejectedAt: DateTime.UtcNow
            ), cancellationToken);

            await _mediator.Publish(new WorkflowCompletedEvent(
                WorkflowInstanceId: workflowInstanceId,
                FormKey: submission.FormKey,
                RequesterOrgUnitId: submission.RequesterOrgUnitId,
                FinalStatus: "Rejected",
                SubmittedAt: submission.SubmittedAt,
                CompletedAt: DateTime.UtcNow
            ), cancellationToken);

            _logger.LogWarning("Quy trình Instance {InstanceId} đã bị người duyệt từ chối kết thúc luồng.", workflowInstanceId);
        }
    }
}

