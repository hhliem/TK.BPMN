using EnterpriseBpm.Analytics.Application.Contracts;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace EnterpriseBpm.Analytics.Infrastructure.BackgroundWorkers;

/// <summary>
/// Background Worker định kỳ tổng hợp chỉ số Materialized Metrics Big Data (TSK-703)
/// </summary>
public class MetricsAggregationBackgroundWorker : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<MetricsAggregationBackgroundWorker> _logger;

    public MetricsAggregationBackgroundWorker(
        IServiceProvider serviceProvider,
        ILogger<MetricsAggregationBackgroundWorker> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("MetricsAggregationBackgroundWorker đã khởi động.");

        // Chờ 10 giây ban đầu để hệ thống ổn định
        await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var aggregationService = scope.ServiceProvider.GetRequiredService<IMetricsAggregationService>();

                // Tổng hợp điểm nghẽn SLA
                await aggregationService.AggregateStepSlaMetricsAsync(stoppingToken);

                // Tổng hợp số liệu ngày hôm nay
                await aggregationService.AggregateWorkflowMetricsAsync(DateTime.UtcNow, stoppingToken);

                // Tổng hợp năng suất tháng hiện tại
                await aggregationService.AggregateEmployeeProductivityAsync(DateTime.UtcNow.ToString("yyyy-MM"), stoppingToken);

                _logger.LogInformation("MetricsAggregationBackgroundWorker: Hoàn tất đợt tổng hợp dữ liệu định kỳ.");
            }
            catch (Exception ex) when (!stoppingToken.IsCancellationRequested)
            {
                _logger.LogError(ex, "Lỗi trong chu kỳ tổng hợp MetricsAggregationBackgroundWorker");
            }

            // Chờ 30 phút cho chu kỳ tiếp theo
            await Task.Delay(TimeSpan.FromMinutes(30), stoppingToken);
        }
    }
}

/// <summary>
/// Background Worker kiểm tra lịch phát hành báo cáo tự động (TSK-709)
/// </summary>
public class ScheduledReportDistributionWorker : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<ScheduledReportDistributionWorker> _logger;

    public ScheduledReportDistributionWorker(
        IServiceProvider serviceProvider,
        ILogger<ScheduledReportDistributionWorker> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("ScheduledReportDistributionWorker đã khởi động.");

        // Chờ 15 giây ban đầu
        await Task.Delay(TimeSpan.FromSeconds(15), stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var scheduler = scope.ServiceProvider.GetRequiredService<IReportSchedulerService>();

                await scheduler.ProcessDueSubscriptionsAsync(stoppingToken);
            }
            catch (Exception ex) when (!stoppingToken.IsCancellationRequested)
            {
                _logger.LogError(ex, "Lỗi trong ScheduledReportDistributionWorker");
            }

            // Quét mỗi 60 giây
            await Task.Delay(TimeSpan.FromSeconds(60), stoppingToken);
        }
    }
}

