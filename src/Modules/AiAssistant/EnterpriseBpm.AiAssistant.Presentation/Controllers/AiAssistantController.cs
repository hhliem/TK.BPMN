using System.Text.Json;
using EnterpriseBpm.AiAssistant.Application.Contracts;
using EnterpriseBpm.AiAssistant.Application.DTOs;
using EnterpriseBpm.SharedKernel.Contracts;
using Microsoft.AspNetCore.Mvc;

namespace EnterpriseBpm.AiAssistant.Presentation.Controllers;

[ApiController]
[Route("api/ai")]
public class AiAssistantController : ControllerBase
{
    private readonly IAiAssistantService _aiService;
    private readonly IConversationLoggingService _loggingService;
    private readonly ICurrentUserService _currentUserService;

    public AiAssistantController(
        IAiAssistantService aiService,
        IConversationLoggingService loggingService,
        ICurrentUserService currentUserService)
    {
        _aiService = aiService;
        _loggingService = loggingService;
        _currentUserService = currentUserService;
    }

    [HttpPost("chat")]
    public async Task<IActionResult> Chat([FromBody] AiChatRequestDto request, CancellationToken cancellationToken)
    {
        var userId = _currentUserService.UserId ?? Guid.Empty;
        var response = await _aiService.ChatAsync(request, userId, cancellationToken);
        return Ok(response);
    }

    [HttpPost("chat/stream")]
    public async Task StreamChat([FromBody] AiChatRequestDto request, CancellationToken cancellationToken)
    {
        var userId = _currentUserService.UserId ?? Guid.Empty;
        Response.Headers["Content-Type"] = "text/event-stream";
        Response.Headers["Cache-Control"] = "no-cache";
        Response.Headers["Connection"] = "keep-alive";

        await foreach (var evt in _aiService.StreamChatAsync(request, userId, cancellationToken))
        {
            var dataJson = JsonSerializer.Serialize(evt);
            var sseMessage = $"data: {dataJson}\n\n";
            var bytes = System.Text.Encoding.UTF8.GetBytes(sseMessage);
            await Response.Body.WriteAsync(bytes, cancellationToken);
            await Response.Body.FlushAsync(cancellationToken);
        }
    }

    [HttpPost("feedback/{logId:guid}")]
    public async Task<IActionResult> RecordFeedback(Guid logId, [FromBody] RecordFeedbackRequest request, CancellationToken cancellationToken)
    {
        await _loggingService.RecordFeedbackAsync(logId, request.Feedback, request.Comment, cancellationToken);
        return NoContent();
    }

    [HttpGet("logs")]
    public async Task<IActionResult> GetLogs([FromQuery] int limit = 50, CancellationToken cancellationToken = default)
    {
        var logs = await _loggingService.GetRecentLogsAsync(limit, cancellationToken);
        return Ok(logs);
    }
}

public record RecordFeedbackRequest(int Feedback, string? Comment);
