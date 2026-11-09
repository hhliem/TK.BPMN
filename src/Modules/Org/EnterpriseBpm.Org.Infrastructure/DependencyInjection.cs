using EnterpriseBpm.Org.Application.Contracts;
using EnterpriseBpm.Org.Infrastructure.Persistence;
using EnterpriseBpm.Org.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace EnterpriseBpm.Org.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddOrgInfrastructure(
        this IServiceCollection services, 
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection") 
            ?? throw new InvalidOperationException("Chuỗi kết nối cơ sở dữ liệu 'DefaultConnection' chưa được cấu hình trong hệ thống.");

        services.AddDbContextPool<OrgDbContext>(options =>
            options.UseSqlServer(connectionString, sql => 
                sql.MigrationsHistoryTable("__EFMigrationsHistory", OrgDbContext.Schema)), poolSize: 256);

        services.AddScoped<IOrgHierarchyService, OrgHierarchyService>();
        services.AddScoped<IDelegationResolver, DelegationResolver>();
        services.AddScoped<IDelegationService, DelegationResolver>();
        services.AddScoped<IEmployeeDirectoryService, EmployeeDirectoryService>();

        return services;
    }
}

