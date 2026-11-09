namespace EnterpriseBpm.SharedKernel.Caching;

/// <summary>
/// Giao diện dịch vụ Caching 2 tầng (Multi-Level Caching: L1 Memory + L2 Redis) (TSK-804)
/// Tối ưu hóa độ trễ đọc P99 < 30ms cho Master Data, Org Directory và Form Definitions.
/// </summary>
public interface IMultiLevelCacheService
{
    Task<T?> GetOrSetAsync<T>(
        string key,
        Func<Task<T>> factory,
        TimeSpan? l1Ttl = null,
        TimeSpan? l2Ttl = null,
        CancellationToken cancellationToken = default);

    Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default);

    Task SetAsync<T>(
        string key,
        T value,
        TimeSpan? l1Ttl = null,
        TimeSpan? l2Ttl = null,
        CancellationToken cancellationToken = default);

    Task RemoveAsync(string key, CancellationToken cancellationToken = default);

    Task RemoveByPrefixAsync(string prefix, CancellationToken cancellationToken = default);
}

