using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http.Resilience;
using Polly;

namespace EnterpriseBpm.DocumentManagement.Infrastructure.Resilience;

public static class GraphApiResilienceExtensions
{
    /// <summary>
    /// Đăng ký Resilience Pipeline chuẩn Polly v8 cho các cuộc gọi Microsoft Graph API
    /// Bao gồm: Rate Limiting, Retry with Exponential Backoff Jitter, Circuit Breaker.
    /// </summary>
    public static IHttpClientBuilder AddGraphApiResilientClient(
        this IServiceCollection services, 
        string clientName = "MicrosoftGraphClient")
    {
        return services.AddHttpClient(clientName, client =>
        {
            client.BaseAddress = new Uri("https://graph.microsoft.com/v1.0/");
            client.DefaultRequestHeaders.Add("User-Agent", "EnterpriseBpm-DocumentModule/1.0");
        })
        .AddStandardResilienceHandler(options =>
        {
            // 1. Cấu hình Retry khi gặp mã lỗi 429 (Too Many Requests), 503, 504
            options.Retry.MaxRetryAttempts = 3;
            options.Retry.BackoffType = DelayBackoffType.Exponential;
            options.Retry.UseJitter = true; // Ngăn chặn xung đột đồng thời (Thundering Herd)
            options.Retry.Delay = TimeSpan.FromSeconds(2);

            // 2. Cấu hình Circuit Breaker ngắt kết nối khi Graph API sập kéo dài
            options.CircuitBreaker.SamplingDuration = TimeSpan.FromSeconds(30);
            options.CircuitBreaker.FailureRatio = 0.5; // Ngắt mạch nếu lỗi vượt 50%
            options.CircuitBreaker.MinimumThroughput = 10;
            options.CircuitBreaker.BreakDuration = TimeSpan.FromSeconds(20);

            // 3. Cấu hình Timeout
            options.AttemptTimeout.Timeout = TimeSpan.FromSeconds(15);
            options.TotalRequestTimeout.Timeout = TimeSpan.FromSeconds(45);
        });
    }
}

