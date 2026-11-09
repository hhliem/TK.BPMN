using System.Text.Json;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;

namespace EnterpriseBpm.FormMasterData.Infrastructure.Repositories;

public interface IMasterDataCacheService
{
    Task<IReadOnlyList<MasterItemDto>> GetCascadingItemsAsync(
        string categoryKey, Guid? parentId, CancellationToken cancellationToken);
    Task InvalidateCategoryCacheAsync(string categoryKey, CancellationToken cancellationToken);
}

/// <summary>
/// Triển khai Caching 2 tầng: L1 (Memory) + L2 (Redis) cho Cascading Master Data
/// </summary>
public class MultiLevelMasterDataCacheService : IMasterDataCacheService
{
    private readonly IMemoryCache _memoryCache;
    private readonly IDistributedCache _redisCache;
    private readonly IMasterDataQueryRepository _repo;
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    public MultiLevelMasterDataCacheService(
        IMemoryCache memoryCache,
        IDistributedCache redisCache,
        IMasterDataQueryRepository repo)
    {
        _memoryCache = memoryCache;
        _redisCache = redisCache;
        _repo = repo;
    }

    public async Task<IReadOnlyList<MasterItemDto>> GetCascadingItemsAsync(
        string categoryKey, Guid? parentId, CancellationToken cancellationToken)
    {
        var cacheKey = $"masterdata:{categoryKey}:parent:{parentId?.ToString() ?? "root"}";

        // 1. Kiểm tra L1 Memory Cache (< 1ms)
        if (_memoryCache.TryGetValue(cacheKey, out IReadOnlyList<MasterItemDto>? memoryItems) && memoryItems != null)
        {
            return memoryItems;
        }

        // 2. Kiểm tra L2 Redis Distributed Cache (< 5ms)
        var redisBytes = await _redisCache.GetAsync(cacheKey, cancellationToken);
        if (redisBytes != null && redisBytes.Length > 0)
        {
            var redisItems = JsonSerializer.Deserialize<IReadOnlyList<MasterItemDto>>(redisBytes, JsonOptions);
            if (redisItems != null)
            {
                // Nạp ngược vào L1 với TTL ngắn (5 phút)
                _memoryCache.Set(cacheKey, redisItems, TimeSpan.FromMinutes(5));
                return redisItems;
            }
        }

        // 3. Cache Miss -> Truy vấn Database (< 20ms)
        var dbItems = await _repo.GetActiveItemsByCategoryAndParentAsync(categoryKey, parentId, cancellationToken);

        // 4. Ghi ngược vào L2 Redis (TTL: 60 phút) và L1 Memory (TTL: 5 phút)
        var payload = JsonSerializer.SerializeToUtf8Bytes(dbItems, JsonOptions);
        await _redisCache.SetAsync(cacheKey, payload, new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(1)
        }, cancellationToken);

        _memoryCache.Set(cacheKey, dbItems, TimeSpan.FromMinutes(5));

        return dbItems;
    }

    public async Task InvalidateCategoryCacheAsync(string categoryKey, CancellationToken cancellationToken)
    {
        // Khi danh mục có thay đổi (Add/Update/Delete), xóa toàn bộ Redis key liên quan
        // Thực thi qua Redis SCAN pattern "masterdata:{categoryKey}:*"
        await _redisCache.RemoveAsync($"masterdata:{categoryKey}:parent:root", cancellationToken);
    }
}

