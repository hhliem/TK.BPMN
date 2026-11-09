using EnterpriseBpm.Form.Domain.Entities;
using EnterpriseBpm.SharedKernel.Extensions;
using Microsoft.EntityFrameworkCore;

namespace EnterpriseBpm.Form.Infrastructure.Persistence;

public class FormDbContext : DbContext
{
    public const string Schema = "form";

    public DbSet<FormDefinition> FormDefinitions => Set<FormDefinition>();
    public DbSet<MasterCategory> MasterCategories => Set<MasterCategory>();
    public DbSet<MasterItem> MasterItems => Set<MasterItem>();

    public FormDbContext(DbContextOptions<FormDbContext> options) : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.HasDefaultSchema(Schema);

        // Apply Soft Delete nếu có
        modelBuilder.ApplySoftDeleteQueryFilter();

        // FormDefinitions
        modelBuilder.Entity<FormDefinition>(b =>
        {
            b.ToTable("FormDefinitions");
            b.HasKey(f => f.Id);

            // Filtered Unique Index: Mỗi FormKey chỉ có đúng 1 bản ghi IsLatest=true
            b.HasIndex(f => f.FormKey)
                .IsUnique()
                .HasFilter("[IsLatest] = 1")
                .HasDatabaseName("UX_FormDefinitions_LatestPerFormKey");

            // Index tra cứu lịch sử phiên bản
            b.HasIndex(f => new { f.FormKey, f.Version })
                .HasDatabaseName("IX_FormDefinitions_VersionHistory");

            b.Property(f => f.FormKey).HasMaxLength(100).IsRequired();
            b.Property(f => f.Title).HasMaxLength(255).IsRequired();
            b.Property(f => f.SchemaJson).IsRequired();
            b.Property(f => f.ChangeNotes).HasMaxLength(500);
        });

        // MasterCategories
        modelBuilder.Entity<MasterCategory>(b =>
        {
            b.ToTable("MasterCategories");
            b.HasKey(c => c.Id);
            b.HasIndex(c => c.CategoryKey).IsUnique();
            b.Property(c => c.CategoryKey).HasMaxLength(50).IsRequired();
            b.Property(c => c.Name).HasMaxLength(255).IsRequired();
            b.Property(c => c.Description).HasMaxLength(500);
        });

        // MasterItems
        modelBuilder.Entity<MasterItem>(b =>
        {
            b.ToTable("MasterItems");
            b.HasKey(i => i.Id);

            // Non-clustered Index tối ưu hóa truy vấn Cascading Loading < 10ms
            b.HasIndex(i => new { i.CategoryId, i.ParentId, i.IsActive })
                .HasDatabaseName("IX_MasterItems_CascadingQuery")
                .IncludeProperties(i => new { i.Code, i.Name, i.DisplayOrder });

            b.Property(i => i.Code).HasMaxLength(50).IsRequired();
            b.Property(i => i.Name).HasMaxLength(255).IsRequired();

            b.HasOne(i => i.Category)
                .WithMany(c => c.Items)
                .HasForeignKey(i => i.CategoryId)
                .OnDelete(DeleteBehavior.Cascade);

            b.HasOne(i => i.Parent)
                .WithMany(p => p.Children)
                .HasForeignKey(i => i.ParentId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }
}

