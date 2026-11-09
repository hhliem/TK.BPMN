using System.Security.Claims;
using EnterpriseBpm.Form.Application.Contracts;
using EnterpriseBpm.Form.Application.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EnterpriseBpm.Form.Presentation.Controllers;

[ApiController]
[Route("api/forms")]
[Authorize]
public class FormDefinitionsController : ControllerBase
{
    private readonly IFormVersionService _versionService;

    public FormDefinitionsController(IFormVersionService versionService)
    {
        _versionService = versionService;
    }

    [HttpPost]
    public async Task<IActionResult> RegisterForm([FromBody] RegisterFormCommand command, CancellationToken cancellationToken)
    {
        var publishedBy = GetUserId();
        var id = await _versionService.RegisterFormAsync(command, publishedBy, cancellationToken);
        return Ok(new { Id = id, Message = "Đăng ký biểu mẫu mới thành công." });
    }

    [HttpPost("{formKey}/versions")]
    public async Task<IActionResult> CreateNewVersion(
        string formKey, 
        [FromBody] CreateVersionRequest request, 
        CancellationToken cancellationToken)
    {
        var publishedBy = GetUserId();
        var result = await _versionService.CreateNewVersionAsync(formKey, request.SchemaJson, request.ChangeNotes, publishedBy, cancellationToken);
        return Ok(result);
    }

    [HttpGet("{formKey}")]
    public async Task<IActionResult> GetForm(
        string formKey, 
        [FromQuery] int? version, 
        CancellationToken cancellationToken)
    {
        var result = await _versionService.GetVersionAsync(formKey, version, cancellationToken);
        if (result == null)
        {
            return NotFound(new { Message = $"Không tìm thấy định nghĩa biểu mẫu '{formKey}'." });
        }
        return Ok(result);
    }

    [HttpPost("{formKey}/rollback")]
    public async Task<IActionResult> Rollback(
        string formKey, 
        [FromBody] RollbackRequest request, 
        CancellationToken cancellationToken)
    {
        var rolledBackBy = GetUserId();
        var result = await _versionService.RollbackToVersionAsync(formKey, request.TargetVersion, request.Reason, rolledBackBy, cancellationToken);
        return Ok(result);
    }

    [HttpGet("{formKey}/history")]
    public async Task<IActionResult> GetHistory(string formKey, CancellationToken cancellationToken)
    {
        var history = await _versionService.GetVersionHistoryAsync(formKey, cancellationToken);
        return Ok(history);
    }

    private Guid? GetUserId()
    {
        var id = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
            ?? User.FindFirst("sub")?.Value;
        return Guid.TryParse(id, out var userId) ? userId : null;
    }
}

public record CreateVersionRequest(string SchemaJson, string? ChangeNotes);
public record RollbackRequest(int TargetVersion, string? Reason);
public record ValidateDependencyRequest(Guid ChildId, Guid ParentId);

[ApiController]
[Route("api/master-data")]
[Authorize]
public class MasterDataController : ControllerBase
{
    private readonly IMasterDataService _masterDataService;

    public MasterDataController(IMasterDataService masterDataService)
    {
        _masterDataService = masterDataService;
    }

    [HttpGet("items")]
    public async Task<IActionResult> GetItems(
        [FromQuery] string categoryKey, 
        [FromQuery] Guid? parentId, 
        CancellationToken cancellationToken)
    {
        var items = await _masterDataService.GetItemsAsync(categoryKey, parentId, cancellationToken);
        return Ok(items);
    }

    [HttpPost("items")]
    public async Task<IActionResult> CreateItem(
        [FromBody] CreateMasterItemCommand command, 
        CancellationToken cancellationToken)
    {
        var id = await _masterDataService.CreateItemAsync(command, cancellationToken);
        return Ok(new { Id = id, Message = "Tạo mục danh mục thành công" });
    }

    [HttpPost("validate-dependency")]
    public async Task<IActionResult> ValidateDependency(
        [FromBody] ValidateDependencyRequest request, 
        CancellationToken cancellationToken)
    {
        var isValid = await _masterDataService.ValidateItemBelongsToParentAsync(request.ChildId, request.ParentId, cancellationToken);
        return Ok(new { IsValid = isValid, Message = "Dữ liệu phụ thuộc hợp lệ." });
    }
}

