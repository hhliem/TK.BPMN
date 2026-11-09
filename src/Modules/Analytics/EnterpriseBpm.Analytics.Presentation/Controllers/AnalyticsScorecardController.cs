using EnterpriseBpm.Analytics.Application.Contracts;
using EnterpriseBpm.Analytics.Application.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EnterpriseBpm.Analytics.Presentation.Controllers;

[ApiController]
[Route("api/analytics")]
[Authorize]
public class AnalyticsScorecardController : ControllerBase
{
    private readonly IEmployeeScorecardService _scorecardService;
    private readonly IFinancialAnalyticsService _financialService;

    public AnalyticsScorecardController(
        IEmployeeScorecardService scorecardService,
        IFinancialAnalyticsService financialService)
    {
        _scorecardService = scorecardService;
        _financialService = financialService;
    }

    [HttpGet("scorecards/department")]
    public async Task<ActionResult<IReadOnlyList<EmployeeProductivityDto>>> GetDepartmentScorecards(
        [FromQuery] Guid? departmentId,
        [FromQuery] string? yearMonth,
        CancellationToken cancellationToken)
    {
        var ym = string.IsNullOrWhiteSpace(yearMonth) ? DateTime.UtcNow.ToString("yyyy-MM") : yearMonth;
        var result = await _scorecardService.GetDepartmentScorecardsAsync(departmentId, ym, cancellationToken);
        return Ok(result);
    }

    [HttpGet("scorecards/user/{userId:guid}")]
    public async Task<ActionResult<EmployeeProductivityDto>> GetEmployeeScorecard(
        [FromRoute] Guid userId,
        [FromQuery] string? yearMonth,
        CancellationToken cancellationToken)
    {
        var ym = string.IsNullOrWhiteSpace(yearMonth) ? DateTime.UtcNow.ToString("yyyy-MM") : yearMonth;
        var result = await _scorecardService.GetEmployeeScorecardAsync(userId, ym, cancellationToken);
        if (result == null) return NotFound();
        return Ok(result);
    }

    [HttpGet("financial/summary")]
    public async Task<ActionResult<FinancialAnalyticsSummaryDto>> GetFinancialSummary(
        [FromQuery] DateTime? fromDate,
        [FromQuery] DateTime? toDate,
        CancellationToken cancellationToken)
    {
        var result = await _financialService.GetExpenditureSummaryAsync(fromDate, toDate, cancellationToken);
        return Ok(result);
    }
}

