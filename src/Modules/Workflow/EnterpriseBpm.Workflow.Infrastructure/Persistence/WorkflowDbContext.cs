using EnterpriseBpm.EventBus.Outbox;
using EnterpriseBpm.SharedKernel.Contracts;
using EnterpriseBpm.SharedKernel.Extensions;
using EnterpriseBpm.SharedKernel.Interceptors;
using EnterpriseBpm.Workflow.Application.Handlers;
using EnterpriseBpm.Workflow.Domain.Entities;
using EnterpriseBpm.Workflow.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace EnterpriseBpm.Workflow.Infrastructure.Persistence;

public class WorkflowDbContext : DbContext, IWorkflowDbContextProvider
{
    private readonly ICurrentUserService? _currentUserService;

    public WorkflowDbContext(
        DbContextOptions<WorkflowDbContext> options,
        ICurrentUserService? currentUserService = null)
        : base(options)
    {
        _currentUserService = currentUserService;
    }

    public DbSet<WorkflowSubmission> WorkflowSubmissions => Set<WorkflowSubmission>();
    public DbSet<ApprovalTask> ApprovalTasks => Set<ApprovalTask>();
    public DbSet<ApprovalHistory> ApprovalHistory => Set<ApprovalHistory>();
    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        base.OnConfiguring(optionsBuilder);
        optionsBuilder.AddInterceptors(new OutboxMessageSaveChangesInterceptor());
        if (_currentUserService != null)
        {
            optionsBuilder.AddInterceptors(new AuditableFieldsInterceptor(_currentUserService));
        }
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.HasDefaultSchema("workflow");

        // Áp dụng Global Query Filter cho Soft Delete
        modelBuilder.ApplySoftDeleteQueryFilter();

        // 1. WorkflowSubmissions
        modelBuilder.Entity<WorkflowSubmission>(builder =>
        {
            builder.ToTable("WorkflowSubmissions");
            builder.HasKey(s => s.Id);

            builder.Property(s => s.WorkflowInstanceId)
                .HasMaxLength(100)
                .IsRequired();
            builder.HasIndex(s => s.WorkflowInstanceId)
                .IsUnique();

            builder.Property(s => s.WorkflowDefinitionId)
                .HasMaxLength(100)
                .IsRequired();

            builder.Property(s => s.FormKey)
                .HasMaxLength(100)
                .IsRequired();

            builder.Property(s => s.CurrentStatus)
                .HasConversion(
                    v => v.ToString(),
                    v => Enum.Parse<WorkflowStatus>(v))
                .HasMaxLength(50)
                .IsRequired();

            builder.Property(s => s.Priority)
                .HasConversion<int>()
                .IsRequired();

            builder.Property(s => s.FormDataJson)
                .IsRequired();

            builder.HasIndex(s => new { s.CurrentStatus, s.SubmittedByUserId })
                .HasDatabaseName("IX_WorkflowSubmissions_Status");

            builder.HasIndex(s => new { s.RequesterOrgUnitId, s.SubmittedAt })
                .HasDatabaseName("IX_WorkflowSubmissions_OrgUnit");

            builder.HasMany(s => s.Tasks)
                .WithOne(t => t.Submission)
                .HasPrincipalKey(s => s.WorkflowInstanceId)
                .HasForeignKey(t => t.WorkflowInstanceId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // 2. ApprovalTasks
        modelBuilder.Entity<ApprovalTask>(builder =>
        {
            builder.ToTable("ApprovalTasks");
            builder.HasKey(t => t.Id);

            builder.Property(t => t.WorkflowInstanceId)
                .HasMaxLength(100)
                .IsRequired();

            builder.Property(t => t.ActivityId)
                .HasMaxLength(100)
                .IsRequired();

            builder.Property(t => t.TargetRole)
                .HasMaxLength(100);

            builder.Property(t => t.Status)
                .HasConversion(
                    v => v.ToString(),
                    v => Enum.Parse<ApprovalTaskStatus>(v))
                .HasMaxLength(50)
                .IsRequired();

            builder.Property(t => t.Decision)
                .HasConversion(
                    v => v.HasValue ? v.Value.ToString() : null,
                    v => !string.IsNullOrEmpty(v) ? Enum.Parse<ApprovalDecision>(v) : null)
                .HasMaxLength(50);

            builder.HasIndex(t => new { t.AssignedToUserId, t.Status })
                .HasDatabaseName("IX_ApprovalTasks_UserPending");

            // Filtered Index tối ưu hóa truy vấn tác vụ chờ duyệt của cán bộ (TSK-806)
            builder.HasIndex(t => new { t.AssignedToUserId, t.DueDate })
                .HasFilter("[Status] = 'Pending'")
                .HasDatabaseName("IX_ApprovalTasks_PendingFilter");

            builder.HasIndex(t => new { t.WorkflowInstanceId, t.Status })
                .HasDatabaseName("IX_ApprovalTasks_WorkflowInstance");
        });

        // 3. ApprovalHistory
        modelBuilder.Entity<ApprovalHistory>(builder =>
        {
            builder.ToTable("ApprovalHistory");
            builder.HasKey(h => h.Id);

            builder.Property(h => h.WorkflowInstanceId)
                .HasMaxLength(100)
                .IsRequired();

            builder.Property(h => h.StepName)
                .HasMaxLength(255)
                .IsRequired();

            builder.Property(h => h.Decision)
                .HasConversion(
                    v => v.ToString(),
                    v => Enum.Parse<ApprovalDecision>(v))
                .HasMaxLength(50)
                .IsRequired();

            builder.Property(h => h.DigitalSignatureHash)
                .HasMaxLength(256)
                .IsRequired();

            builder.HasIndex(h => new { h.WorkflowInstanceId, h.StepOrder })
                .HasDatabaseName("IX_ApprovalHistory_Workflow");

            builder.HasIndex(h => new { h.ActorUserId, h.ActionTimestamp })
                .HasDatabaseName("IX_ApprovalHistory_Actor");
        });

        // 4. OutboxMessages (Transactional Outbox Pattern - TSK-801, TSK-806)
        modelBuilder.Entity<OutboxMessage>(builder =>
        {
            builder.ToTable("OutboxMessages", "workflow");
            builder.HasKey(m => m.Id);

            builder.Property(m => m.EventType)
                .HasMaxLength(500)
                .IsRequired();

            builder.Property(m => m.Payload)
                .IsRequired();

            builder.Property(m => m.Error)
                .HasMaxLength(2000);

            // Filtered Index cho Background Worker quét nhanh các tin chưa xử lý
            builder.HasIndex(m => m.CreatedAt)
                .HasFilter("[ProcessedAt] IS NULL")
                .HasDatabaseName("IX_OutboxMessages_PendingFilter");

            builder.HasIndex(m => m.CreatedAt)
                .HasDatabaseName("IX_OutboxMessages_CreatedAt");
        });
    }
}

