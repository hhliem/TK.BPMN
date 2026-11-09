using EnterpriseBpm.AccessGovernance.Application.Contracts;
using EnterpriseBpm.AccessGovernance.Infrastructure.Authorization;
using EnterpriseBpm.AccessGovernance.Infrastructure.Cache;
using EnterpriseBpm.AccessGovernance.Infrastructure.Persistence;
using EnterpriseBpm.AccessGovernance.Infrastructure.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace EnterpriseBpm.AccessGovernance.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddAccessGovernanceInfrastructure(
        this IServiceCollection services, 
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection") 
            ?? throw new InvalidOperationException("Chuỗi kết nối cơ sở dữ liệu 'DefaultConnection' chưa được cấu hình trong hệ thống.");

        services.AddDbContextPool<AuthDbContext>(options =>
            options.UseSqlServer(connectionString, sql => 
                sql.MigrationsHistoryTable("__EFMigrationsHistory", AuthDbContext.Schema)), poolSize: 256);

        // Memory cache cho L1
        services.AddMemoryCache();

        // Core Evaluators
        services.AddScoped<IPermissionEvaluator, CachedPermissionEvaluator>();
        services.AddScoped<IDataScopeEvaluator, DataScopeEvaluator>();
        services.AddScoped<IApprovalLimitChecker, ApprovalLimitChecker>();
        services.AddScoped<IAccessGovernanceService, AccessGovernanceService>();

        // Custom Dynamic Authorization Policies
        services.AddSingleton<IAuthorizationPolicyProvider, PermissionPolicyProvider>();
        services.AddScoped<IAuthorizationHandler, PermissionAuthorizationHandler>();

        return services;
    }
}

