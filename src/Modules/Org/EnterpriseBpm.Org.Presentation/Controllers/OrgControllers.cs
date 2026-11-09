using System.Security.Claims;
using EnterpriseBpm.Org.Application.Contracts;
using EnterpriseBpm.Org.Application.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EnterpriseBpm.Org.Presentation.Controllers;

[ApiController]
[Route("api/org/units")]
[Authorize]
public class OrgUnitsController : ControllerBase
{
    private readonly IOrgHierarchyService _hierarchyService;

    public OrgUnitsController(IOrgHierarchyService hierarchyService)
    {
        _hierarchyService = hierarchyService;
    }

    [HttpGet("tree")]
    public async Task<IActionResult> GetOrgTree(CancellationToken cancellationToken)
    {
        var tree = await _hierarchyService.GetOrgTreeAsync(cancellationToken);
        return Ok(tree);
    }

    [HttpPost]
    public async Task<IActionResult> CreateOrgUnit([FromBody] CreateOrgUnitCommand command, CancellationToken cancellationToken)
    {
        var id = await _hierarchyService.CreateOrgUnitAsync(command, cancellationToken);
        return Ok(new { Id = id, Message = "Tạo đơn vị tổ chức thành công" });
    }

    [HttpPut("{id:guid}/move")]
    public async Task<IActionResult> MoveOrgUnit(Guid id, [FromQuery] Guid? newParentId, CancellationToken cancellationToken)
    {
        await _hierarchyService.MoveOrgUnitAsync(id, newParentId, cancellationToken);
        return Ok(new { Message = "Di chuyển đơn vị tổ chức thành công" });
    }
}

[ApiController]
[Route("api/org/delegations")]
[Authorize]
public class DelegationsController : ControllerBase
{
    private readonly IDelegationService _delegationService;

    public DelegationsController(IDelegationService delegationService)
    {
        _delegationService = delegationService;
    }

    [HttpPost]
    public async Task<IActionResult> AssignDelegation([FromBody] AssignDelegationCommand command, CancellationToken cancellationToken)
    {
        var delegationId = await _delegationService.AssignDelegationAsync(command, cancellationToken);
        return Ok(new { Id = delegationId, Message = "Thiết lập ủy quyền thành công" });
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> RevokeDelegation(Guid id, CancellationToken cancellationToken)
    {
        await _delegationService.RevokeDelegationAsync(id, cancellationToken);
        return NoContent();
    }

    [HttpGet("my")]
    public async Task<IActionResult> GetMyDelegations(CancellationToken cancellationToken)
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
            ?? User.FindFirst("sub")?.Value;

        if (!Guid.TryParse(userIdClaim, out var employeeId))
        {
            return Unauthorized();
        }

        var delegations = await _delegationService.GetMyDelegationsAsync(employeeId, cancellationToken);
        return Ok(delegations);
    }
}

[ApiController]
[Route("api/org/directory")]
[Authorize]
public class EmployeeDirectoryController : ControllerBase
{
    private readonly IEmployeeDirectoryService _directoryService;

    public EmployeeDirectoryController(IEmployeeDirectoryService directoryService)
    {
        _directoryService = directoryService;
    }

    [HttpGet("search")]
    public async Task<IActionResult> Search(
        [FromQuery] string? keyword, 
        [FromQuery] Guid? orgUnitId, 
        [FromQuery] int page = 1, 
        [FromQuery] int pageSize = 20, 
        CancellationToken cancellationToken = default)
    {
        var result = await _directoryService.SearchEmployeesAsync(keyword, orgUnitId, page, pageSize, cancellationToken);
        return Ok(result);
    }

    [HttpGet("employees/{id:guid}")]
    public async Task<IActionResult> GetProfile(Guid id, CancellationToken cancellationToken)
    {
        var profile = await _directoryService.GetEmployeeProfileAsync(id, cancellationToken);
        if (profile == null)
        {
            return NotFound(new { Message = "Không tìm thấy hồ sơ nhân sự yêu cầu." });
        }
        return Ok(profile);
    }
}

