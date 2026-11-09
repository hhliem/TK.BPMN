using EnterpriseBpm.SharedKernel.Contracts;
using EnterpriseBpm.Workflow.Application.Contracts;
using EnterpriseBpm.Workflow.Application.DTOs;
using Microsoft.AspNetCore.Mvc;

namespace EnterpriseBpm.Workflow.Presentation.Controllers;

[ApiController]
[Route("api/workflows/tasks")]
public class ApprovalTasksController : ControllerBase
{
    private readonly IApprovalTaskService _taskService;
    private readonly IWorkflowDapperReadService _dapperReadService;
    private readonly ICurrentUserService _currentUserService;

    public ApprovalTasksController(
        IApprovalTaskService taskService,
        IWorkflowDapperReadService dapperReadService,
        ICurrentUserService currentUserService)
    {
        _taskService = taskService;
        _dapperReadService = dapperReadService;
        _currentUserService = currentUserService;
    }

    [HttpGet("pending")]
    public async Task<IActionResult> GetPendingTasks(
        [FromQuery] string? status = "Pending",
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var currentUserId = _currentUserService.UserId;
        if (!currentUserId.HasValue)
        {
            return Unauthorized(new { message = "Không xác định được danh tính người dùng trong phiên làm việc." });
        }

        var filter = new PendingTaskFilterDto(status, page, pageSize);
        // TSK-805: Truy vấn tối ưu hóa đường đọc qua Dapper Streaming
        var result = await _dapperReadService.GetTasksForUserFastAsync(currentUserId.Value, filter, cancellationToken);
        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        var task = await _taskService.GetTaskByIdAsync(id, cancellationToken);
        if (task == null) return NotFound(new { message = $"Không tìm thấy nhiệm vụ phê duyệt với ID: {id}" });

        return Ok(task);
    }

    [HttpPost("{id:guid}/decision")]
    public async Task<IActionResult> ProcessDecision(
        Guid id,
        [FromBody] SubmitApprovalDecisionRequest request,
        CancellationToken cancellationToken)
    {
        var currentUserId = _currentUserService.UserId;
        if (!currentUserId.HasValue)
        {
            return Unauthorized(new { message = "Không xác định được danh tính người dùng trong phiên làm việc." });
        }

        await _taskService.ProcessDecisionAsync(id, request, currentUserId.Value, cancellationToken);
        return Ok(new { message = "Đã xử lý quyết định phê duyệt thành công." });
    }
}

