using EnterpriseBpm.Form.Application.Contracts;
using EnterpriseBpm.Form.Infrastructure.Persistence;
using EnterpriseBpm.Form.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace EnterpriseBpm.Form.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddFormInfrastructure(
        this IServiceCollection services, 
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection") 
            ?? throw new InvalidOperationException("Chuỗi kết nối cơ sở dữ liệu 'DefaultConnection' chưa được cấu hình trong hệ thống.");

        services.AddDbContextPool<FormDbContext>(options =>
            options.UseSqlServer(connectionString, sql => 
                sql.MigrationsHistoryTable("__EFMigrationsHistory", FormDbContext.Schema)), poolSize: 256);

        services.AddMemoryCache();
        services.AddScoped<IFormVersionService, FormVersionService>();
        services.AddScoped<IMasterDataService, MasterDataService>();

        return services;
    }
}

