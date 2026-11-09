using EnterpriseBpm.SystemSettings.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace EnterpriseBpm.SystemSettings.Infrastructure.Persistence;

public class SettingsDbContext : DbContext
{
    public const string Schema = "settings";

    public DbSet<SystemSetting> SystemSettings => Set<SystemSetting>();
    public DbSet<SettingChangeHistory> SettingChangeHistories => Set<SettingChangeHistory>();

    public SettingsDbContext(DbContextOptions<SettingsDbContext> options)
        : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.HasDefaultSchema(Schema);

        modelBuilder.Entity<SystemSetting>(entity =>
        {
            entity.ToTable("SystemSettings", Schema);
            entity.HasKey(e => e.Id);
            entity.Property(e => e.SettingKey).IsRequired().HasMaxLength(100);
            entity.HasIndex(e => e.SettingKey).IsUnique();
            entity.Property(e => e.SettingValue).IsRequired();
            entity.Property(e => e.Description).HasMaxLength(500);
            entity.Property(e => e.DataType).HasConversion<string>().HasMaxLength(50);
        });

        modelBuilder.Entity<SettingChangeHistory>(entity =>
        {
            entity.ToTable("SettingChangeHistory", Schema);
            entity.HasKey(e => e.Id);
            entity.Property(e => e.SettingKey).IsRequired().HasMaxLength(100);
            entity.Property(e => e.NewValue).IsRequired();
            entity.Property(e => e.ChangedByUserName).IsRequired().HasMaxLength(255);
            entity.Property(e => e.Reason).HasMaxLength(500);

            entity.HasIndex(e => new { e.SettingKey, e.ChangedAt })
                  .HasDatabaseName("IX_SettingChangeHistory_Key");
        });
    }
}

