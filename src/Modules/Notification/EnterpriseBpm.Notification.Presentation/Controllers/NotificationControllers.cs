using EnterpriseBpm.Notification.Application.Contracts;
using Microsoft.AspNetCore.Mvc;

namespace EnterpriseBpm.Notification.Presentation.Controllers;

[ApiController]
[Route("api/notifications/templates")]
public class NotificationTemplatesController : ControllerBase
{
    private readonly INotificationTemplateService _templateService;

    public NotificationTemplatesController(INotificationTemplateService templateService)
    {
        _templateService = templateService;
    }

    [HttpGet]
    public async Task<IActionResult> GetAllTemplates(CancellationToken cancellationToken)
    {
        var templates = await _templateService.GetAllTemplatesAsync(cancellationToken);
        return Ok(templates);
    }

    [HttpGet("{code}")]
    public async Task<IActionResult> GetByCode(string code, CancellationToken cancellationToken)
    {
        var template = await _templateService.GetByCodeAsync(code, cancellationToken);
        return Ok(template);
    }

    [HttpPost]
    public async Task<IActionResult> CreateOrUpdateTemplate([FromBody] CreateOrUpdateTemplateDto dto, CancellationToken cancellationToken)
    {
        var result = await _templateService.CreateOrUpdateTemplateAsync(dto, cancellationToken);
        return Ok(result);
    }

    [HttpPost("preview")]
    public async Task<IActionResult> RenderPreview([FromBody] PreviewTemplateRequest request, CancellationToken cancellationToken)
    {
        var preview = await _templateService.RenderPreviewAsync(request.TemplateCode, request.SampleData, cancellationToken);
        return Ok(new { TemplateCode = request.TemplateCode, RenderedHtml = preview });
    }
}

public record PreviewTemplateRequest(
    string TemplateCode,
    Dictionary<string, object?> SampleData
);

[ApiController]
[Route("api/notifications/logs")]
public class NotificationLogsController : ControllerBase
{
    private readonly INotificationLogService _logService;

    public NotificationLogsController(INotificationLogService logService)
    {
        _logService = logService;
    }

    [HttpGet]
    public async Task<IActionResult> GetLogs([FromQuery] string? status = null, [FromQuery] int take = 50, CancellationToken cancellationToken = default)
    {
        var logs = await _logService.GetLogsAsync(status, take, cancellationToken);
        return Ok(logs);
    }

    [HttpGet("stats")]
    public async Task<IActionResult> GetStats(CancellationToken cancellationToken)
    {
        var stats = await _logService.GetStatsAsync(cancellationToken);
        return Ok(stats);
    }
}

