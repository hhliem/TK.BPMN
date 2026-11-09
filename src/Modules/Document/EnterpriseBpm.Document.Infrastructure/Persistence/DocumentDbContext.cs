using EnterpriseBpm.Document.Application.Handlers;
using EnterpriseBpm.Document.Domain.Entities;
using EnterpriseBpm.Document.Domain.Enums;
using EnterpriseBpm.SharedKernel.Contracts;
using EnterpriseBpm.SharedKernel.Extensions;
using EnterpriseBpm.SharedKernel.Interceptors;
using Microsoft.EntityFrameworkCore;

namespace EnterpriseBpm.Document.Infrastructure.Persistence;

public class DocumentDbContext : DbContext, IDocumentDbContextProvider
{
    private readonly ICurrentUserService? _currentUserService;

    public DocumentDbContext(
        DbContextOptions<DocumentDbContext> options,
        ICurrentUserService? currentUserService = null)
        : base(options)
    {
        _currentUserService = currentUserService;
    }

    public DbSet<WorkflowAttachment> WorkflowAttachments => Set<WorkflowAttachment>();
    public DbSet<DocumentTemplate> DocumentTemplates => Set<DocumentTemplate>();
    public DbSet<GeneratedDocument> GeneratedDocuments => Set<GeneratedDocument>();

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        base.OnConfiguring(optionsBuilder);
        if (_currentUserService != null)
        {
            optionsBuilder.AddInterceptors(new AuditableFieldsInterceptor(_currentUserService));
        }
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.HasDefaultSchema("document");

        // Áp dụng Global Query Filter cho Soft Delete
        modelBuilder.ApplySoftDeleteQueryFilter();

        // 1. WorkflowAttachments
        modelBuilder.Entity<WorkflowAttachment>(builder =>
        {
            builder.ToTable("WorkflowAttachments");
            builder.HasKey(a => a.Id);

            builder.Property(a => a.WorkflowInstanceId)
                .HasMaxLength(100)
                .IsRequired();

            builder.Property(a => a.FormKey)
                .HasMaxLength(100)
                .IsRequired();

            builder.Property(a => a.OriginalFileName)
                .HasMaxLength(255)
                .IsRequired();

            builder.Property(a => a.FileExtension)
                .HasMaxLength(20)
                .IsRequired();

            builder.Property(a => a.ContentType)
                .HasMaxLength(100)
                .IsRequired();

            builder.Property(a => a.FileSizeBytes)
                .IsRequired();

            builder.Property(a => a.OneDriveItemId)
                .HasMaxLength(255)
                .IsRequired();

            builder.Property(a => a.OneDriveWebUrl)
                .HasMaxLength(1000)
                .IsRequired();

            builder.HasIndex(a => a.WorkflowInstanceId)
                .HasDatabaseName("IX_Attachments_Workflow");
        });

        // 2. DocumentTemplates
        modelBuilder.Entity<DocumentTemplate>(builder =>
        {
            builder.ToTable("DocumentTemplates");
            builder.HasKey(t => t.Id);

            builder.Property(t => t.TemplateKey)
                .HasMaxLength(100)
                .IsRequired();
            builder.HasIndex(t => t.TemplateKey)
                .IsUnique()
                .HasDatabaseName("UX_DocumentTemplates_Key");

            builder.Property(t => t.Title)
                .HasMaxLength(255)
                .IsRequired();

            builder.Property(t => t.Description)
                .HasMaxLength(500);

            builder.Property(t => t.Format)
                .HasConversion(
                    v => v.ToString(),
                    v => Enum.Parse<TemplateFormat>(v))
                .HasMaxLength(20)
                .IsRequired();

            builder.Property(t => t.OneDriveTemplateItemId)
                .HasMaxLength(255)
                .IsRequired();

            builder.Property(t => t.PlaceholderSchema)
                .IsRequired();

            builder.HasMany(t => t.GeneratedDocuments)
                .WithOne(g => g.Template)
                .HasForeignKey(g => g.TemplateId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // 3. GeneratedDocuments
        modelBuilder.Entity<GeneratedDocument>(builder =>
        {
            builder.ToTable("GeneratedDocuments");
            builder.HasKey(g => g.Id);

            builder.Property(g => g.WorkflowInstanceId)
                .HasMaxLength(100)
                .IsRequired();

            builder.Property(g => g.GeneratedFileName)
                .HasMaxLength(255)
                .IsRequired();

            builder.Property(g => g.OneDriveItemId)
                .HasMaxLength(255)
                .IsRequired();

            builder.Property(g => g.OneDriveWebUrl)
                .HasMaxLength(1000)
                .IsRequired();

            builder.Property(g => g.ContentSha256Hash)
                .HasMaxLength(64)
                .IsRequired();

            builder.HasIndex(g => g.WorkflowInstanceId)
                .HasDatabaseName("IX_GeneratedDocs_Workflow");
        });
    }
}

