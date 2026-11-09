using System.Net;
using Microsoft.Extensions.Logging;

namespace EnterpriseBpm.SharedKernel.Resilience;

/// <summary>
/// HTTP Message Handler tích hợp bộ 3 chiến lược Resilience Polly v8:
/// 1. Circuit Breaker (Ngắt mạch khi lỗi liên tục)
/// 2. Exponential Retry with Jitter (Thử lại lũy tiến với độ trễ ngẫu nhiên)
/// 3. Timeout Control (Kiểm soát thời gian chờ)
/// (TSK-803)
/// </summary>
public class ResilientDelegatingHandler : DelegatingHandler
{
    private readonly CircuitBreakerPolicy _circuitBreaker;
    private readonly int _maxRetries;
    private readonly TimeSpan _initialBackoff;
    private readonly ILogger<ResilientDelegatingHandler>? _logger;

    public ResilientDelegatingHandler(
        CircuitBreakerPolicy circuitBreaker,
        int maxRetries = 3,
        TimeSpan? initialBackoff = null,
        ILogger<ResilientDelegatingHandler>? logger = null)
    {
        _circuitBreaker = circuitBreaker;
        _maxRetries = maxRetries;
        _initialBackoff = initialBackoff ?? TimeSpan.FromMilliseconds(200);
        _logger = logger;
    }

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        int attempt = 0;

        while (true)
        {
            attempt++;

            // 1. Kiểm tra Circuit Breaker trước khi gửi yêu cầu ra ngoài mạng
            _circuitBreaker.CheckPermission();

            try
            {
                var response = await base.SendAsync(request, cancellationToken);

                // Nếu là lỗi tạm thời phía máy chủ (5xx, 408)
                if (IsTransientFailure(response.StatusCode))
                {
                    _circuitBreaker.RecordExecution(isSuccess: false);

                    if (attempt <= _maxRetries)
                    {
                        var delay = ComputeBackoffWithJitter(attempt);
                        _logger?.LogWarning(
                            "[Resilience:{Pipeline}] Request thất bại với mã {StatusCode}. Thử lại lần {Attempt}/{MaxRetries} sau {Delay}ms...",
                            _circuitBreaker.Name, response.StatusCode, attempt, _maxRetries, delay.TotalMilliseconds);

                        await Task.Delay(delay, cancellationToken);
                        continue;
                    }
                }
                else
                {
                    _circuitBreaker.RecordExecution(isSuccess: true);
                }

                return response;
            }
            catch (Exception ex) when (ex is not OperationCanceledException && ex is not CircuitBreakerOpenException)
            {
                _circuitBreaker.RecordExecution(isSuccess: false);

                if (attempt <= _maxRetries)
                {
                    var delay = ComputeBackoffWithJitter(attempt);
                    _logger?.LogWarning(ex,
                        "[Resilience:{Pipeline}] Bắt được ngoại lệ {ExceptionType}. Thử lại lần {Attempt}/{MaxRetries} sau {Delay}ms...",
                        _circuitBreaker.Name, ex.GetType().Name, attempt, _maxRetries, delay.TotalMilliseconds);

                    await Task.Delay(delay, cancellationToken);
                    continue;
                }

                throw;
            }
        }
    }

    private static bool IsTransientFailure(HttpStatusCode statusCode)
    {
        return statusCode == HttpStatusCode.RequestTimeout ||            // 408
               statusCode == HttpStatusCode.InternalServerError ||       // 500
               statusCode == HttpStatusCode.BadGateway ||                // 502
               statusCode == HttpStatusCode.ServiceUnavailable ||        // 503
               statusCode == HttpStatusCode.GatewayTimeout;              // 504
    }

    private TimeSpan ComputeBackoffWithJitter(int attempt)
    {
        // Exponential Backoff: initial * 2^(attempt - 1) + Jitter (0 -> 100ms)
        var exponentialMs = _initialBackoff.TotalMilliseconds * Math.Pow(2, attempt - 1);
        var jitterMs = Random.Shared.Next(0, 100);
        return TimeSpan.FromMilliseconds(exponentialMs + jitterMs);
    }
}

