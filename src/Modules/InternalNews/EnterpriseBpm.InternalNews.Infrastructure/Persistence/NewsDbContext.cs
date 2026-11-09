using EnterpriseBpm.InternalNews.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace EnterpriseBpm.InternalNews.Infrastructure.Persistence;

public class NewsDbContext : DbContext
{
    public const string Schema = "news";

    public DbSet<NewsArticle> NewsArticles => Set<NewsArticle>();
    public DbSet<NewsReadReceipt> NewsReadReceipts => Set<NewsReadReceipt>();

    public NewsDbContext(DbContextOptions<NewsDbContext> options)
        : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.HasDefaultSchema(Schema);

        modelBuilder.Entity<NewsArticle>(entity =>
        {
            entity.ToTable("NewsArticles", Schema);
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Title).IsRequired().HasMaxLength(255);
            entity.Property(e => e.Summary).IsRequired().HasMaxLength(500);
            entity.Property(e => e.ContentHtml).IsRequired();
            entity.Property(e => e.CoverImageUrl).HasMaxLength(500);
            entity.Property(e => e.Category).HasConversion<string>().HasMaxLength(50);
            entity.Property(e => e.Status).HasConversion<string>().HasMaxLength(50);

            entity.HasIndex(e => new { e.Status, e.PublishedAt })
                  .HasDatabaseName("IX_News_PublishedLookup");

            entity.HasIndex(e => new { e.ScheduledPublishAt, e.Status })
                  .HasDatabaseName("IX_News_ScheduledLookup");

            entity.HasMany(e => e.ReadReceipts)
                  .WithOne(r => r.Article)
                  .HasForeignKey(r => r.ArticleId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<NewsReadReceipt>(entity =>
        {
            entity.ToTable("NewsReadReceipts", Schema);
            entity.HasKey(e => new { e.ArticleId, e.EmployeeId });
            entity.Property(e => e.ReadAt).IsRequired();
        });
    }
}

