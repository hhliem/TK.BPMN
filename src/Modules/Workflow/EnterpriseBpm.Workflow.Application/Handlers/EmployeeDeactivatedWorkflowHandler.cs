using EnterpriseBpm.Org.Application.Contracts;
using EnterpriseBpm.Org.Application.Events;
using EnterpriseBpm.Workflow.Domain.Entities;
using EnterpriseBpm.Workflow.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace EnterpriseBpm.Workflow.Application.Handlers;

/// <summary>
/// Lắng nghe sự kiện EmployeeDeactivatedEvent từ Org module.
/// Tự động thu hồi hoặc điều chuyển các nhiệm vụ duyệt đang Pending của nhân sự đã nghỉ việc/vô hiệu hóa.
/// </summary>
public interface IWorkflowDbContextProvider
{
    DbSet<ApprovalTask> ApprovalTasks { get; }
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}

public class EmployeeDeactivatedWorkflowHandler : INotificationHandler<EmployeeDeactivatedEvent>
{
    private readonly IWorkflowDbContextProvider _dbContext;
    private readonly IOrgHierarchyService _orgService;
    private readonly ILogger<EmployeeDeactivatedWorkflowHandler> _logger;

    public EmployeeDeactivatedWorkflowHandler(
        IWorkflowDbContextProvider dbContext,
        IOrgHierarchyService orgService,
        ILogger<EmployeeDeactivatedWorkflowHandler> logger)
    {
        _dbContext = dbContext;
        _orgService = orgService;
        _logger = logger;
    }

    public async Task Handle(EmployeeDeactivatedEvent notification, CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Tiếp nhận sự kiện nhân sự {FullName} ({EmployeeCode}) bị vô hiệu hóa. Bắt đầu rà soát các nhiệm vụ phê duyệt đang Pending.",
            notification.FullName, notification.EmployeeCode);

        var pendingTasks = await _dbContext.ApprovalTasks
            .Where(t => t.AssignedToUserId == notification.EmployeeId && t.Status == ApprovalTaskStatus.Pending)
            .ToListAsync(cancellationToken);

        if (pendingTasks.Count == 0)
        {
            _logger.LogInformation("Không có nhiệm vụ Pending nào thuộc về nhân sự {EmployeeId}.", notification.EmployeeId);
            return;
        }

        var managerId = await _orgService.GetDirectManagerIdAsync(notification.EmployeeId, cancellationToken);
        var adminId = Guid.Parse("11111111-1111-1111-1111-111111111111"); // Workflow Admin fallback
        var targetApproverId = managerId ?? adminId;

        foreach (var task in pendingTasks)
        {
            var note = managerId.HasValue
                ? $"Nhân sự tiền nhiệm ({notification.FullName}) đã thôi việc. Tự động điều chuyển sang Quản lý trực tiếp."
                : $"Nhân sự tiền nhiệm ({notification.FullName}) đã thôi việc và không có quản lý trực tiếp. Tự động điều chuyển sang Quản trị viên quy trình (Workflow Admin).";

            task.Reassign(targetApproverId, note);
            _logger.LogWarning(
                "Nhiệm vụ {TaskId} đã được tự động điều chuyển từ nhân sự thôi việc {OldUser} sang {TargetApproverId} ({Reason})",
                task.Id, notification.EmployeeId, targetApproverId, note);
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}

