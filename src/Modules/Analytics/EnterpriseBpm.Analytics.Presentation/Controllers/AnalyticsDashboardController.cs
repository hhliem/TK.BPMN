using EnterpriseBpm.Analytics.Application.Contracts;
using EnterpriseBpm.Analytics.Application.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EnterpriseBpm.Analytics.Presentation.Controllers;

[ApiController]
[Route("api/analytics")]
[Authorize]
public class AnalyticsDashboardController : ControllerBase
{
    private readonly IExecutiveDashboardService _dashboardService;
    private readonly IBottleneckAnalysisService _bottleneckService;

    public AnalyticsDashboardController(
        IExecutiveDashboardService dashboardService,
        IBottleneckAnalysisService bottleneckService)
    {
        _dashboardService = dashboardService;
        _bottleneckService = bottleneckService;
    }

    [HttpGet("dashboard")]
    public async Task<ActionResult<ExecutiveDashboardMetricsDto>> GetDashboardMetrics(CancellationToken cancellationToken)
    {
        var result = await _dashboardService.GetDashboardMetricsAsync(cancellationToken);
        return Ok(result);
    }

    [HttpGet("bottlenecks")]
    public async Task<ActionResult<IReadOnlyList<StepSlaAnalyticsDto>>> GetBottlenecks([FromQuery] string? workflowDefinitionId, CancellationToken cancellationToken)
    {
        var result = await _bottleneckService.GetBottlenecksAsync(workflowDefinitionId, cancellationToken);
        return Ok(result);
    }

    [HttpPost("bottlenecks/recalculate")]
    public async Task<ActionResult<IReadOnlyList<StepSlaAnalyticsDto>>> RecalculateBottlenecks(CancellationToken cancellationToken)
    {
        var result = await _bottleneckService.AnalyzeAndRecalculateBottlenecksAsync(cancellationToken);
        return Ok(result);
    }
}

