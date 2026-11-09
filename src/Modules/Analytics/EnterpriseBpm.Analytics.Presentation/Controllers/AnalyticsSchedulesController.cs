using EnterpriseBpm.Analytics.Application.Contracts;
using EnterpriseBpm.Analytics.Application.DTOs;
using EnterpriseBpm.SharedKernel.Contracts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EnterpriseBpm.Analytics.Presentation.Controllers;

[ApiController]
[Route("api/analytics/schedules")]
[Authorize]
public class AnalyticsSchedulesController : ControllerBase
{
    private readonly IReportSchedulerService _schedulerService;
    private readonly ICurrentUserService _currentUserService;

    public AnalyticsSchedulesController(
        IReportSchedulerService schedulerService,
        ICurrentUserService currentUserService)
    {
        _schedulerService = schedulerService;
        _currentUserService = currentUserService;
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<ScheduledReportSubscriptionDto>>> GetSubscriptions(CancellationToken cancellationToken)
    {
        var result = await _schedulerService.GetSubscriptionsAsync(cancellationToken);
        return Ok(result);
    }

    [HttpPost]
    public async Task<ActionResult<Guid>> CreateSubscription([FromBody] CreateSubscriptionRequestDto request, CancellationToken cancellationToken)
    {
        var userId = _currentUserService.UserId ?? Guid.Empty;
        var id = await _schedulerService.CreateSubscriptionAsync(request, userId, cancellationToken);
        return Ok(id);
    }

    [HttpPost("{id:guid}/trigger")]
    public async Task<IActionResult> TriggerSubscription([FromRoute] Guid id, CancellationToken cancellationToken)
    {
        await _schedulerService.ExecuteSubscriptionAsync(id, cancellationToken);
        return Ok(new { message = "Đã kích hoạt chạy và xuất báo cáo tự động thành công." });
    }
}

