using System.Collections.Concurrent;

namespace EnterpriseBpm.SharedKernel.Resilience;

/// <summary>
/// Chính sách ngắt mạch Circuit Breaker tự phục hồi chuẩn Polly v8 (TSK-803)
/// Ngăn chặn Thread Exhaustion và Cascading Failures khi dịch vụ bên ngoài bị sự cố liên tục.
/// </summary>
public class CircuitBreakerPolicy
{
    public string Name { get; }
    public double FailureThresholdRate { get; } // 0.5 = 50%
    public TimeSpan SamplingDuration { get; }   // Thời gian cửa sổ thống kê (vd: 30s)
    public TimeSpan BreakDuration { get; }      // Thời gian duy trì mạch mở trước khi thử lại (vd: 15s)
    public int MinimumThroughput { get; }       // Số lượng request tối thiểu trước khi tính tỷ lệ (vd: 4)

    private CircuitState _state = CircuitState.Closed;
    private DateTime _lastStateChangedAt = DateTime.UtcNow;
    private readonly object _syncLock = new();

    private readonly ConcurrentQueue<(DateTime Timestamp, bool IsSuccess)> _recentExecutions = new();

    public CircuitState State
    {
        get
        {
            lock (_syncLock)
            {
                if (_state == CircuitState.Open && DateTime.UtcNow - _lastStateChangedAt >= BreakDuration)
                {
                    _state = CircuitState.HalfOpen;
                    _lastStateChangedAt = DateTime.UtcNow;
                }
                return _state;
            }
        }
    }

    public CircuitBreakerPolicy(
        string name,
        double failureThresholdRate = 0.5,
        TimeSpan? samplingDuration = null,
        TimeSpan? breakDuration = null,
        int minimumThroughput = 4)
    {
        Name = name;
        FailureThresholdRate = failureThresholdRate;
        SamplingDuration = samplingDuration ?? TimeSpan.FromSeconds(30);
        BreakDuration = breakDuration ?? TimeSpan.FromSeconds(15);
        MinimumThroughput = minimumThroughput;
    }

    public void CheckPermission()
    {
        var currentState = State;
        if (currentState == CircuitState.Open)
        {
            var remaining = BreakDuration - (DateTime.UtcNow - _lastStateChangedAt);
            if (remaining < TimeSpan.Zero) remaining = TimeSpan.Zero;
            throw new CircuitBreakerOpenException(Name, remaining);
        }
    }

    public void RecordExecution(bool isSuccess)
    {
        var now = DateTime.UtcNow;
        _recentExecutions.Enqueue((now, isSuccess));

        // Dọn dẹp các mẫu quá hạn SamplingDuration
        var cutoff = now - SamplingDuration;
        while (_recentExecutions.TryPeek(out var item) && item.Timestamp < cutoff)
        {
            _recentExecutions.TryDequeue(out _);
        }

        lock (_syncLock)
        {
            if (_state == CircuitState.HalfOpen)
            {
                if (isSuccess)
                {
                    _state = CircuitState.Closed;
                    _lastStateChangedAt = now;
                    _recentExecutions.Clear();
                }
                else
                {
                    _state = CircuitState.Open;
                    _lastStateChangedAt = now;
                }
                return;
            }

            if (_state == CircuitState.Closed)
            {
                var samples = _recentExecutions.ToArray();
                if (samples.Length >= MinimumThroughput)
                {
                    var failCount = samples.Count(s => !s.IsSuccess);
                    var failureRate = (double)failCount / samples.Length;

                    if (failureRate >= FailureThresholdRate)
                    {
                        _state = CircuitState.Open;
                        _lastStateChangedAt = now;
                    }
                }
            }
        }
    }

    public void Trip()
    {
        lock (_syncLock)
        {
            _state = CircuitState.Open;
            _lastStateChangedAt = DateTime.UtcNow;
        }
    }

    public void Reset()
    {
        lock (_syncLock)
        {
            _state = CircuitState.Closed;
            _lastStateChangedAt = DateTime.UtcNow;
            _recentExecutions.Clear();
        }
    }

    public async Task<TResult> ExecuteAsync<TResult>(Func<Task<TResult>> action)
    {
        CheckPermission();
        try
        {
            var result = await action();
            RecordExecution(true);
            return result;
        }
        catch (CircuitBreakerOpenException)
        {
            throw;
        }
        catch (Exception)
        {
            RecordExecution(false);
            throw;
        }
    }

    public async Task ExecuteAsync(Func<Task> action)
    {
        CheckPermission();
        try
        {
            await action();
            RecordExecution(true);
        }
        catch (CircuitBreakerOpenException)
        {
            throw;
        }
        catch (Exception)
        {
            RecordExecution(false);
            throw;
        }
    }
}

