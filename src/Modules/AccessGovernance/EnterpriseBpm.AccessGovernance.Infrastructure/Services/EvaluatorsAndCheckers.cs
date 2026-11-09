using EnterpriseBpm.AccessGovernance.Application.Contracts;
using EnterpriseBpm.AccessGovernance.Application.DTOs;
using EnterpriseBpm.AccessGovernance.Domain.Enums;
using EnterpriseBpm.AccessGovernance.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace EnterpriseBpm.AccessGovernance.Infrastructure.Services;

public class DataScopeEvaluator : IDataScopeEvaluator
{
    private readonly AuthDbContext _dbContext;
    private readonly ILogger<DataScopeEvaluator> _logger;

    public DataScopeEvaluator(AuthDbContext dbContext, ILogger<DataScopeEvaluator> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<DataScopeResult> EvaluateScopeAsync(Guid userId, string resourceName, CancellationToken cancellationToken = default)
    {
        // Lấy toàn bộ chính sách phạm vi dữ liệu áp dụng cho các vai trò của người dùng
        var policies = await _dbContext.UserRoles
            .AsNoTracking()
            .Where(ur => ur.UserId == userId && ur.Role.IsActive)
            .SelectMany(ur => ur.Role.DataScopePolicies.Where(p => p.Resource == resourceName || p.Resource == "*"))
            .ToListAsync(cancellationToken);

        if (!policies.Any())
        {
            // Mặc định an toàn: Chỉ nhìn thấy dữ liệu của chính mình (Self)
            return new DataScopeResult("Self", null, Array.Empty<Guid>());
        }

        // Chọn phạm vi rộng nhất (Global > Division > Department > Self)
        if (policies.Any(p => p.ScopeType == DataScopeType.Global))
        {
            return new DataScopeResult("Global", null, Array.Empty<Guid>());
        }

        if (policies.Any(p => p.ScopeType == DataScopeType.Division))
        {
            return new DataScopeResult("Division", null, Array.Empty<Guid>());
        }

        if (policies.Any(p => p.ScopeType == DataScopeType.Department))
        {
            return new DataScopeResult("Department", null, Array.Empty<Guid>());
        }

        return new DataScopeResult("Self", null, Array.Empty<Guid>());
    }
}

public class ApprovalLimitChecker : IApprovalLimitChecker
{
    private readonly AuthDbContext _dbContext;
    private readonly ILogger<ApprovalLimitChecker> _logger;

    public ApprovalLimitChecker(AuthDbContext dbContext, ILogger<ApprovalLimitChecker> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<ApprovalLimitResult> CanApproveAmountAsync(
        Guid userId, 
        string formKey, 
        decimal amount, 
        string currency = "VND", 
        CancellationToken cancellationToken = default)
    {
        // 1. Kiểm tra hạn mức gắn với vai trò của user
        var roleLimits = await _dbContext.UserRoles
            .AsNoTracking()
            .Where(ur => ur.UserId == userId && ur.Role.IsActive)
            .SelectMany(ur => ur.Role.ApprovalLimits
                .Where(al => al.IsActive && (al.FormKey == formKey || al.FormKey == "*") && al.Currency == currency))
            .Select(al => al.MaxLimitAmount)
            .ToListAsync(cancellationToken);

        if (!roleLimits.Any())
        {
            // Không có cấu hình hạn mức -> Cho phép duyệt nếu không yêu cầu hạn chế tài chính
            // Hoặc nếu đơn có số tiền > 0 mà không có hạn mức thì từ chối an toàn
            if (amount > 0)
            {
                return new ApprovalLimitResult(
                    IsAllowed: false,
                    MaxAllowedLimit: 0,
                    ReasonMessage: $"Người dùng chưa được cấu hình hạn mức phê duyệt cho biểu mẫu '{formKey}' đối với loại tiền {currency}.");
            }

            return new ApprovalLimitResult(IsAllowed: true, MaxAllowedLimit: decimal.MaxValue, ReasonMessage: null);
        }

        var maxLimit = roleLimits.Max();
        if (amount <= maxLimit)
        {
            return new ApprovalLimitResult(
                IsAllowed: true,
                MaxAllowedLimit: maxLimit,
                ReasonMessage: null);
        }

        return new ApprovalLimitResult(
            IsAllowed: false,
            MaxAllowedLimit: maxLimit,
            ReasonMessage: $"Số tiền yêu cầu ({amount:N0} {currency}) vượt quá hạn mức tối đa được phép duyệt ({maxLimit:N0} {currency}).");
    }
}

