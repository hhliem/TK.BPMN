using EnterpriseBpm.Document.Application.Contracts;
using EnterpriseBpm.SharedKernel.Contracts;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace EnterpriseBpm.Document.Presentation.Controllers;

[ApiController]
[Route("api/documents/attachments")]
public class WorkflowAttachmentsController : ControllerBase
{
    private readonly IDocumentStorageService _storageService;
    private readonly ICurrentUserService _currentUserService;

    public WorkflowAttachmentsController(
        IDocumentStorageService storageService,
        ICurrentUserService currentUserService)
    {
        _storageService = storageService;
        _currentUserService = currentUserService;
    }

    [HttpPost("upload")]
    [RequestSizeLimit(30 * 1024 * 1024)] // 30 MB request cap
    public async Task<IActionResult> UploadAttachment(
        [FromForm] string workflowInstanceId,
        [FromForm] string formKey,
        IFormFile? file,
        CancellationToken cancellationToken)
    {
        var currentUserId = _currentUserService.UserId;
        if (!currentUserId.HasValue)
        {
            return Unauthorized(new { message = "Không xác định được danh tính người dùng trong phiên làm việc." });
        }

        if (file == null || file.Length == 0)
        {
            return BadRequest(new { message = "Vui lòng chọn tệp tin hợp lệ để tải lên." });
        }

        await using var stream = file.OpenReadStream();
        var result = await _storageService.UploadFileAsync(
            workflowInstanceId: workflowInstanceId,
            formKey: formKey,
            fileName: file.FileName,
            fileStream: stream,
            contentType: file.ContentType,
            uploadedByUserId: currentUserId.Value,
            cancellationToken: cancellationToken);

        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        var attachment = await _storageService.GetAttachmentByIdAsync(id, cancellationToken);
        if (attachment == null) return NotFound(new { message = $"Không tìm thấy tệp đính kèm với ID: {id}" });

        return Ok(attachment);
    }

    [HttpGet("workflow/{workflowInstanceId}")]
    public async Task<IActionResult> GetByWorkflow(string workflowInstanceId, CancellationToken cancellationToken)
    {
        var list = await _storageService.GetAttachmentsByWorkflowAsync(workflowInstanceId, cancellationToken);
        return Ok(list);
    }

    [HttpGet("{id:guid}/preview-url")]
    public async Task<IActionResult> GetPreviewUrl(Guid id, CancellationToken cancellationToken)
    {
        var preview = await _storageService.GetSecurePreviewUrlAsync(id, cancellationToken);
        return Ok(preview);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> DeleteAttachment(Guid id, CancellationToken cancellationToken)
    {
        var currentUserId = _currentUserService.UserId;
        if (!currentUserId.HasValue)
        {
            return Unauthorized(new { message = "Không xác định được danh tính người dùng." });
        }

        await _storageService.DeleteAttachmentAsync(id, currentUserId.Value, cancellationToken);
        return NoContent();
    }
}

