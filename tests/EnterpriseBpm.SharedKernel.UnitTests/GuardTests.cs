using EnterpriseBpm.SharedKernel.Common;
using Xunit;

namespace EnterpriseBpm.SharedKernel.UnitTests;

public class GuardTests
{
    [Fact]
    public void AgainstNull_WhenValueIsNotNull_ShouldNotThrow()
    {
        var input = "valid-string";
        var exception = Record.Exception(() => Guard.AgainstNull(input, nameof(input)));
        Assert.Null(exception);
    }

    [Fact]
    public void AgainstNull_WhenValueIsNull_ShouldThrowArgumentNullException()
    {
        string? input = null;
        Assert.Throws<ArgumentNullException>(() => Guard.AgainstNull(input, nameof(input)));
    }

    [Fact]
    public void AgainstNullOrWhiteSpace_WhenValid_ShouldNotThrow()
    {
        var input = "workflow:task:approve";
        var exception = Record.Exception(() => Guard.AgainstNullOrWhiteSpace(input, nameof(input)));
        Assert.Null(exception);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void AgainstNullOrWhiteSpace_WhenInvalid_ShouldThrowArgumentException(string? input)
    {
        Assert.Throws<ArgumentException>(() => Guard.AgainstNullOrWhiteSpace(input, nameof(input)));
    }

    [Fact]
    public void AgainstNegative_WhenPositiveOrZero_ShouldNotThrow()
    {
        var exceptionZero = Record.Exception(() => Guard.AgainstNegative(0, "amount"));
        var exceptionPos = Record.Exception(() => Guard.AgainstNegative(100.5m, "amount"));
        Assert.Null(exceptionZero);
        Assert.Null(exceptionPos);
    }

    [Fact]
    public void AgainstNegative_WhenNegative_ShouldThrowArgumentOutOfRangeException()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => Guard.AgainstNegative(-5m, "amount"));
    }

    [Fact]
    public void AgainstEmptyGuid_WhenValidGuid_ShouldNotThrow()
    {
        var exception = Record.Exception(() => Guard.AgainstEmptyGuid(Guid.NewGuid(), "id"));
        Assert.Null(exception);
    }

    [Fact]
    public void AgainstEmptyGuid_WhenEmptyGuid_ShouldThrowArgumentException()
    {
        Assert.Throws<ArgumentException>(() => Guard.AgainstEmptyGuid(Guid.Empty, "id"));
    }
}
