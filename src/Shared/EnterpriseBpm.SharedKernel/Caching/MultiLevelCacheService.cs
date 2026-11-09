using System.Collections.Concurrent;
using System.Text.Json;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace EnterpriseBpm.SharedKernel.Caching;

/// <summary>
/// Triển khai Caching 2 tầng:
/// - L1: In-Memory (IMemoryCache) - Phản hồi tức thì < 1ms
/// - L2: Distributed Cache (IDistributedCache / Redis) - Đồng bộ cụm phân tán
/// - Cache Stampede Protection qua SemaphoreSlim
/// (TSK-804)
/// </summary>
public class MultiLevelCacheService : IMultiLevelCacheService
{
    private readonly IMemoryCache _memoryCache;
    private readonly IDistributedCache _distributedCache;
    private readonly ILogger<MultiLevelCacheService> _logger;

    private static readonly ConcurrentDictionary<string, SemaphoreSlim> KeyLocks = new();
    private static readonly ConcurrentDictionary<string, byte> TrackedKeys = new();

    private readonly TimeSpan _defaultL1Ttl = TimeSpan.FromMinutes(10);
    private readonly TimeSpan _defaultL2Ttl = TimeSpan.FromHours(1);

    public MultiLevelCacheService(
        IMemoryCache memoryCache,
        IDistributedCache distributedCache,
        ILogger<MultiLevelCacheService> logger)
    {
        _memoryCache = memoryCache;
        _distributedCache = distributedCache;
        _logger = logger;
    }

    public async Task<T?> GetOrSetAsync<T>(
        string key,
        Func<Task<T>> factory,
        TimeSpan? l1Ttl = null,
        TimeSpan? l2Ttl = null,
        CancellationToken cancellationToken = default)
    {
        // 1. Thử đọc từ L1 In-Memory Cache (Latency < 1ms)
        if (_memoryCache.TryGetValue(key, out T? l1Value) && l1Value != null)
        {
            _logger.LogTrace("[Cache] L1 HIT cho khóa: {Key}", key);
            return l1Value;
        }

        // 2. Thử đọc từ L2 Distributed Cache (Redis)
        try
        {
            var l2Bytes = await _distributedCache.GetAsync(key, cancellationToken);
            if (l2Bytes != null && l2Bytes.Length > 0)
            {
                var deserialized = JsonSerializer.Deserialize<T>(l2Bytes);
                if (deserialized != null)
                {
                    _logger.LogTrace("[Cache] L2 HIT cho khóa: {Key}. Đồng bộ ngược về L1.", key);
                    _memoryCache.Set(key, deserialized, l1Ttl ?? _defaultL1Ttl);
                    TrackedKeys.TryAdd(key, 0);
                    return deserialized;
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "[Cache] Lỗi khi truy vấn L2 Distributed Cache cho khóa {Key}. Tiếp tục fallback sang DB.", key);
        }

        // 3. Cache Miss cả 2 tầng: Dùng SemaphoreSlim chống Cache Stampede (nhiều thread truy vấn DB cùng lúc)
        var keyLock = KeyLocks.GetOrAdd(key, _ => new SemaphoreSlim(1, 1));
        await keyLock.WaitAsync(cancellationToken);

        try
        {
            // Double-check L1 sau khi giành được lock
            if (_memoryCache.TryGetValue(key, out l1Value) && l1Value != null)
            {
                return l1Value;
            }

            _logger.LogDebug("[Cache] L1 & L2 MISS cho khóa: {Key}. Thực thi truy vấn cơ sở dữ liệu...", key);
            var dbValue = await factory();

            if (dbValue != null)
            {
                await SetAsync(key, dbValue, l1Ttl, l2Ttl, cancellationToken);
            }

            return dbValue;
        }
        finally
        {
            keyLock.Release();
        }
    }

    public async Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default)
    {
        if (_memoryCache.TryGetValue(key, out T? l1Val) && l1Val != null)
        {
            return l1Val;
        }

        try
        {
            var l2Bytes = await _distributedCache.GetAsync(key, cancellationToken);
            if (l2Bytes != null && l2Bytes.Length > 0)
            {
                var deserialized = JsonSerializer.Deserialize<T>(l2Bytes);
                if (deserialized != null)
                {
                    _memoryCache.Set(key, deserialized, _defaultL1Ttl);
                    TrackedKeys.TryAdd(key, 0);
                    return deserialized;
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "[Cache] Lỗi đọc L2 cho khóa {Key}", key);
        }

        return default;
    }

    public async Task SetAsync<T>(
        string key,
        T value,
        TimeSpan? l1Ttl = null,
        TimeSpan? l2Ttl = null,
        CancellationToken cancellationToken = default)
    {
        if (value == null) return;

        // Lưu L1 Memory
        _memoryCache.Set(key, value, l1Ttl ?? _defaultL1Ttl);
        TrackedKeys.TryAdd(key, 0);

        // Lưu L2 Distributed Redis
        try
        {
            var json = JsonSerializer.Serialize(value);
            var options = new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = l2Ttl ?? _defaultL2Ttl
            };
            await _distributedCache.SetStringAsync(key, json, options, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "[Cache] Lỗi ghi L2 Distributed Cache cho khóa {Key}", key);
        }
    }

    public async Task RemoveAsync(string key, CancellationToken cancellationToken = default)
    {
        _memoryCache.Remove(key);
        TrackedKeys.TryRemove(key, out _);

        try
        {
            await _distributedCache.RemoveAsync(key, cancellationToken);
            _logger.LogDebug("[Cache] Đã xóa cache cho khóa: {Key}", key);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "[Cache] Lỗi xóa L2 cho khóa {Key}", key);
        }
    }

    public async Task RemoveByPrefixAsync(string prefix, CancellationToken cancellationToken = default)
    {
        var keysToRemove = TrackedKeys.Keys
            .Where(k => k.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            .ToList();

        foreach (var key in keysToRemove)
        {
            await RemoveAsync(key, cancellationToken);
        }

        _logger.LogInformation("[Cache] Đã xóa toàn bộ {Count} khóa cache bắt đầu với tiền tố: '{Prefix}'", keysToRemove.Count, prefix);
    }
}

