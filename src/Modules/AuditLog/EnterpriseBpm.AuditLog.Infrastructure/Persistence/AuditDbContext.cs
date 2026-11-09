using EnterpriseBpm.AuditLog.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace EnterpriseBpm.AuditLog.Infrastructure.Persistence;

public class AuditDbContext : DbContext
{
    public const string Schema = "audit";

    public DbSet<AuditEntry> AuditEntries => Set<AuditEntry>();
    public DbSet<SecurityEvent> SecurityEvents => Set<SecurityEvent>();

    public AuditDbContext(DbContextOptions<AuditDbContext> options) : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.HasDefaultSchema(Schema);

        modelBuilder.Entity<AuditEntry>(entity =>
        {
            entity.ToTable("AuditEntries", Schema);
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedOnAdd();

            entity.Property(e => e.Action).HasMaxLength(50).IsRequired();
            entity.Property(e => e.EntityType).HasMaxLength(200).IsRequired();
            entity.Property(e => e.EntityId).HasMaxLength(100).IsRequired();
            entity.Property(e => e.ModuleName).HasMaxLength(50).IsRequired();
            entity.Property(e => e.UserName).HasMaxLength(255).IsRequired();
            entity.Property(e => e.IpAddress).HasMaxLength(50);
            entity.Property(e => e.UserAgent).HasMaxLength(500);

            entity.HasIndex(e => new { e.EntityType, e.EntityId, e.Timestamp })
                  .HasDatabaseName("IX_AuditEntries_EntityLookup");

            entity.HasIndex(e => new { e.UserId, e.Timestamp })
                  .HasDatabaseName("IX_AuditEntries_UserTimeline");

            entity.HasIndex(e => new { e.ModuleName, e.Timestamp })
                  .HasDatabaseName("IX_AuditEntries_ModuleLookup");

            entity.HasIndex(e => new { e.Action, e.Timestamp })
                  .HasDatabaseName("IX_AuditEntries_ActionLookup");
        });

        modelBuilder.Entity<SecurityEvent>(entity =>
        {
            entity.ToTable("SecurityEvents", Schema);
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedOnAdd();

            entity.Property(e => e.EventType).HasMaxLength(50).IsRequired();
            entity.Property(e => e.UserName).HasMaxLength(255);
            entity.Property(e => e.IpAddress).HasMaxLength(50);
            entity.Property(e => e.UserAgent).HasMaxLength(500);
            entity.Property(e => e.FailureReason).HasMaxLength(500);

            entity.HasIndex(e => new { e.UserId, e.Timestamp })
                  .HasDatabaseName("IX_SecurityEvents_UserTimeline");

            entity.HasIndex(e => new { e.EventType, e.Timestamp })
                  .HasDatabaseName("IX_SecurityEvents_Type");
        });
    }
}

