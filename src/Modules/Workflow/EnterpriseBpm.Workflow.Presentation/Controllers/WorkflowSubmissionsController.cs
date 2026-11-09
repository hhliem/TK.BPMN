using EnterpriseBpm.SharedKernel.Contracts;
using EnterpriseBpm.Workflow.Application.Contracts;
using EnterpriseBpm.Workflow.Application.DTOs;
using Microsoft.AspNetCore.Mvc;

namespace EnterpriseBpm.Workflow.Presentation.Controllers;

[ApiController]
[Route("api/workflows/submissions")]
public class WorkflowSubmissionsController : ControllerBase
{
    private readonly IWorkflowSubmissionService _submissionService;
    private readonly ICurrentUserService _currentUserService;

    public WorkflowSubmissionsController(
        IWorkflowSubmissionService submissionService,
        ICurrentUserService currentUserService)
    {
        _submissionService = submissionService;
        _currentUserService = currentUserService;
    }

    [HttpPost]
    public async Task<IActionResult> Submit([FromBody] CreateWorkflowSubmissionRequest request, CancellationToken cancellationToken)
    {
        var currentUserId = _currentUserService.UserId;
        if (!currentUserId.HasValue)
        {
            return Unauthorized(new { message = "Không xác định được danh tính người dùng trong phiên làm việc." });
        }

        var result = await _submissionService.SubmitWorkflowAsync(request, currentUserId.Value, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        var result = await _submissionService.GetByIdAsync(id, cancellationToken);
        if (result == null) return NotFound(new { message = $"Không tìm thấy phiếu trình ký với ID: {id}" });

        return Ok(result);
    }

    [HttpGet("instance/{instanceId}")]
    public async Task<IActionResult> GetByInstanceId(string instanceId, CancellationToken cancellationToken)
    {
        var result = await _submissionService.GetByInstanceIdAsync(instanceId, cancellationToken);
        if (result == null) return NotFound(new { message = $"Không tìm thấy phiếu trình ký với InstanceId: {instanceId}" });

        return Ok(result);
    }

    [HttpPost("{id:guid}/cancel")]
    public async Task<IActionResult> Cancel(Guid id, [FromBody] CancelWorkflowRequest request, CancellationToken cancellationToken)
    {
        var currentUserId = _currentUserService.UserId;
        if (!currentUserId.HasValue)
        {
            return Unauthorized(new { message = "Không xác định được danh tính người dùng." });
        }

        await _submissionService.CancelSubmissionAsync(id, currentUserId.Value, request.Reason, cancellationToken);
        return NoContent();
    }
}

public record CancelWorkflowRequest(string Reason);

