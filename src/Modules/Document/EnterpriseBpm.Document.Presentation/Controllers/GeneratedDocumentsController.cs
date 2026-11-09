using EnterpriseBpm.Document.Application.Contracts;
using EnterpriseBpm.Document.Application.DTOs;
using EnterpriseBpm.SharedKernel.Contracts;
using Microsoft.AspNetCore.Mvc;

namespace EnterpriseBpm.Document.Presentation.Controllers;

[ApiController]
[Route("api/documents/generated")]
public class GeneratedDocumentsController : ControllerBase
{
    private readonly IDocumentGeneratorService _generatorService;
    private readonly ICurrentUserService _currentUserService;

    public GeneratedDocumentsController(
        IDocumentGeneratorService generatorService,
        ICurrentUserService currentUserService)
    {
        _generatorService = generatorService;
        _currentUserService = currentUserService;
    }

    [HttpPost("from-template")]
    public async Task<IActionResult> GenerateFromTemplate(
        [FromQuery] string workflowInstanceId,
        [FromBody] GenerateDocumentRequest request,
        CancellationToken cancellationToken)
    {
        var currentUserId = _currentUserService.UserId;
        if (!currentUserId.HasValue)
        {
            return Unauthorized(new { message = "Không xác định được danh tính người dùng trong phiên làm việc." });
        }

        var result = await _generatorService.GenerateFromTemplateAsync(
            templateKey: request.TemplateKey,
            workflowInstanceId: workflowInstanceId,
            placeholderValues: request.PlaceholderValues,
            generatedByUserId: currentUserId.Value,
            cancellationToken: cancellationToken);

        return Ok(result);
    }

    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] int limit = 50, CancellationToken cancellationToken = default)
    {
        var docs = await _generatorService.GetAllGeneratedDocumentsAsync(limit, cancellationToken);
        return Ok(docs);
    }

    [HttpGet("workflow/{workflowInstanceId}")]
    public async Task<IActionResult> GetByWorkflow(string workflowInstanceId, CancellationToken cancellationToken)
    {
        var docs = await _generatorService.GetGeneratedDocumentsAsync(workflowInstanceId, cancellationToken);
        return Ok(docs);
    }

    [HttpGet("{id}/verify")]
    public async Task<IActionResult> VerifyDocument(Guid id, CancellationToken cancellationToken)
    {
        var isValid = await _generatorService.VerifyDocumentIntegrityAsync(id, cancellationToken);
        return Ok(new { DocumentId = id, IsValid = isValid, VerifiedAt = DateTime.UtcNow });
    }
}

