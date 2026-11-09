using Microsoft.EntityFrameworkCore;

namespace EnterpriseBpm.Org.Infrastructure.Persistence;

public class OrgDbContext : DbContext
{
    public DbSet<OrgUnit> OrgUnits => Set<OrgUnit>();
    public DbSet<Employee> Employees => Set<Employee>();
    public DbSet<WorkflowDelegation> Delegations => Set<WorkflowDelegation>();

    public OrgDbContext(DbContextOptions<OrgDbContext> options) : base(options) { }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.HasDefaultSchema("org");

        modelBuilder.Entity<OrgUnit>(b =>
        {
            b.ToTable("OrgUnits");
            b.HasKey(x => x.Id);
            b.Property(x => x.Code).HasMaxLength(50).IsRequired();
            b.Property(x => x.Name).HasMaxLength(255).IsRequired();
            b.HasIndex(x => x.ParentId).HasDatabaseName("IX_OrgUnits_ParentId");
        });

        modelBuilder.Entity<Employee>(b =>
        {
            b.ToTable("Employees");
            b.HasKey(x => x.Id);
            b.Property(x => x.UserPrincipalName).HasMaxLength(255).IsRequired();
            b.HasIndex(x => x.OrgUnitId).HasDatabaseName("IX_Employees_OrgUnitId");
        });

        modelBuilder.Entity<WorkflowDelegation>(b =>
        {
            b.ToTable("WorkflowDelegations");
            b.HasKey(x => x.Id);
            b.HasIndex(x => new { x.DelegatorId, x.IsActive }).HasDatabaseName("IX_Delegation_Lookup");
        });
    }
}
