using Microsoft.AspNetCore.Http;

namespace EnterpriseBpm.SharedKernel.Security;

/// <summary>
/// Middleware thiết lập các HTTP Security Headers tuân thủ OWASP Top 10 (TSK-810)
/// Bảo vệ hệ thống khỏi các lỗ hổng XSS, Clickjacking, MIME-sniffing.
/// </summary>
public class SecurityHeadersMiddleware
{
    private readonly RequestDelegate _next;

    public SecurityHeadersMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // 1. Chống MIME Type Confusion / Sniffing
        context.Response.Headers.Append("X-Content-Type-Options", "nosniff");

        // 2. Chống Clickjacking
        context.Response.Headers.Append("X-Frame-Options", "SAMEORIGIN");

        // 3. Kích hoạt bộ lọc XSS của trình duyệt
        context.Response.Headers.Append("X-XSS-Protection", "1; mode=block");

        // 4. Bảo vệ thông tin Referrer
        context.Response.Headers.Append("Referrer-Policy", "strict-origin-when-cross-origin");

        // 5. Kiểm soát quyền thiết bị phần cứng
        context.Response.Headers.Append("Permissions-Policy", "camera=(), microphone=(), geolocation=()");

        await _next(context);
    }
}

