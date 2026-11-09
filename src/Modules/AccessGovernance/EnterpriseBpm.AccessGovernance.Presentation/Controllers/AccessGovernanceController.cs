using EnterpriseBpm.AccessGovernance.Application.Contracts;
using EnterpriseBpm.AccessGovernance.Application.DTOs;
using EnterpriseBpm.AccessGovernance.Presentation.Extensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EnterpriseBpm.AccessGovernance.Presentation.Controllers;

[ApiController]
[Route("api/auth/governance")]
[Authorize]
public class AccessGovernanceController : ControllerBase
{
    private readonly IAccessGovernanceService _governanceService;

    public AccessGovernanceController(IAccessGovernanceService governanceService)
    {
        _governanceService = governanceService;
    }

    [HttpGet("matrix/role/{roleId:guid}")]
    public async Task<IActionResult> GetRolePermissionMatrix(Guid roleId, CancellationToken cancellationToken)
    {
        var matrix = await _governanceService.GetRolePermissionsAsync(roleId, cancellationToken);
        return Ok(matrix);
    }

    [HttpPost("matrix/role/{roleId:guid}/update")]
    public async Task<IActionResult> UpdateRolePermissions(
        Guid roleId, 
        [FromBody] UpdateRolePermissionsDto dto, 
        CancellationToken cancellationToken)
    {
        await _governanceService.UpdateRolePermissionsAsync(roleId, dto.PermissionIds, cancellationToken);
        return NoContent();
    }

    [HttpGet("users/{userId:guid}/effective-permissions")]
    public async Task<IActionResult> GetEffectiveUserPermissions(Guid userId, CancellationToken cancellationToken)
    {
        var permissions = await _governanceService.GetEffectivePermissionsAsync(userId, cancellationToken);
        return Ok(permissions);
    }

    [HttpPost("approval-limits")]
    public async Task<IActionResult> ConfigureApprovalLimit(
        [FromBody] CreateApprovalLimitCommand command, 
        CancellationToken cancellationToken)
    {
        var limitId = await _governanceService.CreateApprovalLimitAsync(command, cancellationToken);
        return Ok(new { Id = limitId, Message = "Thiết lập hạn mức phê duyệt thành công" });
    }

    [HttpGet("menu/user-navigation")]
    public async Task<IActionResult> GetAuthorizedNavigationMenu(CancellationToken cancellationToken)
    {
        var currentUserId = User.GetUserId();
        var authorizedMenu = await _governanceService.GetUserAuthorizedMenuAsync(currentUserId, cancellationToken);
        return Ok(authorizedMenu);
    }
}

