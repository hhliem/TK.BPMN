using EnterpriseBpm.Analytics.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace EnterpriseBpm.Analytics.Infrastructure.Persistence;

public class AnalyticsDbContext : DbContext
{
    public AnalyticsDbContext(DbContextOptions<AnalyticsDbContext> options) : base(options) { }

    public DbSet<WorkingCalendar> WorkingCalendars => Set<WorkingCalendar>();
    public DbSet<WorkflowSummaryMetrics> WorkflowSummaryMetrics => Set<WorkflowSummaryMetrics>();
    public DbSet<StepSlaAnalytics> StepSlaAnalytics => Set<StepSlaAnalytics>();
    public DbSet<EmployeeProductivityMetrics> EmployeeProductivityMetrics => Set<EmployeeProductivityMetrics>();
    public DbSet<CustomReportTemplate> CustomReportTemplates => Set<CustomReportTemplate>();
    public DbSet<ScheduledReportSubscription> ScheduledReportSubscriptions => Set<ScheduledReportSubscription>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.HasDefaultSchema("analytics");

        // 1. WorkingCalendar
        modelBuilder.Entity<WorkingCalendar>(b =>
        {
            b.ToTable("WorkingCalendars");
            b.HasKey(c => c.Id);
            b.Property(c => c.CalendarCode).HasMaxLength(50).IsRequired();
            b.Property(c => c.Name).HasMaxLength(200).IsRequired();
            b.HasIndex(c => c.CalendarCode).IsUnique();
        });

        // 2. WorkflowSummaryMetrics (Materialized Big Data Summary)
        modelBuilder.Entity<WorkflowSummaryMetrics>(b =>
        {
            b.ToTable("WorkflowSummaryMetrics");
            b.HasKey(m => m.Id);
            b.Property(m => m.WorkflowDefinitionId).HasMaxLength(100).IsRequired();
            b.Property(m => m.WorkflowTitle).HasMaxLength(250).IsRequired();
            b.Property(m => m.DepartmentName).HasMaxLength(200);
            b.Property(m => m.TotalApprovedAmount).HasPrecision(18, 2);

            // Covering Index cho truy vấn Dashboard nhanh dưới 20ms
            b.HasIndex(m => new { m.PeriodDate, m.PeriodType, m.WorkflowDefinitionId })
             .HasDatabaseName("IX_WorkflowSummaryMetrics_Period_Def");
        });

        // 3. StepSlaAnalytics (Bottleneck Detection)
        modelBuilder.Entity<StepSlaAnalytics>(b =>
        {
            b.ToTable("StepSlaAnalytics");
            b.HasKey(s => s.Id);
            b.Property(s => s.WorkflowDefinitionId).HasMaxLength(100).IsRequired();
            b.Property(s => s.WorkflowTitle).HasMaxLength(250).IsRequired();
            b.Property(s => s.ActivityId).HasMaxLength(100).IsRequired();
            b.Property(s => s.StepName).HasMaxLength(200).IsRequired();
            b.Property(s => s.TargetRole).HasMaxLength(100);

            // Filtered Index cho các bước đang bị nghẽn
            b.HasIndex(s => new { s.WorkflowDefinitionId, s.IsCurrentBottleneck })
             .HasDatabaseName("IX_StepSlaAnalytics_Bottleneck");
        });

        // 4. EmployeeProductivityMetrics (Scorecard)
        modelBuilder.Entity<EmployeeProductivityMetrics>(b =>
        {
            b.ToTable("EmployeeProductivityMetrics");
            b.HasKey(p => p.Id);
            b.Property(p => p.UserName).HasMaxLength(150).IsRequired();
            b.Property(p => p.UserEmail).HasMaxLength(150);
            b.Property(p => p.DepartmentName).HasMaxLength(200);
            b.Property(p => p.YearMonth).HasMaxLength(7).IsRequired(); // YYYY-MM
            b.Property(p => p.ScoreRank).HasMaxLength(10).IsRequired();

            b.HasIndex(p => new { p.UserId, p.YearMonth })
             .IsUnique()
             .HasDatabaseName("IX_EmployeeProductivity_User_YearMonth");
            b.HasIndex(p => new { p.DepartmentId, p.YearMonth })
             .HasDatabaseName("IX_EmployeeProductivity_Dept_YearMonth");
        });

        // 5. CustomReportTemplate
        modelBuilder.Entity<CustomReportTemplate>(b =>
        {
            b.ToTable("CustomReportTemplates");
            b.HasKey(t => t.Id);
            b.Property(t => t.ReportKey).HasMaxLength(100).IsRequired();
            b.Property(t => t.Title).HasMaxLength(250).IsRequired();
            b.Property(t => t.Description).HasMaxLength(500);
            b.HasIndex(t => t.ReportKey).IsUnique();
        });

        // 6. ScheduledReportSubscription
        modelBuilder.Entity<ScheduledReportSubscription>(b =>
        {
            b.ToTable("ScheduledReportSubscriptions");
            b.HasKey(s => s.Id);
            b.Property(s => s.Title).HasMaxLength(250).IsRequired();
            b.Property(s => s.CronExpression).HasMaxLength(50).IsRequired();

            // Filtered index cho các subscription đang active
            b.HasIndex(s => new { s.IsActive, s.NextRunAt })
             .HasDatabaseName("IX_ScheduledSubscriptions_Active_NextRun");
        });
    }
}

