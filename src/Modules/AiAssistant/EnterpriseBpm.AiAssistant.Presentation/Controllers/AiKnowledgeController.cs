using EnterpriseBpm.AiAssistant.Application.Contracts;
using EnterpriseBpm.AiAssistant.Domain.Enums;
using EnterpriseBpm.SharedKernel.Contracts;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace EnterpriseBpm.AiAssistant.Presentation.Controllers;

[ApiController]
[Route("api/ai/knowledge")]
public class AiKnowledgeController : ControllerBase
{
    private readonly IKnowledgeService _knowledgeService;
    private readonly ICurrentUserService _currentUserService;

    public AiKnowledgeController(
        IKnowledgeService knowledgeService,
        ICurrentUserService currentUserService)
    {
        _knowledgeService = knowledgeService;
        _currentUserService = currentUserService;
    }

    [HttpPost("upload")]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> UploadDocument(
        [FromForm] IFormFile file,
        [FromForm] KnowledgeDocumentType type = KnowledgeDocumentType.Policy,
        CancellationToken cancellationToken = default)
    {
        if (file == null || file.Length == 0)
        {
            return BadRequest(new { message = "Vui lòng chọn tệp tài liệu hợp lệ để đánh chỉ mục." });
        }

        var userId = _currentUserService.UserId ?? Guid.Empty;
        using var stream = file.OpenReadStream();

        var docId = await _knowledgeService.UploadAndIndexDocumentAsync(
            fileStream: stream,
            fileName: file.FileName,
            type: type,
            userId: userId,
            cancellationToken: cancellationToken);

        return Ok(new
        {
            documentId = docId,
            fileName = file.FileName,
            documentType = type.ToString(),
            message = "Tài liệu đã được tiếp nhận và đưa vào hàng đợi đánh chỉ mục Qdrant."
        });
    }

    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
    {
        var list = await _knowledgeService.GetKnowledgeDocumentsAsync(cancellationToken);
        return Ok(list);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        var doc = await _knowledgeService.GetDocumentByIdAsync(id, cancellationToken);
        if (doc == null) return NotFound(new { message = $"Không tìm thấy tài liệu tri thức với mã: {id}" });
        return Ok(doc);
    }
}

