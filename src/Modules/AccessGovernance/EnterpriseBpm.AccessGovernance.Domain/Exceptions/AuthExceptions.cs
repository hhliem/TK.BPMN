namespace EnterpriseBpm.AccessGovernance.Domain.Exceptions;

public class ApprovalLimitExceededException : Exception
{
    public Guid UserId { get; }
    public string FormKey { get; }
    public decimal RequestedAmount { get; }
    public decimal MaxLimitAmount { get; }

    public ApprovalLimitExceededException(Guid userId, string formKey, decimal requestedAmount, decimal maxLimitAmount)
        : base($"Hạn mức phê duyệt cho người dùng '{userId}' đối với biểu mẫu '{formKey}' là {maxLimitAmount:N0} VND, số tiền yêu cầu {requestedAmount:N0} VND đã vượt thẩm quyền cho phép.")
    {
        UserId = userId;
        FormKey = formKey;
        RequestedAmount = requestedAmount;
        MaxLimitAmount = maxLimitAmount;
    }
}

public class UnauthorizedResourceAccessException : Exception
{
    public Guid UserId { get; }
    public string RequiredPermission { get; }
    public string Resource { get; }

    public UnauthorizedResourceAccessException(Guid userId, string requiredPermission, string resource)
        : base($"Người dùng '{userId}' không có quyền '{requiredPermission}' để truy cập tài nguyên '{resource}'.")
    {
        UserId = userId;
        RequiredPermission = requiredPermission;
        Resource = resource;
    }
}

