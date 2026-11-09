using EnterpriseBpm.EventBus.Outbox;
using EnterpriseBpm.Workflow.Application.Contracts;
using EnterpriseBpm.Workflow.Application.Handlers;
using EnterpriseBpm.Workflow.Domain.Services;
using EnterpriseBpm.Workflow.Infrastructure.Persistence;
using EnterpriseBpm.Workflow.Infrastructure.Services;
using EnterpriseBpm.Workflow.Infrastructure.Workers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace EnterpriseBpm.Workflow.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddWorkflowInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Chuỗi kết nối cơ sở dữ liệu 'DefaultConnection' chưa được cấu hình trong hệ thống.");

        // DbContext Pooling (TSK-807: MaxPoolSize = 256)
        services.AddDbContextPool<WorkflowDbContext>(options =>
            options.UseSqlServer(connectionString, sqlOptions =>
            {
                sqlOptions.MigrationsHistoryTable("__EFMigrationsHistory", "workflow");
                sqlOptions.EnableRetryOnFailure(3);
            }), poolSize: 256);

        services.AddScoped<IWorkflowDbContextProvider>(sp => sp.GetRequiredService<WorkflowDbContext>());

        // Domain Services
        services.AddSingleton<IDigitalSignatureService, DigitalSignatureService>();

        // Infrastructure & Application Services
        services.AddScoped<IDistributedLockService, RedisDistributedLockService>();
        services.AddScoped<IApprovalHistoryService, ApprovalHistoryService>();
        services.AddScoped<IApprovalTaskDispatcher, ApprovalTaskDispatcher>();
        services.AddScoped<IWorkflowRuntimeEngine, WorkflowRuntimeEngine>();
        services.AddScoped<IWorkflowSubmissionService, WorkflowSubmissionService>();
        services.AddScoped<IApprovalTaskService, ApprovalTaskService>();
        services.AddScoped<IWorkflowDapperReadService, WorkflowDapperReadService>();

        // Transactional Outbox Pattern (TSK-801, TSK-802)
        services.AddScoped<IOutboxStore, WorkflowOutboxStore>();
        services.AddHostedService<OutboxProcessorWorker>();

        // Background SLA Escalation Worker
        services.AddHostedService<SlaEscalationBackgroundWorker>();

        // MediatR
        services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssembly(typeof(EmployeeDeactivatedWorkflowHandler).Assembly);
        });

        return services;
    }
}

