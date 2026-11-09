using EnterpriseBpm.AiAssistant.Domain.Enums;

namespace EnterpriseBpm.AiAssistant.Application.DTOs;

public record ChatMessageDto(
    ChatRole Role,
    string Content,
    string? ToolCallId = null,
    string? ToolName = null
);

public record ChatToolCallDto(
    string Id,
    string Name,
    string ArgumentsJson
);

public record ToolParameterProperty(
    string Type,
    string Description,
    bool Required = true
);

public record ToolDefinitionDto(
    string Name,
    string Description,
    Dictionary<string, object> ParametersSchema
);

public record ChatCompletionRequest(
    IReadOnlyList<ChatMessageDto> Messages,
    string? Model = null,
    double Temperature = 0.1,
    int MaxTokens = 2048,
    IReadOnlyList<ToolDefinitionDto>? Tools = null,
    bool Stream = false
);

public record ChatCompletionResult(
    string ResponseText,
    IReadOnlyList<ChatToolCallDto> ToolCalls,
    string FinishReason,
    int PromptTokens,
    int CompletionTokens,
    string ModelName,
    AiProviderType ProviderType
);

public record ChatCompletionChunk(
    string DeltaText,
    ChatToolCallDto? ToolCallDelta = null,
    string? FinishReason = null,
    bool IsCompleted = false
);

public record AiChatRequestDto(
    Guid? SessionId,
    string Message,
    AiProviderType? PreferredProvider = null,
    string? PreferredModel = null
);

public record CitationPillDto(
    string DocumentTitle,
    string PageOrSection,
    float RelevanceScore,
    string OneDrivePreviewUrl
);

public record AiChatResponseDto(
    Guid SessionId,
    string ResponseText,
    string ModelName,
    AiProviderType Provider,
    int ResponseTimeMs,
    IReadOnlyList<CitationPillDto> Citations,
    IReadOnlyList<string> ToolsInvoked,
    Guid LogId
);

public record AiChatStreamEventDto(
    string EventType, // "delta", "citation", "tool_call", "done", "error"
    string Content,
    CitationPillDto? Citation = null
);

public record DocumentChunkDto(
    int ChunkIndex,
    string Text,
    int PageNumber,
    string SectionTitle
);

public record QdrantVectorPoint(
    string Id,
    float[] Vector,
    Dictionary<string, object> Payload
);

public record QdrantScoredPoint(
    string Id,
    float Score,
    Dictionary<string, object> Payload
);

public record KnowledgeDocumentDto(
    Guid Id,
    string Title,
    KnowledgeDocumentType DocumentType,
    string SourceFilePath,
    string QdrantCollectionName,
    int ChunkCount,
    string EmbeddingModel,
    IndexingStatus Status,
    string? ErrorMessage,
    bool IsActive,
    DateTime IndexedAt,
    DateTime CreatedAt
);

public record ConversationLogDto(
    Guid Id,
    Guid SessionId,
    Guid UserId,
    AiProviderType Provider,
    string ModelName,
    string UserQuery,
    string AiResponse,
    string? ToolsCalledJson,
    int ResponseTimeMs,
    int? UserFeedback,
    string? FeedbackComment,
    DateTime CreatedAt
);

public record AiProviderConfigurationDto(
    Guid Id,
    AiProviderType ProviderType,
    string DisplayName,
    string ModelName,
    string? ApiEndpoint,
    bool HasApiKeyConfigured,
    double Temperature,
    int MaxTokens,
    bool IsDefault,
    bool IsActive
);

public record SaveAiProviderConfigRequest(
    AiProviderType ProviderType,
    string DisplayName,
    string ModelName,
    string? ApiEndpoint,
    string? PlainApiKey,
    double Temperature,
    int MaxTokens,
    bool IsDefault
);

