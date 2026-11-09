using EnterpriseBpm.AuditLog.Application.Contracts;
using EnterpriseBpm.AuditLog.Infrastructure.Interceptors;
using EnterpriseBpm.AuditLog.Infrastructure.Persistence;
using EnterpriseBpm.AuditLog.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace EnterpriseBpm.AuditLog.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddAuditLogInfrastructure(
        this IServiceCollection services, 
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection") 
            ?? throw new InvalidOperationException("Chuỗi kết nối cơ sở dữ liệu 'DefaultConnection' chưa được cấu hình trong hệ thống.");

        services.AddScoped<AuditSaveChangesInterceptor>();

        services.AddDbContext<AuditDbContext>((sp, options) =>
        {
            var interceptor = sp.GetRequiredService<AuditSaveChangesInterceptor>();
            options.UseSqlServer(connectionString, sql => 
                sql.MigrationsHistoryTable("__EFMigrationsHistory", AuditDbContext.Schema))
                   .AddInterceptors(interceptor);
        });

        services.AddScoped<IAuditLogService, AuditLogService>();
        services.AddScoped<IAuditQueryService, AuditQueryService>();

        return services;
    }
}

