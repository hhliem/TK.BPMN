using EnterpriseBpm.Workflow.Application.Contracts;
using EnterpriseBpm.Workflow.Application.DTOs;
using EnterpriseBpm.Workflow.Domain.Entities;
using EnterpriseBpm.Workflow.Domain.Enums;
using EnterpriseBpm.Workflow.Domain.Exceptions;
using EnterpriseBpm.Workflow.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace EnterpriseBpm.Workflow.Infrastructure.Services;

public class ApprovalTaskService : IApprovalTaskService
{
    private readonly WorkflowDbContext _dbContext;
    private readonly IDistributedLockService _lockService;
    private readonly IApprovalHistoryService _historyService;
    private readonly IWorkflowRuntimeEngine _runtimeEngine;
    private readonly ILogger<ApprovalTaskService> _logger;

    public ApprovalTaskService(
        WorkflowDbContext dbContext,
        IDistributedLockService lockService,
        IApprovalHistoryService historyService,
        IWorkflowRuntimeEngine runtimeEngine,
        ILogger<ApprovalTaskService> logger)
    {
        _dbContext = dbContext;
        _lockService = lockService;
        _historyService = historyService;
        _runtimeEngine = runtimeEngine;
        _logger = logger;
    }

    public async Task<PagedWorkflowResult<ApprovalTaskDto>> GetTasksForUserAsync(
        Guid userId,
        PendingTaskFilterDto filter,
        CancellationToken cancellationToken = default)
    {
        var query = _dbContext.ApprovalTasks
            .AsNoTracking()
            .Where(t => t.AssignedToUserId == userId);

        if (!string.IsNullOrWhiteSpace(filter.Status))
        {
            if (Enum.TryParse<ApprovalTaskStatus>(filter.Status, true, out var statusEnum))
            {
                query = query.Where(t => t.Status == statusEnum);
            }
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderByDescending(t => t.DueDate.HasValue)
            .ThenBy(t => t.DueDate)
            .ThenByDescending(t => t.CreatedAt)
            .Skip((filter.Page - 1) * filter.PageSize)
            .Take(filter.PageSize)
            .Select(t => new ApprovalTaskDto(
                t.Id,
                t.WorkflowInstanceId,
                t.ActivityId,
                t.TargetRole,
                t.AssignedToUserId,
                t.DelegatedFromUserId,
                t.Status.ToString(),
                t.Decision.HasValue ? t.Decision.Value.ToString() : null,
                t.Comment,
                t.DueDate,
                t.IsEscalated,
                t.CompletedAt,
                t.CreatedAt
            ))
            .ToListAsync(cancellationToken);

        return new PagedWorkflowResult<ApprovalTaskDto>(items, totalCount, filter.Page, filter.PageSize);
    }

    public async Task<ApprovalTaskDto?> GetTaskByIdAsync(Guid taskId, CancellationToken cancellationToken = default)
    {
        var task = await _dbContext.ApprovalTasks
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.Id == taskId, cancellationToken);

        if (task == null) return null;

        return new ApprovalTaskDto(
            task.Id,
            task.WorkflowInstanceId,
            task.ActivityId,
            task.TargetRole,
            task.AssignedToUserId,
            task.DelegatedFromUserId,
            task.Status.ToString(),
            task.Decision.HasValue ? task.Decision.Value.ToString() : null,
            task.Comment,
            task.DueDate,
            task.IsEscalated,
            task.CompletedAt,
            task.CreatedAt
        );
    }

    public async Task ProcessDecisionAsync(
        Guid taskId,
        SubmitApprovalDecisionRequest request,
        Guid currentUserId,
        CancellationToken cancellationToken = default)
    {
        var resourceLockKey = $"approval_task:{taskId}";
        
        // 1. Áp dụng Redis Distributed Lock để chống click đúp & race condition
        await using var lockHandle = await _lockService.AcquireLockAsync(
            resourceLockKey,
            TimeSpan.FromSeconds(5),
            cancellationToken);

        if (lockHandle == null)
        {
            throw new DistributedLockAcquisitionException(resourceLockKey);
        }

        // 2. Kiểm tra tính hợp lệ của Task
        var task = await _dbContext.ApprovalTasks
            .FirstOrDefaultAsync(t => t.Id == taskId, cancellationToken)
            ?? throw new ApprovalTaskNotFoundException(taskId);

        if (task.Status != ApprovalTaskStatus.Pending)
        {
            throw new TaskAlreadyProcessedException(taskId, task.Status.ToString());
        }

        if (task.AssignedToUserId != currentUserId)
        {
            throw new InvalidWorkflowOperationException("Người dùng không có thẩm quyền xử lý nhiệm vụ này.");
        }

        if (!Enum.TryParse<ApprovalDecision>(request.Decision, true, out var decisionEnum))
        {
            throw new InvalidWorkflowOperationException($"Quyết định phê duyệt '{request.Decision}' không hợp lệ.");
        }

        // 3. Sử dụng Database Transaction bảo đảm Atomic State Synchronization
        await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            // Cập nhật Task
            task.Complete(decisionEnum, request.Comment);
            await _dbContext.SaveChangesAsync(cancellationToken);

            // Ghi nhận lịch sử duyệt bất biến (Immutable Approval History)
            await _historyService.RecordHistoryAsync(
                workflowInstanceId: task.WorkflowInstanceId,
                stepOrder: 1, // Thứ tự bước
                stepName: task.TargetRole ?? "Phê duyệt",
                actorUserId: currentUserId,
                decision: decisionEnum,
                comment: request.Comment,
                onBehalfOfUserId: task.DelegatedFromUserId,
                delegationId: null,
                cancellationToken: cancellationToken);

            // Resume Bookmark của Elsa Workflow Runtime
            var payload = new ApprovalResultPayload(
                ActorUserId: currentUserId,
                OnBehalfOfUserId: task.DelegatedFromUserId,
                DelegationId: null,
                Decision: decisionEnum.ToString(),
                Comment: request.Comment,
                DigitalSignatureHash: string.Empty
            );

            await _runtimeEngine.ResumeBookmarkAsync(
                workflowInstanceId: task.WorkflowInstanceId,
                bookmarkName: $"Approval_{task.ActivityId}",
                payload: payload,
                cancellationToken: cancellationToken);

            await transaction.CommitAsync(cancellationToken);
            _logger.LogInformation("Nhiệm vụ {TaskId} đã được xử lý thành công với quyết định: {Decision}", taskId, decisionEnum);
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync(cancellationToken);
            _logger.LogError(ex, "Lỗi xảy ra trong quá trình xử lý quyết định cho nhiệm vụ {TaskId}. Đã rollback giao dịch.", taskId);
            throw;
        }
    }
}

