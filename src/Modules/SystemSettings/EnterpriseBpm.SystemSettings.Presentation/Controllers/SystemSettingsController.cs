using EnterpriseBpm.SystemSettings.Application.Contracts;
using Microsoft.AspNetCore.Mvc;

namespace EnterpriseBpm.SystemSettings.Presentation.Controllers;

[ApiController]
[Route("api/settings")]
public class SystemSettingsController : ControllerBase
{
    private readonly ISystemSettingService _settingService;

    public SystemSettingsController(ISystemSettingService settingService)
    {
        _settingService = settingService;
    }

    [HttpGet]
    public async Task<IActionResult> GetAllSettings(CancellationToken cancellationToken)
    {
        var settings = await _settingService.GetAllSettingsAsync(cancellationToken);
        return Ok(settings);
    }

    [HttpGet("{key}")]
    public async Task<IActionResult> GetSetting(string key, CancellationToken cancellationToken)
    {
        var val = await _settingService.GetSettingValueAsync(key, cancellationToken: cancellationToken);
        return Ok(new { Key = key, Value = val });
    }

    [HttpPut("{key}")]
    public async Task<IActionResult> UpdateSetting(
        string key,
        [FromBody] UpdateSettingRequest request,
        [FromQuery] Guid? userId = null,
        [FromQuery] string? userName = null,
        CancellationToken cancellationToken = default)
    {
        var user = userId ?? Guid.Parse("11111111-1111-1111-1111-111111111111");
        var name = userName ?? "SuperAdmin";

        var result = await _settingService.UpdateSettingAsync(key, request.Value, request.Reason, user, name, cancellationToken);
        return Ok(result);
    }

    [HttpGet("{key}/history")]
    public async Task<IActionResult> GetSettingHistory(string key, CancellationToken cancellationToken)
    {
        var history = await _settingService.GetChangeHistoryAsync(key, cancellationToken);
        return Ok(history);
    }

    [HttpGet("brand-theme")]
    public async Task<IActionResult> GetBrandTheme(CancellationToken cancellationToken)
    {
        var theme = await _settingService.GetBrandThemeAsync(cancellationToken);
        return Ok(theme);
    }

    [HttpPut("brand-theme")]
    public async Task<IActionResult> UpdateBrandTheme(
        [FromBody] BrandThemeDto dto,
        [FromQuery] string? reason = null,
        [FromQuery] Guid? userId = null,
        [FromQuery] string? userName = null,
        CancellationToken cancellationToken = default)
    {
        var user = userId ?? Guid.Parse("11111111-1111-1111-1111-111111111111");
        var name = userName ?? "SuperAdmin";

        var result = await _settingService.UpdateBrandThemeAsync(dto, reason, user, name, cancellationToken);
        return Ok(result);
    }
}

