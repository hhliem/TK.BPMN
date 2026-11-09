using EnterpriseBpm.Document.Application.Contracts;
using EnterpriseBpm.Document.Application.DTOs;
using Microsoft.AspNetCore.Mvc;

namespace EnterpriseBpm.Document.Presentation.Controllers;

[ApiController]
[Route("api/documents/templates")]
public class DocumentTemplatesController : ControllerBase
{
    private readonly IDocumentTemplateService _templateService;

    public DocumentTemplatesController(IDocumentTemplateService templateService)
    {
        _templateService = templateService;
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateDocumentTemplateCommand command, CancellationToken cancellationToken)
    {
        var id = await _templateService.CreateTemplateAsync(command, cancellationToken);
        return CreatedAtAction(nameof(GetByKey), new { key = command.TemplateKey }, new { id, command.TemplateKey });
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateDocumentTemplateCommand command, CancellationToken cancellationToken)
    {
        await _templateService.UpdateTemplateAsync(id, command, cancellationToken);
        return NoContent();
    }

    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] bool? activeOnly, CancellationToken cancellationToken)
    {
        var list = await _templateService.GetAllTemplatesAsync(activeOnly, cancellationToken);
        return Ok(list);
    }

    [HttpGet("{key}")]
    public async Task<IActionResult> GetByKey(string key, CancellationToken cancellationToken)
    {
        var template = await _templateService.GetTemplateByKeyAsync(key, cancellationToken);
        if (template == null) return NotFound(new { message = $"Không tìm thấy mẫu tài liệu với khóa: {key}" });

        return Ok(template);
    }
}

