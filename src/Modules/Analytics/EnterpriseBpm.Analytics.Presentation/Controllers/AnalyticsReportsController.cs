using EnterpriseBpm.Analytics.Application.Contracts;
using EnterpriseBpm.Analytics.Application.DTOs;
using EnterpriseBpm.Analytics.Domain.Enums;
using EnterpriseBpm.SharedKernel.Contracts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EnterpriseBpm.Analytics.Presentation.Controllers;

[ApiController]
[Route("api/analytics/reports")]
[Authorize]
public class AnalyticsReportsController : ControllerBase
{
    private readonly IDynamicReportService _reportService;
    private readonly ICurrentUserService _currentUserService;

    public AnalyticsReportsController(
        IDynamicReportService reportService,
        ICurrentUserService currentUserService)
    {
        _reportService = reportService;
        _currentUserService = currentUserService;
    }

    [HttpGet("columns/{dataSource}")]
    public ActionResult<IReadOnlyList<DynamicReportColumnDto>> GetColumns([FromRoute] DataSourceType dataSource)
    {
        var columns = _reportService.GetAvailableColumns(dataSource);
        return Ok(columns);
    }

    [HttpPost("execute")]
    public async Task<ActionResult<DynamicReportResultDto>> ExecuteReport([FromBody] ExecuteReportRequestDto request, CancellationToken cancellationToken)
    {
        var result = await _reportService.ExecuteReportAsync(request, cancellationToken);
        return Ok(result);
    }

    [HttpPost("export")]
    public async Task<IActionResult> ExportReport(
        [FromBody] ExecuteReportRequestDto request,
        [FromQuery] ReportExportFormat format = ReportExportFormat.Excel,
        CancellationToken cancellationToken = default)
    {
        var fileBytes = await _reportService.ExportReportFileAsync(request, format, cancellationToken);
        var timeStamp = DateTime.UtcNow.ToString("yyyyMMdd_HHmmss");

        if (format == ReportExportFormat.Csv)
        {
            return File(fileBytes, "text/csv", $"Report_{request.DataSource}_{timeStamp}.csv");
        }

        return File(fileBytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"Report_{request.DataSource}_{timeStamp}.xlsx");
    }

    [HttpGet("templates")]
    public async Task<ActionResult<IReadOnlyList<CustomReportTemplateDto>>> GetTemplates(CancellationToken cancellationToken)
    {
        var userId = _currentUserService.UserId;
        var templates = await _reportService.GetSavedTemplatesAsync(userId, cancellationToken);
        return Ok(templates);
    }

    [HttpPost("templates")]
    public async Task<ActionResult<Guid>> SaveTemplate([FromBody] SaveReportTemplateRequestDto request, CancellationToken cancellationToken)
    {
        var userId = _currentUserService.UserId ?? Guid.Empty;
        var id = await _reportService.SaveReportTemplateAsync(request, userId, cancellationToken);
        return Ok(id);
    }
}

