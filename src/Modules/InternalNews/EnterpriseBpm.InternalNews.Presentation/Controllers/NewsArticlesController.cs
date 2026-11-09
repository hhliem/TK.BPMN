using EnterpriseBpm.InternalNews.Application.Contracts;
using Microsoft.AspNetCore.Mvc;

namespace EnterpriseBpm.InternalNews.Presentation.Controllers;

[ApiController]
[Route("api/news")]
public class NewsArticlesController : ControllerBase
{
    private readonly INewsService _newsService;

    public NewsArticlesController(INewsService newsService)
    {
        _newsService = newsService;
    }

    [HttpGet]
    public async Task<IActionResult> GetArticles(
        [FromQuery] string? category = null,
        [FromQuery] string? status = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var articles = await _newsService.GetArticlesAsync(category, status, page, pageSize, cancellationToken);
        return Ok(articles);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetArticleById(Guid id, [FromQuery] Guid? userId = null, CancellationToken cancellationToken = default)
    {
        var article = await _newsService.GetArticleByIdAsync(id, userId, cancellationToken);
        return Ok(article);
    }

    [HttpPost]
    public async Task<IActionResult> CreateArticle([FromBody] CreateNewsArticleDto dto, [FromQuery] Guid? authorId = null, CancellationToken cancellationToken = default)
    {
        var author = authorId ?? Guid.Parse("11111111-1111-1111-1111-111111111111");
        var result = await _newsService.CreateArticleAsync(dto, author, cancellationToken);
        return CreatedAtAction(nameof(GetArticleById), new { id = result.Id }, result);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateArticle(Guid id, [FromBody] UpdateNewsArticleDto dto, [FromQuery] Guid? updaterId = null, CancellationToken cancellationToken = default)
    {
        var updater = updaterId ?? Guid.Parse("11111111-1111-1111-1111-111111111111");
        var result = await _newsService.UpdateArticleAsync(id, dto, updater, cancellationToken);
        return Ok(result);
    }

    [HttpPost("{id}/publish")]
    public async Task<IActionResult> PublishArticle(Guid id, [FromQuery] Guid? publisherId = null, CancellationToken cancellationToken = default)
    {
        var publisher = publisherId ?? Guid.Parse("11111111-1111-1111-1111-111111111111");
        await _newsService.PublishArticleAsync(id, publisher, cancellationToken);
        return Ok(new { Message = "Xuất bản bài viết thành công." });
    }

    [HttpPost("{id}/archive")]
    public async Task<IActionResult> ArchiveArticle(Guid id, [FromQuery] Guid? archiverId = null, CancellationToken cancellationToken = default)
    {
        var archiver = archiverId ?? Guid.Parse("11111111-1111-1111-1111-111111111111");
        await _newsService.ArchiveArticleAsync(id, archiver, cancellationToken);
        return Ok(new { Message = "Lưu trữ bài viết thành công." });
    }

    [HttpPost("{id}/acknowledge-read")]
    public async Task<IActionResult> AcknowledgeRead(Guid id, [FromQuery] Guid? employeeId = null, CancellationToken cancellationToken = default)
    {
        var employee = employeeId ?? Guid.Parse("22222222-2222-2222-2222-222222222222");
        await _newsService.AcknowledgeReadAsync(id, employee, cancellationToken);
        return Ok(new { Message = "Đã xác nhận đọc bài viết." });
    }

    [HttpGet("{id}/compliance")]
    public async Task<IActionResult> GetComplianceStats(Guid id, CancellationToken cancellationToken = default)
    {
        var stats = await _newsService.GetComplianceStatsAsync(id, cancellationToken);
        return Ok(stats);
    }
}

