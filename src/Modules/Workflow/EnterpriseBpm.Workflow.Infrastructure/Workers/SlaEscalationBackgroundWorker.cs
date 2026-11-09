using EnterpriseBpm.Workflow.Application.Events;
using EnterpriseBpm.Workflow.Domain.Enums;
using EnterpriseBpm.Workflow.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace EnterpriseBpm.Workflow.Infrastructure.Workers;

public class SlaEscalationBackgroundWorker : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<SlaEscalationBackgroundWorker> _logger;

    public SlaEscalationBackgroundWorker(
        IServiceProvider serviceProvider,
        ILogger<SlaEscalationBackgroundWorker> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Khởi động SLA Escalation Background Worker.");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessOverdueTasksAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi xảy ra trong quá trình quét và xử lý nhiệm vụ vi phạm SLA.");
            }

            // Quét định kỳ 15 phút (hoặc dừng khi có tín hiệu cancel)
            await Task.Delay(TimeSpan.FromMinutes(15), stoppingToken);
        }
    }

    public async Task<int> ProcessOverdueTasksAsync(CancellationToken cancellationToken = default)
    {
        using var scope = _serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<WorkflowDbContext>();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

        var now = DateTime.UtcNow;
        var overdueTasks = await dbContext.ApprovalTasks
            .Where(t => t.Status == ApprovalTaskStatus.Pending 
                     && !t.IsEscalated 
                     && t.DueDate.HasValue 
                     && t.DueDate.Value < now)
            .ToListAsync(cancellationToken);

        if (overdueTasks.Count > 0)
        {
            _logger.LogWarning("Phát hiện {Count} nhiệm vụ phê duyệt trễ hạn SLA. Bắt đầu kích hoạt quy trình leo thang.", overdueTasks.Count);

            foreach (var task in overdueTasks)
            {
                task.MarkEscalated();

                var submission = await dbContext.WorkflowSubmissions
                    .AsNoTracking()
                    .FirstOrDefaultAsync(s => s.WorkflowInstanceId == task.WorkflowInstanceId, cancellationToken);

                await mediator.Publish(new TaskSlaBreachedEvent(
                    TaskId: task.Id,
                    WorkflowInstanceId: task.WorkflowInstanceId,
                    FormKey: submission?.FormKey ?? "UNKNOWN",
                    RequesterOrgUnitId: submission?.RequesterOrgUnitId ?? Guid.Empty,
                    AssignedToUserId: task.AssignedToUserId,
                    DueDate: task.DueDate!.Value
                ), cancellationToken);

                _logger.LogWarning(
                    "Đã phát cảnh báo leo thang SLA cho Task {TaskId} (Instance: {InstanceId}, User: {UserId}, Hạn: {DueDate})",
                    task.Id, task.WorkflowInstanceId, task.AssignedToUserId, task.DueDate.Value);
            }

            await dbContext.SaveChangesAsync(cancellationToken);
        }

        return overdueTasks.Count;
    }
}

