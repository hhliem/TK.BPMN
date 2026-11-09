using EnterpriseBpm.AuditLog.Application.Contracts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EnterpriseBpm.AuditLog.Presentation.Controllers;

[ApiController]
[Route("api/audit")]
[Authorize]
public class AuditController : ControllerBase
{
    private readonly IAuditQueryService _queryService;

    public AuditController(IAuditQueryService queryService)
    {
        _queryService = queryService;
    }

    [HttpGet("entity/{entityType}/{entityId}")]
    public async Task<IActionResult> GetEntityHistory(
        string entityType, 
        string entityId,
        [FromQuery] int pageIndex = 1, 
        [FromQuery] int pageSize = 50,
        CancellationToken cancellationToken = default)
    {
        var result = await _queryService.GetEntityHistoryAsync(
            entityType, entityId, pageIndex, pageSize, cancellationToken);
        return Ok(result);
    }

    [HttpGet("users/{userId}")]
    public async Task<IActionResult> GetUserActivity(
        Guid userId,
        [FromQuery] DateTime? from, 
        [FromQuery] DateTime? to,
        [FromQuery] int pageIndex = 1, 
        [FromQuery] int pageSize = 50,
        CancellationToken cancellationToken = default)
    {
        var result = await _queryService.GetUserActivityAsync(
            userId, from, to, pageIndex, pageSize, cancellationToken);
        return Ok(result);
    }

    [HttpGet("security-events")]
    public async Task<IActionResult> GetSecurityEvents(
        [FromQuery] DateTime? from, 
        [FromQuery] DateTime? to,
        [FromQuery] string? eventType,
        [FromQuery] int pageIndex = 1, 
        [FromQuery] int pageSize = 50,
        CancellationToken cancellationToken = default)
    {
        var result = await _queryService.GetSecurityEventsAsync(
            from, to, eventType, pageIndex, pageSize, cancellationToken);
        return Ok(result);
    }

    [HttpGet("export")]
    public async Task<IActionResult> ExportAuditLogs(
        [FromQuery] DateTime from, 
        [FromQuery] DateTime to,
        [FromQuery] string? moduleName,
        CancellationToken cancellationToken = default)
    {
        var fileBytes = await _queryService.ExportExcelAsync(from, to, moduleName, cancellationToken);
        var fileName = $"AuditLogs_{from:yyyyMMdd}_{to:yyyyMMdd}.xlsx";
        return File(fileBytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
    }
}

