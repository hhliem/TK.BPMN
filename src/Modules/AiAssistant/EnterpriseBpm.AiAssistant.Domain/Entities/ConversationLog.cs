using EnterpriseBpm.AiAssistant.Domain.Enums;
using EnterpriseBpm.SharedKernel;

namespace EnterpriseBpm.AiAssistant.Domain.Entities;

public class ConversationLog : BaseEntity<Guid>
{
    public Guid SessionId { get; private set; }
    public Guid UserId { get; private set; }
    public AiProviderType Provider { get; private set; }
    public string ModelName { get; private set; } = default!;
    public string UserQuery { get; private set; } = default!;
    public string AiResponse { get; private set; } = default!;
    public string? ToolsCalledJson { get; private set; } // JSON array of tool names and parameters
    public int ResponseTimeMs { get; private set; }
    public int PromptTokens { get; private set; }
    public int CompletionTokens { get; private set; }
    public int? UserFeedback { get; private set; } // 1: Helpful (Like), -1: Not helpful (Dislike), null: None
    public string? FeedbackComment { get; private set; }
    public DateTime CreatedAt { get; private set; } = DateTime.UtcNow;

    protected ConversationLog() { }

    public static ConversationLog Create(
        Guid sessionId,
        Guid userId,
        AiProviderType provider,
        string modelName,
        string userQuery,
        string aiResponse,
        string? toolsCalledJson = null,
        int responseTimeMs = 0,
        int promptTokens = 0,
        int completionTokens = 0)
    {
        return new ConversationLog
        {
            Id = Guid.NewGuid(),
            SessionId = sessionId == Guid.Empty ? Guid.NewGuid() : sessionId,
            UserId = userId,
            Provider = provider,
            ModelName = string.IsNullOrWhiteSpace(modelName) ? "unknown" : modelName.Trim(),
            UserQuery = userQuery ?? string.Empty,
            AiResponse = aiResponse ?? string.Empty,
            ToolsCalledJson = toolsCalledJson,
            ResponseTimeMs = responseTimeMs,
            PromptTokens = promptTokens,
            CompletionTokens = completionTokens,
            CreatedAt = DateTime.UtcNow
        };
    }

    public void SetFeedback(int feedback, string? comment = null)
    {
        UserFeedback = feedback;
        FeedbackComment = comment?.Trim();
    }
}

