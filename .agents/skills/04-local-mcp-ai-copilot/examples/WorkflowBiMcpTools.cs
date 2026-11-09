using System.ComponentModel;
using EnterpriseBpm.Analytics.Application.Services;
using ModelContextProtocol.NET.Server.Attributes;

namespace EnterpriseBpm.AiMcp.Infrastructure.Tools;

public class WorkflowBiMcpTools
{
    private readonly IAnalyticsQueryService _analyticsService;
    private readonly IUserContextAccessor _userContext;

    public WorkflowBiMcpTools(IAnalyticsQueryService analyticsService, IUserContextAccessor userContext)
    {
        _analyticsService = analyticsService;
        _userContext = userContext;
    }

    [McpTool("get_pending_approvals_count")]
    [Description("Thống kê số lượng đơn trình ký đang chờ phê duyệt của một phòng ban hoặc toàn công ty")]
    public async Task<int> GetPendingApprovalsCountAsync(
        [Description("Mã phòng ban (nếu để trống sẽ xét theo quyền hạn người hỏi)")] string? departmentCode,
        CancellationToken ct)
    {
        var currentUser = _userContext.GetCurrentUser();

        // RBAC Enforcement: Trưởng phòng chỉ được xem đơn phòng mình
        if (!currentUser.Roles.Contains("CEO") && !currentUser.Roles.Contains("CFO"))
        {
            departmentCode = currentUser.DepartmentCode;
        }

        return await _analyticsService.CountPendingWorkflowsAsync(departmentCode, ct);
    }

    [McpTool("get_sla_delayed_workflows")]
    [Description("Liệt kê danh sách các đơn trình ký đang bị nghẽn quá hạn SLA")]
    public async Task<IEnumerable<DelayedWorkflowSummaryDto>> GetDelayedWorkflowsAsync(
        [Description("Ngưỡng số giờ quá hạn (mặc định 24h)")] int delayedThresholdHours = 24,
        CancellationToken ct = default)
    {
        var currentUser = _userContext.GetCurrentUser();
        return await _analyticsService.GetDelayedWorkflowsSummaryAsync(currentUser.OrgUnitId, delayedThresholdHours, ct);
    }
}
