using EnterpriseBpm.Org.Domain.Exceptions;
using EnterpriseBpm.Org.Domain.Services;
using Xunit;

namespace EnterpriseBpm.SharedKernel.UnitTests;

public class AntiCircularAncestryValidatorTests
{
    [Fact]
    public void ValidateNoCycle_WhenNewParentIsNull_ShouldPass()
    {
        var unitId = Guid.NewGuid();
        var exception = Record.Exception(() => 
            AntiCircularAncestryValidator.ValidateNoCycle(unitId, null, _ => null));

        Assert.Null(exception);
    }

    [Fact]
    public void ValidateNoCycle_WhenParentIsSelf_ShouldThrowCircularOrgException()
    {
        var unitId = Guid.NewGuid();
        Assert.Throws<CircularOrgException>(() => 
            AntiCircularAncestryValidator.ValidateNoCycle(unitId, unitId, _ => null));
    }

    [Fact]
    public void ValidateNoCycle_WhenChildAssignedAsParent_ShouldDetectCycleAndThrow()
    {
        // Giả lập cây: Root (A) -> Phòng Ban (B) -> Nhóm (C)
        var unitA = Guid.NewGuid();
        var unitB = Guid.NewGuid();
        var unitC = Guid.NewGuid();

        var tree = new Dictionary<Guid, Guid?>
        {
            [unitA] = null,
            [unitB] = unitA,
            [unitC] = unitB
        };

        // Cố tình chuyển Parent của A thành C (A -> B -> C -> A vòng lặp)
        Assert.Throws<CircularOrgException>(() => 
            AntiCircularAncestryValidator.ValidateNoCycle(unitA, unitC, id => tree.GetValueOrDefault(id)));
    }

    [Fact]
    public void ValidateNoCycle_WhenMovingToDifferentValidBranch_ShouldPass()
    {
        var unitA = Guid.NewGuid();
        var unitB = Guid.NewGuid();
        var unitC = Guid.NewGuid();

        var tree = new Dictionary<Guid, Guid?>
        {
            [unitA] = null,
            [unitB] = unitA,
            [unitC] = null // C là nhánh độc lập khác
        };

        // Chuyển B sang trực thuộc C -> hợp lệ
        var exception = Record.Exception(() => 
            AntiCircularAncestryValidator.ValidateNoCycle(unitB, unitC, id => tree.GetValueOrDefault(id)));

        Assert.Null(exception);
    }
}

