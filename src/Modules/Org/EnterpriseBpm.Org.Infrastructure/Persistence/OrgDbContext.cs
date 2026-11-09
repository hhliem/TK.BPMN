using EnterpriseBpm.Org.Domain.Entities;
using EnterpriseBpm.SharedKernel.Contracts;
using EnterpriseBpm.SharedKernel.Extensions;
using Microsoft.EntityFrameworkCore;

namespace EnterpriseBpm.Org.Infrastructure.Persistence;

public class OrgDbContext : DbContext
{
    public const string Schema = "org";

    public DbSet<OrgUnit> OrgUnits => Set<OrgUnit>();
    public DbSet<Position> Positions => Set<Position>();
    public DbSet<Employee> Employees => Set<Employee>();
    public DbSet<Role> Roles => Set<Role>();
    public DbSet<EmployeeRole> EmployeeRoles => Set<EmployeeRole>();
    public DbSet<WorkflowDelegation> WorkflowDelegations => Set<WorkflowDelegation>();

    public OrgDbContext(DbContextOptions<OrgDbContext> options) : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.HasDefaultSchema(Schema);

        // Apply Soft Delete global query filter
        modelBuilder.ApplySoftDeleteQueryFilter();

        // OrgUnits
        modelBuilder.Entity<OrgUnit>(b =>
        {
            b.ToTable("OrgUnits");
            b.HasKey(o => o.Id);
            b.HasIndex(o => o.Code).IsUnique();
            b.HasIndex(o => o.ParentId);
            b.Property(o => o.Code).HasMaxLength(50).IsRequired();
            b.Property(o => o.Name).HasMaxLength(255).IsRequired();
            b.Property(o => o.UnitType).HasConversion<int>().IsRequired();

            b.HasOne(o => o.Parent)
                .WithMany(o => o.Children)
                .HasForeignKey(o => o.ParentId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // Positions
        modelBuilder.Entity<Position>(b =>
        {
            b.ToTable("Positions");
            b.HasKey(p => p.Id);
            b.HasIndex(p => p.Code).IsUnique();
            b.Property(p => p.Code).HasMaxLength(50).IsRequired();
            b.Property(p => p.Title).HasMaxLength(255).IsRequired();
        });

        // Employees
        modelBuilder.Entity<Employee>(b =>
        {
            b.ToTable("Employees");
            b.HasKey(e => e.Id);
            b.HasIndex(e => e.UserPrincipalName).IsUnique();
            b.HasIndex(e => e.EmployeeCode).IsUnique();
            b.HasIndex(e => e.OrgUnitId);
            b.HasIndex(e => e.PositionId);
            b.HasIndex(e => e.DirectManagerId);

            // Covering Index phục vụ tìm kiếm danh bạ nội bộ (Directory Search)
            b.HasIndex(e => new { e.IsActive, e.FullName, e.EmployeeCode })
                .HasDatabaseName("IX_Employees_DirectorySearch")
                .IncludeProperties(e => new { e.PhoneNumber, e.Extension, e.JobTitle, e.WorkLocation, e.OrgUnitId, e.AvatarUrl });

            b.Property(e => e.UserPrincipalName).HasMaxLength(255).IsRequired();
            b.Property(e => e.EmployeeCode).HasMaxLength(50).IsRequired();
            b.Property(e => e.FullName).HasMaxLength(255).IsRequired();
            b.Property(e => e.PhoneNumber).HasMaxLength(20);
            b.Property(e => e.Extension).HasMaxLength(10);
            b.Property(e => e.AvatarUrl).HasMaxLength(500);
            b.Property(e => e.JobTitle).HasMaxLength(255);
            b.Property(e => e.WorkLocation).HasMaxLength(255);

            b.HasOne(e => e.OrgUnit)
                .WithMany(o => o.Employees)
                .HasForeignKey(e => e.OrgUnitId)
                .OnDelete(DeleteBehavior.Restrict);

            b.HasOne(e => e.Position)
                .WithMany(p => p.Employees)
                .HasForeignKey(e => e.PositionId)
                .OnDelete(DeleteBehavior.Restrict);

            b.HasOne(e => e.DirectManager)
                .WithMany(m => m.Subordinates)
                .HasForeignKey(e => e.DirectManagerId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // Roles
        modelBuilder.Entity<Role>(b =>
        {
            b.ToTable("Roles");
            b.HasKey(r => r.Id);
            b.HasIndex(r => r.RoleKey).IsUnique();
            b.Property(r => r.RoleKey).HasMaxLength(100).IsRequired();
            b.Property(r => r.Name).HasMaxLength(255).IsRequired();
            b.Property(r => r.Description).HasMaxLength(500);
        });

        // EmployeeRoles (Composite Key)
        modelBuilder.Entity<EmployeeRole>(b =>
        {
            b.ToTable("EmployeeRoles");
            b.HasKey(er => new { er.EmployeeId, er.RoleId });
            b.HasOne(er => er.Employee)
                .WithMany(e => e.EmployeeRoles)
                .HasForeignKey(er => er.EmployeeId)
                .OnDelete(DeleteBehavior.Cascade);
            b.HasOne(er => er.Role)
                .WithMany(r => r.EmployeeRoles)
                .HasForeignKey(er => er.RoleId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // WorkflowDelegations
        modelBuilder.Entity<WorkflowDelegation>(b =>
        {
            b.ToTable("WorkflowDelegations");
            b.HasKey(d => d.Id);
            b.HasIndex(d => new { d.DelegatorId, d.IsActive, d.FromDate, d.ToDate })
                .HasDatabaseName("IX_Delegations_ActiveLookup");
            b.Property(d => d.ScopeFormKey).HasMaxLength(100);
            b.Property(d => d.Reason).HasMaxLength(500).IsRequired();

            b.HasOne(d => d.Delegator)
                .WithMany(e => e.GivenDelegations)
                .HasForeignKey(d => d.DelegatorId)
                .OnDelete(DeleteBehavior.Restrict);

            b.HasOne(d => d.Delegatee)
                .WithMany(e => e.ReceivedDelegations)
                .HasForeignKey(d => d.DelegateeId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }
}

