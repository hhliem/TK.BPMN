using EnterpriseBpm.Notification.Application.Contracts;
using EnterpriseBpm.Notification.Infrastructure.Persistence;
using EnterpriseBpm.Notification.Infrastructure.Services;
using EnterpriseBpm.Notification.Infrastructure.Workers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace EnterpriseBpm.Notification.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddNotificationInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Chuỗi kết nối 'DefaultConnection' chưa được cấu hình.");

        services.AddDbContext<NotificationDbContext>(options =>
            options.UseSqlServer(connectionString, sql =>
            {
                sql.MigrationsHistoryTable("__EFMigrationsHistory", NotificationDbContext.Schema);
                sql.EnableRetryOnFailure(3);
            }));

        // Bounded Queue
        services.AddSingleton<INotificationQueue, NotificationQueue>(_ => new NotificationQueue(1000));

        // Providers
        services.AddScoped<IMailProvider, O365GraphMailProvider>();
        services.AddHttpClient<ITeamsWebhookProvider, TeamsWebhookProvider>();

        // Services
        services.AddScoped<INotificationDispatcher, NotificationDispatcher>();
        services.AddScoped<INotificationTemplateService, NotificationTemplateService>();
        services.AddScoped<INotificationLogService, NotificationLogService>();

        // Background Worker
        services.AddHostedService<NotificationBackgroundWorker>();

        // MediatR Handlers registration
        services.AddMediatR(cfg =>
            cfg.RegisterServicesFromAssembly(typeof(Application.Handlers.TaskAssignedNotificationHandler).Assembly));

        return services;
    }
}

