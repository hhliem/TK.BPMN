using EnterpriseBpm.Org.Domain.Exceptions;
using EnterpriseBpm.Org.Domain.Services;
using Xunit;

namespace EnterpriseBpm.SharedKernel.UnitTests;

public class AntiCircularDelegationValidatorTests
{
    [Fact]
    public void Validate_WhenDelegatingToSelf_ShouldThrowInvalidDelegationException()
    {
        var userId = Guid.NewGuid();
        var now = DateTime.UtcNow;

        Assert.Throws<InvalidDelegationException>(() =>
            AntiCircularDelegationValidator.Validate(
                userId, 
                userId, 
                now, 
                now.AddDays(7), 
                Array.Empty<(Guid, Guid, DateTime, DateTime, bool)>()));
    }

    [Fact]
    public void Validate_WhenFromDateGreaterThanToDate_ShouldThrow()
    {
        var userA = Guid.NewGuid();
        var userB = Guid.NewGuid();
        var now = DateTime.UtcNow;

        Assert.Throws<InvalidDelegationException>(() =>
            AntiCircularDelegationValidator.Validate(
                userA, 
                userB, 
                now.AddDays(5), 
                now, 
                Array.Empty<(Guid, Guid, DateTime, DateTime, bool)>()));
    }

    [Fact]
    public void Validate_WhenCircularDelegationWithOverlappingDates_ShouldThrow()
    {
        var userA = Guid.NewGuid();
        var userB = Guid.NewGuid();
        var now = DateTime.UtcNow;

        // B đã ủy quyền cho A từ ngày 1 đến ngày 10
        var existing = new[]
        {
            (DelegatorId: userB, DelegateeId: userA, FromDate: now, ToDate: now.AddDays(10), IsActive: true)
        };

        // A cố tình ủy quyền lại cho B từ ngày 5 đến ngày 15 (chồng lấn)
        Assert.Throws<InvalidDelegationException>(() =>
            AntiCircularDelegationValidator.Validate(
                userA, 
                userB, 
                now.AddDays(5), 
                now.AddDays(15), 
                existing));
    }

    [Fact]
    public void Validate_WhenNoOverlapDates_ShouldPass()
    {
        var userA = Guid.NewGuid();
        var userB = Guid.NewGuid();
        var now = DateTime.UtcNow;

        // B ủy quyền cho A trong quá khứ (đã hết hạn)
        var existing = new[]
        {
            (DelegatorId: userB, DelegateeId: userA, FromDate: now.AddDays(-20), ToDate: now.AddDays(-10), IsActive: true)
        };

        // A ủy quyền cho B hiện tại (không chồng lấn)
        var exception = Record.Exception(() =>
            AntiCircularDelegationValidator.Validate(
                userA, 
                userB, 
                now, 
                now.AddDays(7), 
                existing));

        Assert.Null(exception);
    }
}

