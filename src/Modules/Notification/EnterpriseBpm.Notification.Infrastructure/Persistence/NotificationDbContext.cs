using EnterpriseBpm.Notification.Domain.Entities;
using EnterpriseBpm.Notification.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace EnterpriseBpm.Notification.Infrastructure.Persistence;

public class NotificationDbContext : DbContext
{
    public const string Schema = "notification";

    public DbSet<NotificationTemplate> NotificationTemplates => Set<NotificationTemplate>();
    public DbSet<NotificationLog> NotificationLogs => Set<NotificationLog>();

    public NotificationDbContext(DbContextOptions<NotificationDbContext> options)
        : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.HasDefaultSchema(Schema);

        modelBuilder.Entity<NotificationTemplate>(entity =>
        {
            entity.ToTable("NotificationTemplates", Schema);
            entity.HasKey(e => e.Id);
            entity.Property(e => e.TemplateCode).IsRequired().HasMaxLength(100);
            entity.HasIndex(e => e.TemplateCode).IsUnique();
            entity.Property(e => e.Channel).HasConversion<string>().HasMaxLength(50);
            entity.Property(e => e.SubjectTemplate).HasMaxLength(255);
            entity.Property(e => e.BodyTemplate).IsRequired();
        });

        modelBuilder.Entity<NotificationLog>(entity =>
        {
            entity.ToTable("NotificationLogs", Schema);
            entity.HasKey(e => e.Id);
            entity.Property(e => e.TemplateCode).IsRequired().HasMaxLength(100);
            entity.Property(e => e.RecipientEmail).HasMaxLength(255);
            entity.Property(e => e.RecipientPhone).HasMaxLength(50);
            entity.Property(e => e.Channel).HasConversion<string>().HasMaxLength(50);
            entity.Property(e => e.Status).HasConversion<string>().HasMaxLength(50);

            entity.HasIndex(e => new { e.Status, e.RetryCount })
                  .HasDatabaseName("IX_NotificationLogs_Retry");

            entity.HasIndex(e => new { e.SourceEventId, e.TemplateCode, e.RecipientEmail })
                  .IsUnique()
                  .HasFilter("[SourceEventId] IS NOT NULL")
                  .HasDatabaseName("IX_NotificationLogs_Dedup");
        });
    }
}

