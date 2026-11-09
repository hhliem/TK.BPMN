using EnterpriseBpm.AccessGovernance.Application.Contracts;
using EnterpriseBpm.AccessGovernance.Application.DTOs;
using EnterpriseBpm.AccessGovernance.Presentation.Extensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EnterpriseBpm.AccessGovernance.Presentation.Controllers;

[ApiController]
[Route("api/auth/roles")]
[Authorize]
public class RolesController : ControllerBase
{
    private readonly IAccessGovernanceService _governanceService;

    public RolesController(IAccessGovernanceService governanceService)
    {
        _governanceService = governanceService;
    }

    [HttpGet]
    public async Task<IActionResult> GetAllRoles(CancellationToken cancellationToken)
    {
        var roles = await _governanceService.GetAllRolesAsync(cancellationToken);
        return Ok(roles);
    }

    [HttpPost("assign")]
    public async Task<IActionResult> AssignRoleToUser(
        [FromBody] AssignUserRoleDto dto, 
        CancellationToken cancellationToken)
    {
        var assignedBy = User.GetUserId();
        await _governanceService.AssignRoleToUserAsync(dto.UserId, dto.RoleId, assignedBy, cancellationToken);
        return Ok(new { Message = "Gán vai trò thành công." });
    }

    [HttpDelete("revoke/{userId:guid}/{roleId:guid}")]
    public async Task<IActionResult> RevokeRoleFromUser(
        Guid userId, 
        Guid roleId, 
        CancellationToken cancellationToken)
    {
        await _governanceService.RevokeRoleFromUserAsync(userId, roleId, cancellationToken);
        return NoContent();
    }
}

[ApiController]
[Route("api/auth/permissions")]
[Authorize]
public class PermissionsController : ControllerBase
{
    private readonly IAccessGovernanceService _governanceService;

    public PermissionsController(IAccessGovernanceService governanceService)
    {
        _governanceService = governanceService;
    }

    [HttpGet]
    public async Task<IActionResult> GetAllPermissions(CancellationToken cancellationToken)
    {
        var perms = await _governanceService.GetAllPermissionsAsync(cancellationToken);
        return Ok(perms);
    }
}

