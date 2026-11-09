using System.Diagnostics.CodeAnalysis;

namespace EnterpriseBpm.SharedKernel.Common;

/// <summary>
/// Guard Clauses bảo vệ tính hợp lệ của tham số đầu vào
/// </summary>
public static class Guard
{
    public static void AgainstNull<T>([NotNull] T? value, string parameterName)
    {
        if (value is null)
            throw new ArgumentNullException(parameterName, $"Tham số '{parameterName}' không được phép null.");
    }

    public static void AgainstNullOrWhiteSpace([NotNull] string? value, string parameterName)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException($"Tham số '{parameterName}' không được rỗng hoặc chỉ chứa khoảng trắng.", parameterName);
    }

    public static void AgainstNegative(decimal value, string parameterName)
    {
        if (value < 0)
            throw new ArgumentOutOfRangeException(parameterName, $"Giá trị '{parameterName}' không được là số âm.");
    }

    public static void AgainstEmptyGuid(Guid value, string parameterName)
    {
        if (value == Guid.Empty)
            throw new ArgumentException($"Mã định danh '{parameterName}' không được là Guid.Empty.", parameterName);
    }
}

