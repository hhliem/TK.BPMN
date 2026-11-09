using EnterpriseBpm.SystemSettings.Application.Contracts;
using EnterpriseBpm.SystemSettings.Infrastructure.Persistence;
using EnterpriseBpm.SystemSettings.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace EnterpriseBpm.SystemSettings.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddSystemSettingsInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Chuỗi kết nối 'DefaultConnection' chưa được cấu hình.");

        services.AddDbContextPool<SettingsDbContext>(options =>
            options.UseSqlServer(connectionString, sql =>
            {
                sql.MigrationsHistoryTable("__EFMigrationsHistory", SettingsDbContext.Schema);
                sql.EnableRetryOnFailure(3);
            }), poolSize: 256);

        services.AddMemoryCache();
        services.AddDataProtection();
        services.AddSingleton<ISettingEncryptionService, DataProtectionEncryptionService>();
        services.AddScoped<ISystemSettingService, SystemSettingService>();

        return services;
    }
}

