using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace EnterpriseBpm.SharedKernel.Logging;

/// <summary>
/// Middleware gắn và theo dõi mã Correlation ID xuyên suốt qua toàn bộ các module và HTTP Request (TSK-809)
/// </summary>
public class CorrelationIdMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<CorrelationIdMiddleware> _logger;
    public const string CorrelationIdHeader = "X-Correlation-ID";

    public CorrelationIdMiddleware(RequestDelegate next, ILogger<CorrelationIdMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // 1. Lấy Correlation ID từ Request Header hoặc sinh mới
        string correlationId;
        if (context.Request.Headers.TryGetValue(CorrelationIdHeader, out var val) && !string.IsNullOrWhiteSpace(val))
        {
            correlationId = val.ToString();
        }
        else
        {
            correlationId = Guid.NewGuid().ToString("D");
        }

        // 2. Gắn vào HttpContext và Response Header
        context.Items[CorrelationIdHeader] = correlationId;
        context.Response.Headers[CorrelationIdHeader] = correlationId;

        // 3. Đưa vào Scope Log có cấu trúc (Structured Logging)
        using (_logger.BeginScope(new Dictionary<string, object>
        {
            ["CorrelationId"] = correlationId,
            ["TraceId"] = context.TraceIdentifier
        }))
        {
            await _next(context);
        }
    }
}

