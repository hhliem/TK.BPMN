using System.Collections.Concurrent;
using EnterpriseBpm.Workflow.Application.Contracts;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;

namespace EnterpriseBpm.Workflow.Infrastructure.Services;

public class RedisDistributedLockService : IDistributedLockService
{
    private readonly IDistributedCache _cache;
    private readonly ILogger<RedisDistributedLockService> _logger;
    private static readonly ConcurrentDictionary<string, SemaphoreSlim> LocalLocks = new();

    public RedisDistributedLockService(
        IDistributedCache cache,
        ILogger<RedisDistributedLockService> logger)
    {
        _cache = cache;
        _logger = logger;
    }

    public async Task<IAsyncDisposable?> AcquireLockAsync(
        string resourceKey,
        TimeSpan timeout,
        CancellationToken cancellationToken = default)
    {
        var lockToken = Guid.NewGuid().ToString("N");
        var lockName = $"lock:{resourceKey}";
        var acquired = false;
        var start = DateTime.UtcNow;

        // Cơ chế hàng đợi Semaphore cục bộ để giảm tải tối đa cho Redis bus
        var localSemaphore = LocalLocks.GetOrAdd(resourceKey, _ => new SemaphoreSlim(1, 1));
        var localAcquired = await localSemaphore.WaitAsync(timeout, cancellationToken);
        if (!localAcquired)
        {
            _logger.LogWarning("Không thể lấy local lock cho tài nguyên {ResourceKey} trong thời gian chờ {Timeout}", resourceKey, timeout);
            return null;
        }

        try
        {
            while ((DateTime.UtcNow - start) < timeout && !cancellationToken.IsCancellationRequested)
            {
                var existing = await _cache.GetStringAsync(lockName, cancellationToken);
                if (existing == null)
                {
                    // Đặt Distributed Lock với TTL 10 giây
                    await _cache.SetStringAsync(
                        lockName,
                        lockToken,
                        new DistributedCacheEntryOptions
                        {
                            AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(10)
                        },
                        cancellationToken);

                    // Đọc lại để xác nhận quyền sở hữu khóa (atomic idempotency check)
                    var verified = await _cache.GetStringAsync(lockName, cancellationToken);
                    if (verified == lockToken)
                    {
                        acquired = true;
                        break;
                    }
                }

                await Task.Delay(50, cancellationToken);
            }

            if (!acquired)
            {
                localSemaphore.Release();
                _logger.LogWarning("Redis Distributed Lock timeout cho tài nguyên {ResourceKey}", resourceKey);
                return null;
            }

            return new LockReleaser(_cache, lockName, lockToken, localSemaphore, _logger);
        }
        catch (Exception ex)
        {
            localSemaphore.Release();
            _logger.LogError(ex, "Lỗi xảy ra trong quá trình xin cấp khóa phân tán {ResourceKey}", resourceKey);
            return null;
        }
    }

    private sealed class LockReleaser : IAsyncDisposable
    {
        private readonly IDistributedCache _cache;
        private readonly string _lockName;
        private readonly string _lockToken;
        private readonly SemaphoreSlim _localSemaphore;
        private readonly ILogger _logger;
        private bool _disposed;

        public LockReleaser(
            IDistributedCache cache,
            string lockName,
            string lockToken,
            SemaphoreSlim localSemaphore,
            ILogger logger)
        {
            _cache = cache;
            _lockName = lockName;
            _lockToken = lockToken;
            _localSemaphore = localSemaphore;
            _logger = logger;
        }

        public async ValueTask DisposeAsync()
        {
            if (_disposed) return;
            _disposed = true;

            try
            {
                var current = await _cache.GetStringAsync(_lockName);
                if (current == _lockToken)
                {
                    await _cache.RemoveAsync(_lockName);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Không thể giải phóng distributed lock {LockName}", _lockName);
            }
            finally
            {
                _localSemaphore.Release();
            }
        }
    }
}

