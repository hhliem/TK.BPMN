using EnterpriseBpm.AiAssistant.Domain.Entities;
using EnterpriseBpm.AiAssistant.Domain.Enums;
using EnterpriseBpm.SharedKernel.Contracts;
using Microsoft.EntityFrameworkCore;

namespace EnterpriseBpm.AiAssistant.Infrastructure.Persistence;

public class AiDbContext : DbContext
{
    private readonly ICurrentUserService? _currentUserService;

    public DbSet<KnowledgeDocument> KnowledgeDocuments => Set<KnowledgeDocument>();
    public DbSet<ConversationLog> ConversationLogs => Set<ConversationLog>();
    public DbSet<AiProviderConfiguration> AiProviderConfigurations => Set<AiProviderConfiguration>();

    public AiDbContext(
        DbContextOptions<AiDbContext> options,
        ICurrentUserService? currentUserService = null)
        : base(options)
    {
        _currentUserService = currentUserService;
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.HasDefaultSchema("ai");

        // 1. KnowledgeDocuments
        modelBuilder.Entity<KnowledgeDocument>(b =>
        {
            b.ToTable("KnowledgeDocuments");
            b.HasKey(x => x.Id);
            b.Property(x => x.Title).HasMaxLength(255).IsRequired();
            b.Property(x => x.DocumentType).HasConversion<string>().HasMaxLength(50).IsRequired();
            b.Property(x => x.SourceFilePath).HasMaxLength(500).IsRequired();
            b.Property(x => x.QdrantCollectionName).HasMaxLength(100).IsRequired();
            b.Property(x => x.EmbeddingModel).HasMaxLength(100).IsRequired();
            b.Property(x => x.Status).HasConversion<string>().HasMaxLength(50).IsRequired();
            b.Property(x => x.ErrorMessage).HasMaxLength(1000);

            b.HasIndex(x => new { x.IsActive, x.Status })
                .HasDatabaseName("IX_KnowledgeDocuments_ActiveStatus");
        });

        // 2. ConversationLogs
        modelBuilder.Entity<ConversationLog>(b =>
        {
            b.ToTable("ConversationLogs");
            b.HasKey(x => x.Id);
            b.Property(x => x.Provider).HasConversion<string>().HasMaxLength(50).IsRequired();
            b.Property(x => x.ModelName).HasMaxLength(100).IsRequired();
            b.Property(x => x.UserQuery).IsRequired();
            b.Property(x => x.AiResponse).IsRequired();
            b.Property(x => x.ToolsCalledJson);
            b.Property(x => x.FeedbackComment).HasMaxLength(500);

            b.HasIndex(x => new { x.UserId, x.CreatedAt })
                .HasDatabaseName("IX_ConversationLogs_User");
        });

        // 3. AiProviderConfigurations
        modelBuilder.Entity<AiProviderConfiguration>(b =>
        {
            b.ToTable("AiProviderConfigurations");
            b.HasKey(x => x.Id);
            b.Property(x => x.ProviderType).HasConversion<string>().HasMaxLength(50).IsRequired();
            b.Property(x => x.DisplayName).HasMaxLength(100).IsRequired();
            b.Property(x => x.ModelName).HasMaxLength(100).IsRequired();
            b.Property(x => x.ApiEndpoint).HasMaxLength(500);
            b.Property(x => x.EncryptedApiKey);

            b.HasIndex(x => x.ProviderType)
                .IsUnique()
                .HasDatabaseName("IX_AiProviderConfigs_Type");
        });
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        var currentUserId = _currentUserService?.UserId;
        var now = DateTime.UtcNow;

        foreach (var entry in ChangeTracker.Entries<IAuditable>())
        {
            if (entry.State == EntityState.Added)
            {
                entry.Entity.CreatedAt = now;
                entry.Entity.CreatedBy = currentUserId;
            }
            else if (entry.State == EntityState.Modified)
            {
                entry.Entity.UpdatedAt = now;
                entry.Entity.UpdatedBy = currentUserId;
            }
        }

        return base.SaveChangesAsync(cancellationToken);
    }
}

