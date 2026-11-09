namespace EnterpriseBpm.SharedKernel.Resilience;

/// <summary>
/// Trạng thái ngắt mạch của Circuit Breaker (TSK-803)
/// </summary>
public enum CircuitState
{
    Closed = 0,    // Mạch đóng: Luồng lưu thông bình thường
    Open = 1,      // Mạch ngắt: Lập tức từ chối request để chống Thread Exhaustion
    HalfOpen = 2   // Nửa mở: Thử nghiệm cho 1 request thăm dò đi qua
}

public class CircuitBreakerOpenException : Exception
{
    public string PipelineName { get; }
    public TimeSpan RemainingDuration { get; }

    public CircuitBreakerOpenException(string pipelineName, TimeSpan remainingDuration)
        : base($"[CircuitBreaker] Mạch '{pipelineName}' đang ở trạng thái OPEN. Yêu cầu bị ngắt ngay lập tức để bảo vệ tài nguyên máy chủ. Thử lại sau {remainingDuration.TotalSeconds:F1} giây.")
    {
        PipelineName = pipelineName;
        RemainingDuration = remainingDuration;
    }
}

