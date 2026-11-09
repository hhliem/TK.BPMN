using EnterpriseBpm.InternalNews.Application.Contracts;
using EnterpriseBpm.InternalNews.Infrastructure.Persistence;
using EnterpriseBpm.InternalNews.Infrastructure.Services;
using EnterpriseBpm.InternalNews.Infrastructure.Workers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace EnterpriseBpm.InternalNews.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInternalNewsInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Chuỗi kết nối 'DefaultConnection' chưa được cấu hình.");

        services.AddDbContextPool<NewsDbContext>(options =>
            options.UseSqlServer(connectionString, sql =>
            {
                sql.MigrationsHistoryTable("__EFMigrationsHistory", NewsDbContext.Schema);
                sql.EnableRetryOnFailure(3);
            }), poolSize: 256);

        services.AddScoped<INewsService, NewsService>();
        services.AddHostedService<ScheduledNewsPublisherWorker>();

        return services;
    }
}

