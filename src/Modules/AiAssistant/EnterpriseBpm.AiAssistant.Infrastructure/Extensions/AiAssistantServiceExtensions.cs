using EnterpriseBpm.AiAssistant.Application.Contracts;
using EnterpriseBpm.AiAssistant.Application.Services;
using EnterpriseBpm.AiAssistant.Application.Tools;
using EnterpriseBpm.AiAssistant.Infrastructure.Persistence;
using EnterpriseBpm.AiAssistant.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace EnterpriseBpm.AiAssistant.Infrastructure.Extensions;

public static class AiAssistantServiceExtensions
{
    public static IServiceCollection AddAiAssistantModule(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Chuỗi kết nối cơ sở dữ liệu 'DefaultConnection' chưa được cấu hình.");

        services.AddDbContext<AiDbContext>(options =>
            options.UseSqlServer(connectionString, sql =>
            {
                sql.MigrationsHistoryTable("__EFMigrationsHistory_Ai", "ai");
                sql.EnableRetryOnFailure(3, TimeSpan.FromSeconds(5), null);
            }));

        // HTTP Client Factory
        services.AddHttpClient("AiClient", client =>
        {
            client.Timeout = TimeSpan.FromSeconds(60);
        });

        // RAG & Vector Services
        services.AddHttpClient<IQdrantVectorService, QdrantVectorService>();
        services.AddHttpClient<IEmbeddingGeneratorService, EmbeddingGeneratorService>();
        services.AddSingleton<IDocumentChunkingService, DocumentChunkingService>();

        // C# Tools
        services.AddScoped<IChatTool, PolicyRagTool>();
        services.AddScoped<IChatTool, MyPendingTasksTool>();
        services.AddScoped<IChatTool, WorkflowStatsTool>();
        services.AddScoped<IUnifiedToolRegistry, UnifiedToolRegistry>();

        // AI Provider Factory & Orchestrators
        services.AddScoped<IAiProviderFactory, AiProviderFactory>();
        services.AddScoped<IAiAssistantService, AiAssistantService>();
        services.AddScoped<IKnowledgeService, KnowledgeService>();
        services.AddScoped<IConversationLoggingService, ConversationLoggingService>();
        services.AddScoped<IAiProviderConfigService, AiProviderConfigService>();

        return services;
    }
}

