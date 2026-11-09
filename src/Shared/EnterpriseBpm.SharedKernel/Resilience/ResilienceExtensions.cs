using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace EnterpriseBpm.SharedKernel.Resilience;

/// <summary>
/// Tiện ích đăng ký các Pipeline chống chịu lỗi (Resilience Pipelines) cho HttpClient (TSK-803)
/// </summary>
public static class ResilienceExtensions
{
    public static IHttpClientBuilder AddResilientPipeline(
        this IHttpClientBuilder builder,
        string pipelineName,
        double failureThresholdRate = 0.5,
        int maxRetries = 3,
        TimeSpan? samplingDuration = null,
        TimeSpan? breakDuration = null)
    {
        var circuitBreaker = new CircuitBreakerPolicy(
            pipelineName,
            failureThresholdRate,
            samplingDuration,
            breakDuration);

        builder.AddHttpMessageHandler(sp =>
        {
            var logger = sp.GetService<ILogger<ResilientDelegatingHandler>>();
            return new ResilientDelegatingHandler(circuitBreaker, maxRetries, logger: logger);
        });

        return builder;
    }
}

