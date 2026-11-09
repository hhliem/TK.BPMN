using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text.Json;
using EnterpriseBpm.AiAssistant.Application.Contracts;
using EnterpriseBpm.AiAssistant.Application.DTOs;
using EnterpriseBpm.AiAssistant.Domain.Entities;
using EnterpriseBpm.AiAssistant.Domain.Enums;
using EnterpriseBpm.AiAssistant.Infrastructure.Clients;
using EnterpriseBpm.AiAssistant.Infrastructure.Persistence;
using EnterpriseBpm.Document.Application.Contracts;
using EnterpriseBpm.SystemSettings.Application.Contracts;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace EnterpriseBpm.AiAssistant.Infrastructure.Services;

/// <summary>
/// Factory quản lý và giải quyết đa nhà cung cấp AI linh hoạt (TSK-602)
/// </summary>
public class AiProviderFactory : IAiProviderFactory
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IConfiguration _configuration;
    private readonly ISettingEncryptionService _encryptionService;
    private readonly AiDbContext _dbContext;
    private readonly ILoggerFactory _loggerFactory;
    private readonly ILogger<AiProviderFactory> _logger;

    public AiProviderFactory(
        IHttpClientFactory httpClientFactory,
        IConfiguration configuration,
        ISettingEncryptionService encryptionService,
        AiDbContext dbContext,
        ILoggerFactory loggerFactory,
        ILogger<AiProviderFactory> logger)
    {
        _httpClientFactory = httpClientFactory;
        _configuration = configuration;
        _encryptionService = encryptionService;
        _dbContext = dbContext;
        _loggerFactory = loggerFactory;
        _logger = logger;
    }

    public async Task<IAiChatClient> GetClientAsync(AiProviderType? providerType = null, CancellationToken cancellationToken = default)
    {
        var targetType = providerType;
        if (!targetType.HasValue)
        {
            var defConfig = await _dbContext.AiProviderConfigurations
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.IsDefault && c.IsActive, cancellationToken);

            targetType = defConfig?.ProviderType ?? AiProviderType.Gemini;
        }

        var config = await _dbContext.AiProviderConfigurations
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.ProviderType == targetType.Value, cancellationToken);

        var httpClient = _httpClientFactory.CreateClient("AiClient");
        var plainKey = !string.IsNullOrWhiteSpace(config?.EncryptedApiKey)
            ? _encryptionService.Decrypt(config.EncryptedApiKey)
            : _configuration[$"AiSettings:{targetType}:ApiKey"];

        return targetType.Value switch
        {
            AiProviderType.Gemini => new GeminiChatClient(
                httpClient,
                plainKey,
                config?.ModelName ?? _configuration["AiSettings:Gemini:Model"] ?? "gemini-1.5-flash",
                _loggerFactory.CreateLogger<GeminiChatClient>()),

            AiProviderType.OpenAi => new OpenAiChatClient(
                httpClient,
                plainKey,
                config?.ModelName ?? _configuration["AiSettings:OpenAi:Model"] ?? "gpt-4o-mini",
                _loggerFactory.CreateLogger<OpenAiChatClient>()),

            AiProviderType.Claude => new ClaudeChatClient(
                httpClient,
                plainKey,
                config?.ModelName ?? _configuration["AiSettings:Claude:Model"] ?? "claude-3-5-haiku-20241022",
                _loggerFactory.CreateLogger<ClaudeChatClient>()),

            AiProviderType.Ollama or AiProviderType.CustomMcp => new OllamaChatClient(
                httpClient,
                config?.ApiEndpoint ?? _configuration["AiSettings:LocalLlm:Endpoint"] ?? "http://localhost:11434",
                config?.ModelName ?? _configuration["AiSettings:LocalLlm:Model"] ?? "llama3:latest",
                _loggerFactory.CreateLogger<OllamaChatClient>()),

            _ => new GeminiChatClient(
                httpClient,
                plainKey,
                "gemini-1.5-flash",
                _loggerFactory.CreateLogger<GeminiChatClient>())
        };
    }

    public async Task<AiProviderConfigurationDto> GetDefaultProviderConfigAsync(CancellationToken cancellationToken = default)
    {
        var config = await _dbContext.AiProviderConfigurations
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.IsDefault && c.IsActive, cancellationToken);

        if (config == null)
        {
            return new AiProviderConfigurationDto(
                Id: Guid.Empty,
                ProviderType: AiProviderType.Gemini,
                DisplayName: "Google Gemini",
                ModelName: "gemini-1.5-flash",
                ApiEndpoint: "https://generativelanguage.googleapis.com",
                HasApiKeyConfigured: false,
                Temperature: 0.1,
                MaxTokens: 2048,
                IsDefault: true,
                IsActive: true
            );
        }

        return new AiProviderConfigurationDto(
            Id: config.Id,
            ProviderType: config.ProviderType,
            DisplayName: config.DisplayName,
            ModelName: config.ModelName,
            ApiEndpoint: config.ApiEndpoint,
            HasApiKeyConfigured: !string.IsNullOrWhiteSpace(config.EncryptedApiKey),
            Temperature: config.Temperature,
            MaxTokens: config.MaxTokens,
            IsDefault: config.IsDefault,
            IsActive: config.IsActive
        );
    }
}

/// <summary>
/// Dịch vụ Điều phối Trợ lý AI (AI Assistant Orchestrator)
/// </summary>
public class AiAssistantService : IAiAssistantService
{
    private readonly IAiProviderFactory _providerFactory;
    private readonly IUnifiedToolRegistry _toolRegistry;
    private readonly IConversationLoggingService _loggingService;
    private readonly ILogger<AiAssistantService> _logger;

    private const string SystemPrompt =
        "Bạn là Trợ lý AI Doanh Nghiệp (Enterprise AI Assistant) thông minh, tận tâm và bảo mật.\n" +
        "NHIỆM VỤ:\n" +
        "1. Trả lời các câu hỏi về quy trình, chính sách, quy chế và số liệu hồ sơ trình ký của công ty.\n" +
        "2. NGUYÊN TẮC BẢO MẬT: Tuyệt đối tuân thủ phân quyền RBAC/ABAC. Không bao giờ tiết lộ dữ liệu ngoài quyền hạn của người hỏi.\n" +
        "3. CHỐNG BỊA ĐẶT (HALLUCINATION GUARD): Khi tra cứu quy chế, chỉ trả lời dựa trên nội dung điều khoản trích xuất từ công cụ search_company_policy. Nếu không tìm thấy hoặc similarity score thấp, hãy trả lời trung thực: 'Không tìm thấy thông tin phù hợp trong quy chế nội bộ'.\n" +
        "4. ĐỊNH DẠNG: Trả lời ngắn gọn, rõ ràng, sử dụng Markdown và luôn ghi rõ điều khoản, số trang trích dẫn nếu có.";

    public AiAssistantService(
        IAiProviderFactory providerFactory,
        IUnifiedToolRegistry toolRegistry,
        IConversationLoggingService loggingService,
        ILogger<AiAssistantService> logger)
    {
        _providerFactory = providerFactory;
        _toolRegistry = toolRegistry;
        _loggingService = loggingService;
        _logger = logger;
    }

    public async Task<AiChatResponseDto> ChatAsync(AiChatRequestDto request, Guid userId, CancellationToken cancellationToken = default)
    {
        var sw = Stopwatch.StartNew();
        var client = await _providerFactory.GetClientAsync(request.PreferredProvider, cancellationToken);
        var tools = _toolRegistry.GetToolDefinitions().ToList();

        var messages = new List<ChatMessageDto>
        {
            new(ChatRole.System, SystemPrompt),
            new(ChatRole.User, request.Message)
        };

        var invokedTools = new List<string>();
        var citations = new List<CitationPillDto>();

        // Lặp gọi Tool (tối đa 3 vòng lặp)
        ChatCompletionResult? finalResult = null;
        for (int i = 0; i < 3; i++)
        {
            var compReq = new ChatCompletionRequest(
                Messages: messages,
                Model: request.PreferredModel,
                Temperature: 0.1,
                MaxTokens: 2048,
                Tools: tools
            );

            var result = await client.CompleteChatAsync(compReq, cancellationToken);
            finalResult = result;

            if (result.ToolCalls.Count == 0)
            {
                break;
            }

            foreach (var tc in result.ToolCalls)
            {
                invokedTools.Add(tc.Name);
                _logger.LogInformation("Thực thi Tool: {ToolName} với args: {Args}", tc.Name, tc.ArgumentsJson);

                var toolOutput = await _toolRegistry.ExecuteToolAsync(tc.Name, tc.ArgumentsJson, cancellationToken);

                // Nếu là RAG Policy, trích xuất citations
                if (tc.Name == "search_company_policy")
                {
                    ExtractCitations(toolOutput, citations);
                }

                messages.Add(new ChatMessageDto(ChatRole.Assistant, result.ResponseText));
                messages.Add(new ChatMessageDto(ChatRole.Tool, toolOutput, ToolCallId: tc.Id, ToolName: tc.Name));
            }
        }

        sw.Stop();
        var responseText = finalResult?.ResponseText ?? "Đã xử lý yêu cầu.";
        var sessionId = request.SessionId ?? Guid.NewGuid();

        // Ghi nhận nhật ký hội thoại bất biến
        var log = ConversationLog.Create(
            sessionId: sessionId,
            userId: userId,
            provider: client.ProviderType,
            modelName: finalResult?.ModelName ?? "unknown",
            userQuery: request.Message,
            aiResponse: responseText,
            toolsCalledJson: invokedTools.Count > 0 ? JsonSerializer.Serialize(invokedTools) : null,
            responseTimeMs: (int)sw.ElapsedMilliseconds,
            promptTokens: finalResult?.PromptTokens ?? 50,
            completionTokens: finalResult?.CompletionTokens ?? 50
        );

        await _loggingService.LogConversationAsync(log, cancellationToken);

        return new AiChatResponseDto(
            SessionId: sessionId,
            ResponseText: responseText,
            ModelName: finalResult?.ModelName ?? "unknown",
            Provider: client.ProviderType,
            ResponseTimeMs: (int)sw.ElapsedMilliseconds,
            Citations: citations,
            ToolsInvoked: invokedTools,
            LogId: log.Id
        );
    }

    public async IAsyncEnumerable<AiChatStreamEventDto> StreamChatAsync(
        AiChatRequestDto request,
        Guid userId,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var chatResponse = await ChatAsync(request, userId, cancellationToken);

        foreach (var citation in chatResponse.Citations)
        {
            yield return new AiChatStreamEventDto("citation", citation.DocumentTitle, citation);
        }

        var words = chatResponse.ResponseText.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        foreach (var word in words)
        {
            await Task.Delay(25, cancellationToken);
            yield return new AiChatStreamEventDto("delta", word + " ");
        }

        yield return new AiChatStreamEventDto("done", string.Empty);
    }

    private static void ExtractCitations(string toolOutput, List<CitationPillDto> citations)
    {
        try
        {
            using var doc = JsonDocument.Parse(toolOutput);
            if (doc.RootElement.TryGetProperty("results", out var resArr) && resArr.ValueKind == JsonValueKind.Array)
            {
                foreach (var item in resArr.EnumerateArray())
                {
                    var title = item.TryGetProperty("documentTitle", out var t) ? t.GetString() ?? "Quy chế" : "Quy chế";
                    var page = item.TryGetProperty("page", out var p) ? p.GetString() ?? "1" : "1";
                    var score = item.TryGetProperty("score", out var s) ? s.GetSingle() : 0.8f;

                    citations.Add(new CitationPillDto(
                        DocumentTitle: title,
                        PageOrSection: $"Trang {page}",
                        RelevanceScore: score,
                        OneDrivePreviewUrl: "#"
                    ));
                }
            }
        }
        catch
        {
            // Bỏ qua lỗi parse citation
        }
    }
}

/// <summary>
/// Dịch vụ Quản lý Tri thức Tài liệu RAG (TSK-609, TSK-610)
/// </summary>
public class KnowledgeService : IKnowledgeService
{
    private readonly AiDbContext _dbContext;
    private readonly IDocumentChunkingService _chunkingService;
    private readonly IEmbeddingGeneratorService _embeddingService;
    private readonly IQdrantVectorService _qdrantService;
    private readonly IExternalStorageClient _storageClient;
    private readonly ILogger<KnowledgeService> _logger;

    public KnowledgeService(
        AiDbContext dbContext,
        IDocumentChunkingService chunkingService,
        IEmbeddingGeneratorService embeddingService,
        IQdrantVectorService qdrantService,
        IExternalStorageClient storageClient,
        ILogger<KnowledgeService> logger)
    {
        _dbContext = dbContext;
        _chunkingService = chunkingService;
        _embeddingService = embeddingService;
        _qdrantService = qdrantService;
        _storageClient = storageClient;
        _logger = logger;
    }

    public async Task<Guid> UploadAndIndexDocumentAsync(
        Stream fileStream,
        string fileName,
        KnowledgeDocumentType type,
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        // 1. Lưu file lên OneDrive Storage (Zero-Binary-in-DB)
        var uploadResult = await _storageClient.UploadStreamAsync(
            relativePath: $"KnowledgeBase/{fileName}",
            stream: fileStream,
            contentType: "application/octet-stream",
            cancellationToken: cancellationToken);

        var docEntity = KnowledgeDocument.Create(
            title: Path.GetFileNameWithoutExtension(fileName),
            documentType: type,
            sourceFilePath: uploadResult.WebUrl,
            qdrantCollectionName: "company_policies",
            embeddingModel: "nomic-embed-text",
            createdBy: userId
        );

        _dbContext.KnowledgeDocuments.Add(docEntity);
        await _dbContext.SaveChangesAsync(cancellationToken);

        // 2. Chuyển sang trạng thái Processing
        docEntity.MarkProcessing();
        await _dbContext.SaveChangesAsync(cancellationToken);

        try
        {
            // 3. Đọc nội dung văn bản (hỗ trợ text/markdown/pdf trích xuất)
            fileStream.Position = 0;
            using var reader = new StreamReader(fileStream, System.Text.Encoding.UTF8);
            var text = await reader.ReadToEndAsync(cancellationToken);

            // 4. Bẻ chunk 500 tokens (overlap 50 tokens)
            var chunks = _chunkingService.ChunkDocument(text, docEntity.Title, 500, 50);

            // 5. Đảm bảo collection Qdrant tồn tại
            await _qdrantService.EnsureCollectionAsync(docEntity.QdrantCollectionName, 768, cancellationToken);

            // 6. Sinh vector nhúng và nạp vào Qdrant
            var points = new List<QdrantVectorPoint>();
            foreach (var chunk in chunks)
            {
                var vector = await _embeddingService.GenerateEmbeddingAsync(chunk.Text, docEntity.EmbeddingModel, cancellationToken);
                var payload = new Dictionary<string, object>
                {
                    ["documentId"] = docEntity.Id.ToString(),
                    ["documentTitle"] = docEntity.Title,
                    ["documentType"] = docEntity.DocumentType.ToString(),
                    ["page"] = chunk.PageNumber.ToString(),
                    ["section"] = chunk.SectionTitle,
                    ["text"] = chunk.Text
                };

                points.Add(new QdrantVectorPoint(Guid.NewGuid().ToString("N"), vector, payload));
            }

            await _qdrantService.UpsertPointsAsync(docEntity.QdrantCollectionName, points, cancellationToken);

            // 7. Hoàn tất đánh chỉ mục
            docEntity.MarkCompleted(chunks.Count);
            await _dbContext.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("Đã hoàn tất đánh chỉ mục {Count} chunks cho tài liệu {DocId}", chunks.Count, docEntity.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi trong quá trình đánh chỉ mục tài liệu {DocId}", docEntity.Id);
            docEntity.MarkFailed(ex.Message);
            await _dbContext.SaveChangesAsync(cancellationToken);
        }

        return docEntity.Id;
    }

    public async Task<IReadOnlyList<KnowledgeDocumentDto>> GetKnowledgeDocumentsAsync(CancellationToken cancellationToken = default)
    {
        var docs = await _dbContext.KnowledgeDocuments
            .AsNoTracking()
            .OrderByDescending(d => d.IndexedAt)
            .ToListAsync(cancellationToken);

        return docs.Select(d => new KnowledgeDocumentDto(
            Id: d.Id,
            Title: d.Title,
            DocumentType: d.DocumentType,
            SourceFilePath: d.SourceFilePath,
            QdrantCollectionName: d.QdrantCollectionName,
            ChunkCount: d.ChunkCount,
            EmbeddingModel: d.EmbeddingModel,
            Status: d.Status,
            ErrorMessage: d.ErrorMessage,
            IsActive: d.IsActive,
            IndexedAt: d.IndexedAt,
            CreatedAt: d.CreatedAt
        )).ToList();
    }

    public async Task<KnowledgeDocumentDto?> GetDocumentByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var d = await _dbContext.KnowledgeDocuments
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

        if (d == null) return null;

        return new KnowledgeDocumentDto(
            Id: d.Id,
            Title: d.Title,
            DocumentType: d.DocumentType,
            SourceFilePath: d.SourceFilePath,
            QdrantCollectionName: d.QdrantCollectionName,
            ChunkCount: d.ChunkCount,
            EmbeddingModel: d.EmbeddingModel,
            Status: d.Status,
            ErrorMessage: d.ErrorMessage,
            IsActive: d.IsActive,
            IndexedAt: d.IndexedAt,
            CreatedAt: d.CreatedAt
        );
    }
}

/// <summary>
/// Dịch vụ Ghi nhận Lịch sử Hội thoại & Phản hồi (TSK-614)
/// </summary>
public class ConversationLoggingService : IConversationLoggingService
{
    private readonly AiDbContext _dbContext;
    private readonly ILogger<ConversationLoggingService> _logger;

    public ConversationLoggingService(AiDbContext dbContext, ILogger<ConversationLoggingService> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task LogConversationAsync(ConversationLog log, CancellationToken cancellationToken = default)
    {
        try
        {
            _dbContext.ConversationLogs.Add(log);
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi lưu ConversationLog");
        }
    }

    public async Task RecordFeedbackAsync(Guid logId, int feedback, string? comment = null, CancellationToken cancellationToken = default)
    {
        var log = await _dbContext.ConversationLogs.FirstOrDefaultAsync(l => l.Id == logId, cancellationToken);
        if (log != null)
        {
            log.SetFeedback(feedback, comment);
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
    }

    public async Task<IReadOnlyList<ConversationLogDto>> GetRecentLogsAsync(int limit = 50, CancellationToken cancellationToken = default)
    {
        var logs = await _dbContext.ConversationLogs
            .AsNoTracking()
            .OrderByDescending(l => l.CreatedAt)
            .Take(limit)
            .ToListAsync(cancellationToken);

        return logs.Select(l => new ConversationLogDto(
            Id: l.Id,
            SessionId: l.SessionId,
            UserId: l.UserId,
            Provider: l.Provider,
            ModelName: l.ModelName,
            UserQuery: l.UserQuery,
            AiResponse: l.AiResponse,
            ToolsCalledJson: l.ToolsCalledJson,
            ResponseTimeMs: l.ResponseTimeMs,
            UserFeedback: l.UserFeedback,
            FeedbackComment: l.FeedbackComment,
            CreatedAt: l.CreatedAt
        )).ToList();
    }
}

/// <summary>
/// Dịch vụ Quản trị Cấu hình Nhà cung cấp AI (TSK-613, TSK-618)
/// </summary>
public class AiProviderConfigService : IAiProviderConfigService
{
    private readonly AiDbContext _dbContext;
    private readonly ISettingEncryptionService _encryptionService;
    private readonly ILogger<AiProviderConfigService> _logger;

    public AiProviderConfigService(
        AiDbContext dbContext,
        ISettingEncryptionService encryptionService,
        ILogger<AiProviderConfigService> logger)
    {
        _dbContext = dbContext;
        _encryptionService = encryptionService;
        _logger = logger;
    }

    public async Task<IReadOnlyList<AiProviderConfigurationDto>> GetAllConfigsAsync(CancellationToken cancellationToken = default)
    {
        var configs = await _dbContext.AiProviderConfigurations
            .AsNoTracking()
            .OrderBy(c => c.ProviderType)
            .ToListAsync(cancellationToken);

        return configs.Select(c => new AiProviderConfigurationDto(
            Id: c.Id,
            ProviderType: c.ProviderType,
            DisplayName: c.DisplayName,
            ModelName: c.ModelName,
            ApiEndpoint: c.ApiEndpoint,
            HasApiKeyConfigured: !string.IsNullOrWhiteSpace(c.EncryptedApiKey),
            Temperature: c.Temperature,
            MaxTokens: c.MaxTokens,
            IsDefault: c.IsDefault,
            IsActive: c.IsActive
        )).ToList();
    }

    public async Task SetDefaultProviderAsync(AiProviderType providerType, CancellationToken cancellationToken = default)
    {
        var configs = await _dbContext.AiProviderConfigurations.ToListAsync(cancellationToken);
        foreach (var c in configs)
        {
            c.SetDefault(c.ProviderType == providerType);
        }
        await _dbContext.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("Đã chuyển đổi Provider mặc định sang: {Provider}", providerType);
    }

    public async Task SaveConfigAsync(SaveAiProviderConfigRequest request, Guid? updatedBy = null, CancellationToken cancellationToken = default)
    {
        var config = await _dbContext.AiProviderConfigurations
            .FirstOrDefaultAsync(c => c.ProviderType == request.ProviderType, cancellationToken);

        string? encryptedKey = null;
        if (!string.IsNullOrWhiteSpace(request.PlainApiKey))
        {
            encryptedKey = _encryptionService.Encrypt(request.PlainApiKey);
        }

        if (config == null)
        {
            config = AiProviderConfiguration.Create(
                providerType: request.ProviderType,
                displayName: request.DisplayName,
                modelName: request.ModelName,
                apiEndpoint: request.ApiEndpoint,
                encryptedApiKey: encryptedKey,
                temperature: request.Temperature,
                maxTokens: request.MaxTokens,
                isDefault: request.IsDefault,
                createdBy: updatedBy
            );
            _dbContext.AiProviderConfigurations.Add(config);
        }
        else
        {
            config.Update(
                displayName: request.DisplayName,
                modelName: request.ModelName,
                apiEndpoint: request.ApiEndpoint,
                encryptedApiKey: encryptedKey,
                temperature: request.Temperature,
                maxTokens: request.MaxTokens,
                updatedBy: updatedBy
            );
            if (request.IsDefault)
            {
                var others = await _dbContext.AiProviderConfigurations
                    .Where(c => c.Id != config.Id)
                    .ToListAsync(cancellationToken);
                foreach (var o in others) o.SetDefault(false);
                config.SetDefault(true);
            }
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}
