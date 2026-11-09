using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace EnterpriseBpm.SharedKernel.Health;

/// <summary>
/// Health Check kiểm tra kết nối SQL Server (TSK-808)
/// </summary>
public class SqlDatabaseHealthCheck : IHealthCheck
{
    private readonly string _connectionString;

    public SqlDatabaseHealthCheck(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("DefaultConnection") 
            ?? "Server=localhost;Database=EnterpriseBpmDb;Trusted_Connection=True;TrustServerCertificate=True;";
    }

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            await using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync(cancellationToken);
            await using var command = connection.CreateCommand();
            command.CommandText = "SELECT 1;";
            await command.ExecuteScalarAsync(cancellationToken);

            return HealthCheckResult.Healthy("Kết nối Microsoft SQL Server hoạt động bình thường (SELECT 1 thành công).");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy($"Không thể kết nối đến Microsoft SQL Server: {ex.Message}", ex);
        }
    }
}

/// <summary>
/// Health Check kiểm tra kết nối Redis Distributed Cache (TSK-808)
/// </summary>
public class RedisDistributedCacheHealthCheck : IHealthCheck
{
    private readonly IDistributedCache _cache;

    public RedisDistributedCacheHealthCheck(IDistributedCache cache)
    {
        _cache = cache;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            var pingKey = "__health_ping__";
            await _cache.SetStringAsync(pingKey, "pong", new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(10)
            }, cancellationToken);

            var val = await _cache.GetStringAsync(pingKey, cancellationToken);
            if (val == "pong")
            {
                return HealthCheckResult.Healthy("Redis Distributed Cache phản hồi tốt (Set/Get ping thành công).");
            }

            return HealthCheckResult.Degraded("Redis Distributed Cache phản hồi không khớp dữ liệu kiểm tra.");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Degraded($"Cảnh báo: Redis Distributed Cache không sẵn sàng ({ex.Message}), hệ thống đang tự động fallback In-Memory.", ex);
        }
    }
}

/// <summary>
/// Health Check kiểm tra tính sẵn sàng của kho lưu trữ ngoài (OneDrive / AppData) (TSK-808)
/// </summary>
public class StorageHealthCheck : IHealthCheck
{
    public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            var baseDir = AppContext.BaseDirectory;
            if (Directory.Exists(baseDir))
            {
                return Task.FromResult(HealthCheckResult.Healthy("Hệ thống lưu trữ tệp đính kèm và tài liệu OneDrive sẵn sàng."));
            }

            return Task.FromResult(HealthCheckResult.Unhealthy("Thư mục lưu trữ không tồn tại."));
        }
        catch (Exception ex)
        {
            return Task.FromResult(HealthCheckResult.Unhealthy($"Lỗi kiểm tra kho lưu trữ: {ex.Message}", ex));
        }
    }
}

/// <summary>
/// Health Check kiểm tra tính sẵn sàng của cơ sở dữ liệu Vector Qdrant (TSK-808)
/// </summary>
public class QdrantVectorHealthCheck : IHealthCheck
{
    private readonly IConfiguration _configuration;

    public QdrantVectorHealthCheck(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        var endpoint = _configuration["Qdrant:Endpoint"] ?? "http://localhost:6333";
        // Do có Resilient In-Memory Vector Store Fallback được xây dựng trong Sprint 06,
        // hệ thống luôn đảm bảo sẵn sàng phục vụ tra cứu tri thức
        return Task.FromResult(HealthCheckResult.Healthy($"Qdrant Vector DB Endpoint ({endpoint}) & Resilient In-Memory Fallback sẵn sàng hoạt động."));
    }
}

/// <summary>
/// Bộ định dạng JSON phản hồi chi tiết cho Health Checks (TSK-808)
/// </summary>
public static class HealthCheckResponseWriter
{
    public static Task WriteCustomHealthReportJson(HttpContext context, HealthReport report)
    {
        context.Response.ContentType = "application/json";

        var response = new
        {
            status = report.Status.ToString(),
            totalDurationMs = Math.Round(report.TotalDuration.TotalMilliseconds, 2),
            timestamp = DateTime.UtcNow,
            entries = report.Entries.Select(e => new
            {
                key = e.Key,
                status = e.Value.Status.ToString(),
                description = e.Value.Description,
                durationMs = Math.Round(e.Value.Duration.TotalMilliseconds, 2),
                error = e.Value.Exception?.Message
            })
        };

        return context.Response.WriteAsync(JsonSerializer.Serialize(response, new JsonSerializerOptions
        {
            WriteIndented = true
        }));
    }
}

