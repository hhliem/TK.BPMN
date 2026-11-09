using EnterpriseBpm.SharedKernel.Common;
using Xunit;

namespace EnterpriseBpm.SharedKernel.UnitTests;

public class ResultTests
{
    [Fact]
    public void Success_ShouldCreateSuccessfulResult()
    {
        var result = Result.Success("EnterpriseBpm");

        Assert.True(result.IsSuccess);
        Assert.False(result.IsFailure);
        Assert.Equal("EnterpriseBpm", result.Value);
        Assert.Null(result.Error);
    }

    [Fact]
    public void Failure_ShouldCreateFailureResult()
    {
        var result = Result.Failure<string>("Không tìm thấy hồ sơ yêu cầu.");

        Assert.False(result.IsSuccess);
        Assert.True(result.IsFailure);
        Assert.NotNull(result.Error);
        Assert.Equal("Không tìm thấy hồ sơ yêu cầu.", result.Error);
        Assert.Throws<InvalidOperationException>(() => result.Value);
    }

    [Fact]
    public void NonGeneric_Success_And_Failure()
    {
        var success = Result.Success();
        Assert.True(success.IsSuccess);
        Assert.Null(success.Error);

        var failure = Result.Failure("Đã xảy ra lỗi kiểm toán");
        Assert.True(failure.IsFailure);
        Assert.Equal("Đã xảy ra lỗi kiểm toán", failure.Error);
    }

    [Fact]
    public void ImplicitConversion_ShouldWork()
    {
        Result<int> result = 42;
        Assert.True(result.IsSuccess);
        Assert.Equal(42, result.Value);
    }
}
