using EnterpriseBpm.Analytics.Application.Contracts;
using EnterpriseBpm.Analytics.Application.Services;
using EnterpriseBpm.Analytics.Infrastructure.BackgroundWorkers;
using EnterpriseBpm.Analytics.Infrastructure.Persistence;
using EnterpriseBpm.Analytics.Infrastructure.Repositories;
using EnterpriseBpm.Analytics.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace EnterpriseBpm.Analytics.Infrastructure.Extensions;

public static class AnalyticsServiceExtensions
{
    public static IServiceCollection AddAnalyticsModule(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? "Server=(localdb)\\mssqllocaldb;Database=EnterpriseBpmPlatformDb;Trusted_Connection=True;MultipleActiveResultSets=true";

        // 1. Database Context
        services.AddDbContext<AnalyticsDbContext>(options =>
        {
            options.UseSqlServer(connectionString, sql =>
            {
                sql.MigrationsHistoryTable("__EFMigrationsHistory", "analytics");
                sql.EnableRetryOnFailure(3, TimeSpan.FromSeconds(2), null);
            });
        });

        // 2. Core High-Performance Calculation Engines
        services.AddSingleton<ISlaCalculationEngine, BusinessHoursSlaEngine>();
        services.AddSingleton<DynamicQueryBuilderEngine>();

        // 3. Dapper Repository & Query Services
        services.AddScoped<IAnalyticsDapperRepository, AnalyticsDapperRepository>();
        services.AddScoped<IBottleneckAnalysisService, BottleneckAnalysisService>();
        services.AddScoped<IEmployeeScorecardService, EmployeeScorecardService>();
        services.AddScoped<IFinancialAnalyticsService, FinancialAnalyticsService>();
        services.AddScoped<IExecutiveDashboardService, ExecutiveDashboardService>();
        services.AddScoped<IDynamicReportService, DynamicReportService>();
        services.AddScoped<IReportSchedulerService, ReportSchedulerService>();
        services.AddScoped<IMetricsAggregationService, MetricsAggregationService>();

        // 4. Background Workers (Metrics Pre-Aggregation & Report Distribution)
        services.AddHostedService<MetricsAggregationBackgroundWorker>();
        services.AddHostedService<ScheduledReportDistributionWorker>();

        return services;
    }
}

