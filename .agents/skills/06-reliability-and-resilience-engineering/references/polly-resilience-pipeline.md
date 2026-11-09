# Hướng dẫn Kỹ thuật Polly v8 Resilience Pipeline

## 1. Kiến trúc Pipeline Chống Chịu Lỗi (Polly v8)

Trong .NET 8/9, Microsoft tích hợp thư viện `Polly` thế hệ mới thông qua package `Microsoft.Extensions.Http.Resilience` với mô hình cấu hình theo pipeline chuẩn:

```
Request ──► [ Rate Limiter ] ──► [ Total Timeout ] ──► [ Retry (Jitter) ] ──► [ Circuit Breaker ] ──► [ Attempt Timeout ] ──► HttpClient Execution
```

### Các Thành phần Trọng yếu:
1. **Total Request Timeout:** Giới hạn tổng thời gian chạy cho toàn bộ request (bao gồm mọi lần retry), mặc định $45\text{ giây}$.
2. **Retry Strategy:**
   - Số lần thử lại tối đa: $3$ lần.
   - Thuật toán: **Exponential Backoff with Jitter** (Thời gian chờ ngẫu nhiên dao động quanh $2^n$ giây) để tránh hiện tượng *Thundering Herd* làm sập dịch vụ ngoài khi dịch vụ vừa phục hồi.
3. **Circuit Breaker (Ngắt mạch tự động):**
   - Nếu tỷ lệ lỗi vượt quá $50\%$ trong cửa sổ $30\text{ giây}$ (với tối thiểu 10 requests), mạch sẽ chuyển sang trạng thái `Open` (Ngắt mạch).
   - Khi mạch ngắt, toàn bộ requests tiếp theo sẽ lập tức bị từ chối bằng exception `BrokenCircuitException` mà không cần gửi ra mạng, bảo vệ hệ thống khỏi treo luồng (Thread Exhaustion).
   - Sau $15\text{ giây}$, mạch chuyển sang `Half-Open` để cho một vài requests thử nghiệm đi qua. Nếu thành công, mạch đóng lại (`Closed`).

---

## 2. Đăng ký C# Mẫu trong Program.cs

```csharp
builder.Services.AddHttpClient("MicrosoftGraphClient", client =>
{
    client.BaseAddress = new Uri("https://graph.microsoft.com/v1.0/");
    client.DefaultRequestHeaders.Add("Accept", "application/json");
})
.AddStandardResilienceHandler(options =>
{
    options.Retry.MaxRetryAttempts = 3;
    options.Retry.BackoffType = DelayBackoffType.Exponential;
    options.Retry.UseJitter = true;
    options.Retry.Delay = TimeSpan.FromSeconds(2);

    options.CircuitBreaker.SamplingDuration = TimeSpan.FromSeconds(30);
    options.CircuitBreaker.FailureRatio = 0.5;
    options.CircuitBreaker.MinimumThroughput = 10;
    options.CircuitBreaker.BreakDuration = TimeSpan.FromSeconds(15);

    options.TotalRequestTimeout.Timeout = TimeSpan.FromSeconds(45);
    options.AttemptTimeout.Timeout = TimeSpan.FromSeconds(15);
});
```

