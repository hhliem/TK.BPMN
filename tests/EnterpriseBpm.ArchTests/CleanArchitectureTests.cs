using NetArchTest.Rules;
using Xunit;

namespace EnterpriseBpm.ArchTests;

public class CleanArchitectureTests
{
    [Fact]
    public void AuditLog_Domain_Should_Not_DependOn_OtherLayers()
    {
        var result = Types.InAssembly(typeof(AuditLog.Domain.Entities.AuditEntry).Assembly)
            .ShouldNot()
            .HaveDependencyOnAny(
                "EnterpriseBpm.AuditLog.Application",
                "EnterpriseBpm.AuditLog.Infrastructure",
                "EnterpriseBpm.AuditLog.Presentation",
                "EnterpriseBpm.AccessGovernance.Infrastructure")
            .GetResult();

        Assert.True(result.IsSuccessful, "AuditLog.Domain must not depend on higher layers or foreign module infrastructure.");
    }

    [Fact]
    public void AccessGovernance_Domain_Should_Not_DependOn_OtherLayers()
    {
        var result = Types.InAssembly(typeof(AccessGovernance.Domain.Entities.Role).Assembly)
            .ShouldNot()
            .HaveDependencyOnAny(
                "EnterpriseBpm.AccessGovernance.Application",
                "EnterpriseBpm.AccessGovernance.Infrastructure",
                "EnterpriseBpm.AccessGovernance.Presentation",
                "EnterpriseBpm.AuditLog.Infrastructure")
            .GetResult();

        Assert.True(result.IsSuccessful, "AccessGovernance.Domain must not depend on higher layers or foreign module infrastructure.");
    }

    [Fact]
    public void Org_Domain_Should_Not_DependOn_OtherLayers()
    {
        var result = Types.InAssembly(typeof(Org.Domain.Entities.OrgUnit).Assembly)
            .ShouldNot()
            .HaveDependencyOnAny(
                "EnterpriseBpm.Org.Application",
                "EnterpriseBpm.Org.Infrastructure",
                "EnterpriseBpm.Org.Presentation",
                "EnterpriseBpm.Form.Infrastructure")
            .GetResult();

        Assert.True(result.IsSuccessful, "Org.Domain must not depend on higher layers or foreign module infrastructure.");
    }

    [Fact]
    public void Org_Application_Should_Not_DependOn_Infrastructure_Or_Presentation()
    {
        var result = Types.InAssembly(typeof(Org.Application.Contracts.IOrgHierarchyService).Assembly)
            .ShouldNot()
            .HaveDependencyOnAny(
                "EnterpriseBpm.Org.Infrastructure",
                "EnterpriseBpm.Org.Presentation")
            .GetResult();

        Assert.True(result.IsSuccessful, "Org.Application must not depend on Infrastructure or Presentation.");
    }

    [Fact]
    public void Form_Domain_Should_Not_DependOn_OtherLayers()
    {
        var result = Types.InAssembly(typeof(Form.Domain.Entities.FormDefinition).Assembly)
            .ShouldNot()
            .HaveDependencyOnAny(
                "EnterpriseBpm.Form.Application",
                "EnterpriseBpm.Form.Infrastructure",
                "EnterpriseBpm.Form.Presentation",
                "EnterpriseBpm.Org.Infrastructure")
            .GetResult();

        Assert.True(result.IsSuccessful, "Form.Domain must not depend on higher layers or foreign module infrastructure.");
    }

    [Fact]
    public void Form_Application_Should_Not_DependOn_Infrastructure_Or_Presentation()
    {
        var result = Types.InAssembly(typeof(Form.Application.Contracts.IFormVersionService).Assembly)
            .ShouldNot()
            .HaveDependencyOnAny(
                "EnterpriseBpm.Form.Infrastructure",
                "EnterpriseBpm.Form.Presentation")
            .GetResult();

        Assert.True(result.IsSuccessful, "Form.Application must not depend on Infrastructure or Presentation.");
    }
}
