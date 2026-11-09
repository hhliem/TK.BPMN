using EnterpriseBpm.Notification.Application.Contracts;
using EnterpriseBpm.Notification.Domain.Enums;
using EnterpriseBpm.Org.Application.Events;
using EnterpriseBpm.Workflow.Application.Events;
using MediatR;
using Microsoft.Extensions.Logging;

namespace EnterpriseBpm.Notification.Application.Handlers;

public class TaskAssignedNotificationHandler : INotificationHandler<TaskAssignedEvent>
{
    private readonly INotificationDispatcher _dispatcher;
    private readonly ILogger<TaskAssignedNotificationHandler> _logger;

    public TaskAssignedNotificationHandler(
        INotificationDispatcher dispatcher,
        ILogger<TaskAssignedNotificationHandler> logger)
    {
        _dispatcher = dispatcher;
        _logger = logger;
    }

    public async Task Handle(TaskAssignedEvent notification, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Xử lý thông báo phân công nhiệm vụ: TaskId {TaskId}, FormKey {FormKey}", notification.TaskId, notification.FormKey);

        await _dispatcher.EnqueueNotificationAsync(new NotificationDispatchItem(
            TemplateCode: "TPL_TASK_ASSIGNED",
            Channel: NotificationChannel.Email,
            RecipientEmail: $"user-{notification.AssignedToUserId:N}@enterprise.com",
            RecipientUserId: notification.AssignedToUserId,
            SourceEventId: notification.EventId,
            PayloadParameters: new Dictionary<string, object?>
            {
                { "TaskId", notification.TaskId },
                { "WorkflowInstanceId", notification.WorkflowInstanceId },
                { "FormKey", notification.FormKey },
                { "DueDate", notification.DueDate.ToString("dd/MM/yyyy HH:mm") }
            }
        ), cancellationToken);
    }
}

public class WorkflowApprovedNotificationHandler : INotificationHandler<WorkflowApprovedEvent>
{
    private readonly INotificationDispatcher _dispatcher;
    private readonly ILogger<WorkflowApprovedNotificationHandler> _logger;

    public WorkflowApprovedNotificationHandler(
        INotificationDispatcher dispatcher,
        ILogger<WorkflowApprovedNotificationHandler> logger)
    {
        _dispatcher = dispatcher;
        _logger = logger;
    }

    public async Task Handle(WorkflowApprovedEvent notification, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Xử lý thông báo phê duyệt hồ sơ: InstanceId {InstanceId}", notification.WorkflowInstanceId);

        await _dispatcher.EnqueueNotificationAsync(new NotificationDispatchItem(
            TemplateCode: "TPL_WORKFLOW_APPROVED",
            Channel: NotificationChannel.Email,
            RecipientEmail: $"user-{notification.SubmittedByUserId:N}@enterprise.com",
            RecipientUserId: notification.SubmittedByUserId,
            SourceEventId: notification.EventId,
            PayloadParameters: new Dictionary<string, object?>
            {
                { "WorkflowInstanceId", notification.WorkflowInstanceId },
                { "FormKey", notification.FormKey },
                { "ApprovedDate", notification.ApprovedAt.ToString("dd/MM/yyyy HH:mm") }
            }
        ), cancellationToken);
    }
}

public class WorkflowRejectedNotificationHandler : INotificationHandler<WorkflowRejectedEvent>
{
    private readonly INotificationDispatcher _dispatcher;

    public WorkflowRejectedNotificationHandler(INotificationDispatcher dispatcher)
    {
        _dispatcher = dispatcher;
    }

    public async Task Handle(WorkflowRejectedEvent notification, CancellationToken cancellationToken)
    {
        await _dispatcher.EnqueueNotificationAsync(new NotificationDispatchItem(
            TemplateCode: "TPL_WORKFLOW_REJECTED",
            Channel: NotificationChannel.Email,
            RecipientEmail: $"user-{notification.SubmittedByUserId:N}@enterprise.com",
            RecipientUserId: notification.SubmittedByUserId,
            SourceEventId: notification.EventId,
            PayloadParameters: new Dictionary<string, object?>
            {
                { "WorkflowInstanceId", notification.WorkflowInstanceId },
                { "FormKey", notification.FormKey },
                { "RejectedByName", notification.RejectedByName },
                { "RejectionReason", notification.Comment ?? "Không có lý do cụ thể" },
                { "RejectedDate", notification.RejectedAt.ToString("dd/MM/yyyy HH:mm") }
            }
        ), cancellationToken);
    }
}

public class WorkflowCancelledNotificationHandler : INotificationHandler<WorkflowCancelledEvent>
{
    private readonly INotificationDispatcher _dispatcher;

    public WorkflowCancelledNotificationHandler(INotificationDispatcher dispatcher)
    {
        _dispatcher = dispatcher;
    }

    public async Task Handle(WorkflowCancelledEvent notification, CancellationToken cancellationToken)
    {
        await _dispatcher.EnqueueNotificationAsync(new NotificationDispatchItem(
            TemplateCode: "TPL_WORKFLOW_CANCELLED",
            Channel: NotificationChannel.Email,
            RecipientEmail: $"user-{notification.CancelledByUserId:N}@enterprise.com",
            RecipientUserId: notification.CancelledByUserId,
            SourceEventId: notification.EventId,
            PayloadParameters: new Dictionary<string, object?>
            {
                { "WorkflowInstanceId", notification.WorkflowInstanceId },
                { "FormKey", notification.FormKey },
                { "CancelledByName", notification.CancelledByName },
                { "CancellationReason", notification.Reason }
            }
        ), cancellationToken);
    }
}

public class TaskSlaBreachedNotificationHandler : INotificationHandler<TaskSlaBreachedEvent>
{
    private readonly INotificationDispatcher _dispatcher;

    public TaskSlaBreachedNotificationHandler(INotificationDispatcher dispatcher)
    {
        _dispatcher = dispatcher;
    }

    public async Task Handle(TaskSlaBreachedEvent notification, CancellationToken cancellationToken)
    {
        await _dispatcher.EnqueueNotificationAsync(new NotificationDispatchItem(
            TemplateCode: "TPL_TASK_SLA_BREACHED",
            Channel: NotificationChannel.Email,
            RecipientEmail: $"user-{notification.AssignedToUserId:N}@enterprise.com",
            RecipientUserId: notification.AssignedToUserId,
            SourceEventId: notification.EventId,
            PayloadParameters: new Dictionary<string, object?>
            {
                { "TaskId", notification.TaskId },
                { "WorkflowInstanceId", notification.WorkflowInstanceId },
                { "FormKey", notification.FormKey },
                { "DueDate", notification.DueDate.ToString("dd/MM/yyyy HH:mm") }
            }
        ), cancellationToken);
    }
}

public class EmployeeDeactivatedNotificationHandler : INotificationHandler<EmployeeDeactivatedEvent>
{
    private readonly INotificationDispatcher _dispatcher;

    public EmployeeDeactivatedNotificationHandler(INotificationDispatcher dispatcher)
    {
        _dispatcher = dispatcher;
    }

    public async Task Handle(EmployeeDeactivatedEvent notification, CancellationToken cancellationToken)
    {
        await _dispatcher.EnqueueNotificationAsync(new NotificationDispatchItem(
            TemplateCode: "TPL_EMPLOYEE_DEACTIVATED",
            Channel: NotificationChannel.Email,
            RecipientEmail: "hr-notification@enterprise.com",
            SourceEventId: notification.EventId,
            PayloadParameters: new Dictionary<string, object?>
            {
                { "EmployeeCode", notification.EmployeeCode },
                { "EmployeeName", notification.FullName },
                { "DeactivationDate", notification.DeactivatedAt.ToString("dd/MM/yyyy HH:mm") }
            }
        ), cancellationToken);
    }
}

