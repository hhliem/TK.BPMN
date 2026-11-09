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

public class WorkflowSubmissionService : IWorkflowSubmissionService
{
    private readonly WorkflowDbContext _dbContext;
    private readonly IWorkflowRuntimeEngine _runtimeEngine;
    private readonly IApprovalTaskDispatcher _taskDispatcher;
    private readonly IMediator _mediator;
    private readonly ILogger<WorkflowSubmissionService> _logger;

    public WorkflowSubmissionService(
        WorkflowDbContext dbContext,
        IWorkflowRuntimeEngine runtimeEngine,
        IApprovalTaskDispatcher taskDispatcher,
        IMediator mediator,
        ILogger<WorkflowSubmissionService> logger)
    {
        _dbContext = dbContext;
        _runtimeEngine = runtimeEngine;
        _taskDispatcher = taskDispatcher;
        _mediator = mediator;
        _logger = logger;
    }

    public async Task<WorkflowSubmissionDto> SubmitWorkflowAsync(
        CreateWorkflowSubmissionRequest request,
        Guid submittedByUserId,
        CancellationToken cancellationToken = default)
    {
        // 1. Khởi động Workflow Engine
        var inputParams = new Dictionary<string, object>
        {
            { "FormKey", request.FormKey },
            { "SubmittedByUserId", submittedByUserId },
            { "RequesterOrgUnitId", request.RequesterOrgUnitId },
            { "FormData", request.FormDataJson }
        };

        var instanceId = await _runtimeEngine.StartWorkflowAsync(
            request.WorkflowDefinitionId,
            inputParams,
            cancellationToken);

        // 2. Tạo bản ghi Submission trong Schema [workflow]
        var submission = WorkflowSubmission.Create(
            workflowInstanceId: instanceId,
            workflowDefinitionId: request.WorkflowDefinitionId,
            formKey: request.FormKey,
            submittedByUserId: submittedByUserId,
            requesterOrgUnitId: request.RequesterOrgUnitId,
            formDataJson: request.FormDataJson,
            priority: request.Priority);

        await _dbContext.WorkflowSubmissions.AddAsync(submission, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);

        // 3. Tự động khởi tạo bước duyệt đầu tiên
        await _taskDispatcher.DispatchApprovalTaskAsync(
            workflowInstanceId: instanceId,
            activityId: "InitialApprovalNode",
            stepName: "Phê duyệt bước đầu",
            stepOrder: 1,
            formKey: request.FormKey,
            targetRole: "LineManager",
            slaHours: 24,
            allowDelegation: true,
            cancellationToken: cancellationToken);

        _logger.LogInformation(
            "Đã nộp thành công phiếu trình ký {SubmissionId} với Instance {InstanceId}",
            submission.Id, instanceId);

        return MapToDto(submission);
    }

    public async Task<WorkflowSubmissionDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var submission = await _dbContext.WorkflowSubmissions
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.Id == id, cancellationToken);

        return submission != null ? MapToDto(submission) : null;
    }

    public async Task<WorkflowSubmissionDto?> GetByInstanceIdAsync(string workflowInstanceId, CancellationToken cancellationToken = default)
    {
        var submission = await _dbContext.WorkflowSubmissions
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.WorkflowInstanceId == workflowInstanceId, cancellationToken);

        return submission != null ? MapToDto(submission) : null;
    }

    public async Task CancelSubmissionAsync(
        Guid submissionId,
        Guid cancelledByUserId,
        string reason,
        CancellationToken cancellationToken = default)
    {
        var submission = await _dbContext.WorkflowSubmissions
            .FirstOrDefaultAsync(s => s.Id == submissionId, cancellationToken)
            ?? throw new WorkflowSubmissionNotFoundException(submissionId.ToString());

        if (submission.SubmittedByUserId != cancelledByUserId)
        {
            throw new InvalidWorkflowOperationException("Chỉ người nộp đơn mới có quyền hủy phiếu trình ký.");
        }

        submission.Cancel(cancelledByUserId);

        var pendingTasks = await _dbContext.ApprovalTasks
            .Where(t => t.WorkflowInstanceId == submission.WorkflowInstanceId && t.Status == ApprovalTaskStatus.Pending)
            .ToListAsync(cancellationToken);

        var affectedUserIds = pendingTasks.Select(t => t.AssignedToUserId).Distinct().ToList();

        foreach (var task in pendingTasks)
        {
            task.Cancel();
        }

        await _dbContext.SaveChangesAsync(cancellationToken);

        await _mediator.Publish(new WorkflowCancelledEvent(
            WorkflowInstanceId: submission.WorkflowInstanceId,
            FormKey: submission.FormKey,
            CancelledByUserId: cancelledByUserId,
            CancelledByName: $"User_{cancelledByUserId}",
            Reason: reason,
            AffectedUserIds: affectedUserIds
        ), cancellationToken);

        await _mediator.Publish(new WorkflowCompletedEvent(
            WorkflowInstanceId: submission.WorkflowInstanceId,
            FormKey: submission.FormKey,
            RequesterOrgUnitId: submission.RequesterOrgUnitId,
            FinalStatus: "Cancelled",
            SubmittedAt: submission.SubmittedAt,
            CompletedAt: DateTime.UtcNow
        ), cancellationToken);

        _logger.LogInformation("Phiếu trình ký {SubmissionId} đã được hủy bỏ thành công.", submissionId);
    }

    private static WorkflowSubmissionDto MapToDto(WorkflowSubmission s) =>
        new(
            Id: s.Id,
            WorkflowInstanceId: s.WorkflowInstanceId,
            WorkflowDefinitionId: s.WorkflowDefinitionId,
            FormKey: s.FormKey,
            SubmittedByUserId: s.SubmittedByUserId,
            RequesterOrgUnitId: s.RequesterOrgUnitId,
            FormDataJson: s.FormDataJson,
            CurrentStatus: s.CurrentStatus.ToString(),
            Priority: (int)s.Priority,
            SubmittedAt: s.SubmittedAt,
            CompletedAt: s.CompletedAt
        );
}

