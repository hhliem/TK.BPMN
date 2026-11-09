using EnterpriseBpm.AccessGovernance.Domain.Entities;
using EnterpriseBpm.SharedKernel.Contracts;
using EnterpriseBpm.SharedKernel.Extensions;
using Microsoft.EntityFrameworkCore;

namespace EnterpriseBpm.AccessGovernance.Infrastructure.Persistence;

public class AuthDbContext : DbContext
{
    public const string Schema = "auth";

    public DbSet<Permission> Permissions => Set<Permission>();
    public DbSet<Role> Roles => Set<Role>();
    public DbSet<RolePermission> RolePermissions => Set<RolePermission>();
    public DbSet<SecurityGroup> SecurityGroups => Set<SecurityGroup>();
    public DbSet<SecurityGroupRole> SecurityGroupRoles => Set<SecurityGroupRole>();
    public DbSet<SecurityGroupMember> SecurityGroupMembers => Set<SecurityGroupMember>();
    public DbSet<UserRole> UserRoles => Set<UserRole>();
    public DbSet<DataScopePolicy> DataScopePolicies => Set<DataScopePolicy>();
    public DbSet<ApprovalLimit> ApprovalLimits => Set<ApprovalLimit>();
    public DbSet<FormPermission> FormPermissions => Set<FormPermission>();
    public DbSet<MenuPermission> MenuPermissions => Set<MenuPermission>();

    public AuthDbContext(DbContextOptions<AuthDbContext> options) : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.HasDefaultSchema(Schema);

        // Apply global soft delete filter
        modelBuilder.ApplySoftDeleteQueryFilter();

        // Permissions
        modelBuilder.Entity<Permission>(b =>
        {
            b.ToTable("Permissions");
            b.HasKey(p => p.Id);
            b.HasIndex(p => p.PermissionCode).IsUnique();
            b.HasIndex(p => new { p.ModuleName, p.Resource, p.Action });
            b.Property(p => p.PermissionCode).HasMaxLength(100).IsRequired();
            b.Property(p => p.ModuleName).HasMaxLength(50).IsRequired();
            b.Property(p => p.Resource).HasMaxLength(50).IsRequired();
            b.Property(p => p.Action).HasMaxLength(50).IsRequired();
            b.Property(p => p.Name).HasMaxLength(255).IsRequired();
            b.Property(p => p.Description).HasMaxLength(500);
        });

        // Roles
        modelBuilder.Entity<Role>(b =>
        {
            b.ToTable("Roles");
            b.HasKey(r => r.Id);
            b.HasIndex(r => r.RoleCode).IsUnique();
            b.Property(r => r.RoleCode).HasMaxLength(100).IsRequired();
            b.Property(r => r.Name).HasMaxLength(255).IsRequired();
            b.Property(r => r.RoleType).HasConversion<string>().HasMaxLength(50).IsRequired();
            b.Property(r => r.Description).HasMaxLength(500);
        });

        // RolePermissions (Composite Key)
        modelBuilder.Entity<RolePermission>(b =>
        {
            b.ToTable("RolePermissions");
            b.HasKey(rp => new { rp.RoleId, rp.PermissionId });
            b.HasOne(rp => rp.Role)
                .WithMany(r => r.RolePermissions)
                .HasForeignKey(rp => rp.RoleId)
                .OnDelete(DeleteBehavior.Cascade);
            b.HasOne(rp => rp.Permission)
                .WithMany(p => p.RolePermissions)
                .HasForeignKey(rp => rp.PermissionId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // SecurityGroups
        modelBuilder.Entity<SecurityGroup>(b =>
        {
            b.ToTable("SecurityGroups");
            b.HasKey(g => g.Id);
            b.HasIndex(g => g.GroupCode).IsUnique();
            b.Property(g => g.GroupCode).HasMaxLength(100).IsRequired();
            b.Property(g => g.Name).HasMaxLength(255).IsRequired();
            b.Property(g => g.Description).HasMaxLength(500);
        });

        // SecurityGroupRoles (Composite Key)
        modelBuilder.Entity<SecurityGroupRole>(b =>
        {
            b.ToTable("SecurityGroupRoles");
            b.HasKey(sgr => new { sgr.GroupId, sgr.RoleId });
            b.HasOne(sgr => sgr.Group)
                .WithMany(g => g.SecurityGroupRoles)
                .HasForeignKey(sgr => sgr.GroupId)
                .OnDelete(DeleteBehavior.Cascade);
            b.HasOne(sgr => sgr.Role)
                .WithMany(r => r.SecurityGroupRoles)
                .HasForeignKey(sgr => sgr.RoleId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // SecurityGroupMembers (Composite Key)
        modelBuilder.Entity<SecurityGroupMember>(b =>
        {
            b.ToTable("SecurityGroupMembers");
            b.HasKey(sgm => new { sgm.GroupId, sgm.UserId });
            b.HasIndex(sgm => sgm.UserId);
            b.HasOne(sgm => sgm.Group)
                .WithMany(g => g.Members)
                .HasForeignKey(sgm => sgm.GroupId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // UserRoles (Composite Key)
        modelBuilder.Entity<UserRole>(b =>
        {
            b.ToTable("UserRoles");
            b.HasKey(ur => new { ur.UserId, ur.RoleId });
            b.HasIndex(ur => ur.UserId);
            b.HasOne(ur => ur.Role)
                .WithMany(r => r.UserRoles)
                .HasForeignKey(ur => ur.RoleId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // DataScopePolicies
        modelBuilder.Entity<DataScopePolicy>(b =>
        {
            b.ToTable("DataScopePolicies");
            b.HasKey(p => p.Id);
            b.HasIndex(p => new { p.RoleId, p.Resource });
            b.Property(p => p.Resource).HasMaxLength(50).IsRequired();
            b.Property(p => p.ScopeType).HasConversion<string>().HasMaxLength(50).IsRequired();
            b.HasOne(p => p.Role)
                .WithMany(r => r.DataScopePolicies)
                .HasForeignKey(p => p.RoleId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // ApprovalLimits
        modelBuilder.Entity<ApprovalLimit>(b =>
        {
            b.ToTable("ApprovalLimits");
            b.HasKey(a => a.Id);
            b.HasIndex(a => new { a.FormKey, a.PositionId, a.RoleId });
            b.Property(a => a.FormKey).HasMaxLength(100).IsRequired();
            b.Property(a => a.Currency).HasMaxLength(10).IsRequired();
            b.Property(a => a.MaxLimitAmount).HasPrecision(18, 2).IsRequired();
            b.HasOne(a => a.Role)
                .WithMany(r => r.ApprovalLimits)
                .HasForeignKey(a => a.RoleId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // FormPermissions
        modelBuilder.Entity<FormPermission>(b =>
        {
            b.ToTable("FormPermissions");
            b.HasKey(f => f.Id);
            b.HasIndex(f => new { f.FormKey, f.SubjectType, f.SubjectId });
            b.Property(f => f.FormKey).HasMaxLength(100).IsRequired();
            b.Property(f => f.SubjectType).HasConversion<string>().HasMaxLength(20).IsRequired();
        });

        // MenuPermissions
        modelBuilder.Entity<MenuPermission>(b =>
        {
            b.ToTable("MenuPermissions");
            b.HasKey(m => m.Id);
            b.Property(m => m.MenuKey).HasMaxLength(100).IsRequired();
            b.Property(m => m.ParentMenuKey).HasMaxLength(100);
            b.Property(m => m.Title).HasMaxLength(255).IsRequired();
            b.Property(m => m.RouteUrl).HasMaxLength(500).IsRequired();
            b.Property(m => m.IconClass).HasMaxLength(100);
            b.Property(m => m.RequiredPermissionCode).HasMaxLength(100);
        });
    }
}
