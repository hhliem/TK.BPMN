using EnterpriseBpm.AiAssistant.Application.Contracts;
using EnterpriseBpm.AiAssistant.Application.DTOs;
using EnterpriseBpm.AiAssistant.Domain.Enums;
using EnterpriseBpm.SharedKernel.Contracts;
using Microsoft.AspNetCore.Mvc;

namespace EnterpriseBpm.AiAssistant.Presentation.Controllers;

[ApiController]
[Route("api/ai/configs")]
public class AiConfigurationsController : ControllerBase
{
    private readonly IAiProviderConfigService _configService;
    private readonly IAiProviderFactory _providerFactory;
    private readonly ICurrentUserService _currentUserService;

    public AiConfigurationsController(
        IAiProviderConfigService configService,
        IAiProviderFactory providerFactory,
        ICurrentUserService currentUserService)
    {
        _configService = configService;
        _providerFactory = providerFactory;
        _currentUserService = currentUserService;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
    {
        var configs = await _configService.GetAllConfigsAsync(cancellationToken);
        return Ok(configs);
    }

    [HttpGet("default")]
    public async Task<IActionResult> GetDefault(CancellationToken cancellationToken)
    {
        var config = await _providerFactory.GetDefaultProviderConfigAsync(cancellationToken);
        return Ok(config);
    }

    [HttpPost]
    public async Task<IActionResult> SaveConfig([FromBody] SaveAiProviderConfigRequest request, CancellationToken cancellationToken)
    {
        var userId = _currentUserService.UserId;
        await _configService.SaveConfigAsync(request, userId, cancellationToken);
        return Ok(new { message = $"Đã cập nhật cấu hình cho Provider: {request.ProviderType} thành công." });
    }

    [HttpPost("default/{providerType}")]
    public async Task<IActionResult> SetDefault(AiProviderType providerType, CancellationToken cancellationToken)
    {
        await _configService.SetDefaultProviderAsync(providerType, cancellationToken);
        return Ok(new { message = $"Đã đặt {providerType} làm AI Provider mặc định của hệ thống." });
    }
}

