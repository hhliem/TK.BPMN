using Elsa.Workflows;
using Elsa.Workflows.Attributes;
using Elsa.Workflows.Models;
using EnterpriseBpm.SharedKernel.WorkflowModels;
using Microsoft.Extensions.DependencyInjection;

namespace EnterpriseBpm.Workflow.Application.Activities;

[Activity(
    Namespace = "EnterpriseBpm.Workflow",
    Category = "Trình Ký Doanh Nghiệp",
    DisplayName = "Phê duyệt Form Trình Ký",
    Description = "Tạo task chờ duyệt và Bookmark workflow",
    Kind = ActivityKind.Task)]
public class ApproveFormActivity : Activity<string>
{
    [Input(Description = "Mã FormKey của phiếu trình ký")]
    public Input<string> FormKey { get; set; } = default!;

    [Input(Description = "Vai trò xử lý: HeadOfDept, CFO, CEO")]
    public Input<string> TargetRole { get; set; } = default!;

    [Input(Description = "Cho phép ủy quyền")]
    public Input<bool> AllowDelegation { get; set; } = true;

    protected override async ValueTask ExecuteAsync(ActivityExecutionContext context)
    {
        var formKey = FormKey.Get(context);
        var targetRole = TargetRole.Get(context);
        var allowDelegation = AllowDelegation.Get(context);
        var instanceId = context.WorkflowExecutionContext.Id;

        var taskService = context.GetRequiredService<IApprovalTaskService>();
        await taskService.CreateApprovalTaskAsync(instanceId, formKey, targetRole, allowDelegation, context.CancellationToken);

        // Đặt trạng thái Bookmark chờ người dùng thao tác
        context.CreateBookmark(
            bookmarkName: "ResumeApprovalWorkflow",
            callback: OnResumedAsync,
            autoResume: true
        );
    }

    private async ValueTask OnResumedAsync(ActivityExecutionContext context)
    {
        var payload = context.GetInput<ApprovalResultPayload>("ApprovalResult");
        if (payload == null)
            throw new InvalidOperationException("Thiếu payload phê duyệt hợp lệ.");

        context.SetResult(payload.Decision);
        await context.CompleteActivityWithOutcomesAsync(payload.Decision);
    }
}
