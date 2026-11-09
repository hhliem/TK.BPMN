using EnterpriseBpm.AiAssistant.Application.DTOs;
using EnterpriseBpm.AiAssistant.Domain.Entities;
using EnterpriseBpm.AiAssistant.Domain.Enums;

namespace EnterpriseBpm.AiAssistant.Application.Contracts;

public interface IAiChatClient
{
    AiProviderType ProviderType { get; }
    Task<ChatCompletionResult> CompleteChatAsync(ChatCompletionRequest request, CancellationToken cancellationToken = default);
    IAsyncEnumerable<ChatCompletionChunk> StreamChatAsync(ChatCompletionRequest request, CancellationToken cancellationToken = default);
}

public interface IAiProviderFactory
{
    Task<IAiChatClient> GetClientAsync(AiProviderType? providerType = null, CancellationToken cancellationToken = default);
    Task<AiProviderConfigurationDto> GetDefaultProviderConfigAsync(CancellationToken cancellationToken = default);
}

public interface IChatTool
{
    string Name { get; }
    string Description { get; }
    ToolDefinitionDto Definition { get; }
    Task<string> ExecuteAsync(string argumentsJson, CancellationToken cancellationToken = default);
}

public interface IUnifiedToolRegistry
{
    void RegisterTool(IChatTool tool);
    IReadOnlyCollection<ToolDefinitionDto> GetToolDefinitions();
    IChatTool? GetTool(string toolName);
    Task<string> ExecuteToolAsync(string toolName, string argumentsJson, CancellationToken cancellationToken = default);
}

public interface IQdrantVectorService
{
    Task EnsureCollectionAsync(string collectionName, int vectorSize = 768, CancellationToken cancellationToken = default);
    Task UpsertPointsAsync(string collectionName, IEnumerable<QdrantVectorPoint> points, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<QdrantScoredPoint>> SearchAsync(string collectionName, float[] queryVector, int topK = 5, float scoreThreshold = 0.7f, CancellationToken cancellationToken = default);
    Task DeletePointsByFilterAsync(string collectionName, string key, string value, CancellationToken cancellationToken = default);
}

public interface IDocumentChunkingService
{
    IReadOnlyList<DocumentChunkDto> ChunkDocument(string text, string documentTitle, int chunkSize = 500, int overlap = 50);
}

public interface IEmbeddingGeneratorService
{
    Task<float[]> GenerateEmbeddingAsync(string text, string? modelName = null, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<float[]>> GenerateBatchEmbeddingsAsync(IReadOnlyList<string> texts, string? modelName = null, CancellationToken cancellationToken = default);
}

public interface IAiAssistantService
{
    Task<AiChatResponseDto> ChatAsync(AiChatRequestDto request, Guid userId, CancellationToken cancellationToken = default);
    IAsyncEnumerable<AiChatStreamEventDto> StreamChatAsync(AiChatRequestDto request, Guid userId, CancellationToken cancellationToken = default);
}

public interface IKnowledgeService
{
    Task<Guid> UploadAndIndexDocumentAsync(Stream fileStream, string fileName, KnowledgeDocumentType type, Guid userId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<KnowledgeDocumentDto>> GetKnowledgeDocumentsAsync(CancellationToken cancellationToken = default);
    Task<KnowledgeDocumentDto?> GetDocumentByIdAsync(Guid id, CancellationToken cancellationToken = default);
}

public interface IConversationLoggingService
{
    Task LogConversationAsync(ConversationLog log, CancellationToken cancellationToken = default);
    Task RecordFeedbackAsync(Guid logId, int feedback, string? comment = null, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ConversationLogDto>> GetRecentLogsAsync(int limit = 50, CancellationToken cancellationToken = default);
}

public interface IAiProviderConfigService
{
    Task<IReadOnlyList<AiProviderConfigurationDto>> GetAllConfigsAsync(CancellationToken cancellationToken = default);
    Task SetDefaultProviderAsync(AiProviderType providerType, CancellationToken cancellationToken = default);
    Task SaveConfigAsync(SaveAiProviderConfigRequest request, Guid? updatedBy = null, CancellationToken cancellationToken = default);
}

