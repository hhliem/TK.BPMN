using System.Text.Json;
using EnterpriseBpm.Form.Application.Contracts;
using EnterpriseBpm.Form.Application.DTOs;
using EnterpriseBpm.Form.Domain.Entities;
using EnterpriseBpm.Form.Domain.Exceptions;
using EnterpriseBpm.Form.Domain.Services;
using EnterpriseBpm.Form.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace EnterpriseBpm.Form.Infrastructure.Services;

public class MasterDataService : IMasterDataService
{
    private readonly FormDbContext _dbContext;
    private readonly IMemoryCache _memoryCache;
    private readonly IDistributedCache _distributedCache;
    private readonly ILogger<MasterDataService> _logger;

    private static readonly TimeSpan L1Ttl = TimeSpan.FromMinutes(5);
    private static readonly TimeSpan L2Ttl = TimeSpan.FromMinutes(30);

    public MasterDataService(
        FormDbContext dbContext,
        IMemoryCache memoryCache,
        IDistributedCache distributedCache,
        ILogger<MasterDataService> logger)
    {
        _dbContext = dbContext;
        _memoryCache = memoryCache;
        _distributedCache = distributedCache;
        _logger = logger;
    }

    private static string GetCacheKey(string categoryKey, Guid? parentId) => 
        $"masterdata:{categoryKey.ToLowerInvariant()}:parent:{parentId?.ToString() ?? "root"}";

    public async Task<IReadOnlyList<MasterItemDto>> GetItemsAsync(
        string categoryKey, 
        Guid? parentId, 
        CancellationToken cancellationToken = default)
    {
        var cacheKey = GetCacheKey(categoryKey, parentId);

        // 1. Tier 1: Local Memory Cache (< 0.1ms)
        if (_memoryCache.TryGetValue(cacheKey, out List<MasterItemDto>? memoryItems) && memoryItems != null)
        {
            return memoryItems;
        }

        // 2. Tier 2: Distributed Redis Cache (< 2ms)
        try
        {
            var redisData = await _distributedCache.GetStringAsync(cacheKey, cancellationToken);
            if (!string.IsNullOrEmpty(redisData))
            {
                var redisItems = JsonSerializer.Deserialize<List<MasterItemDto>>(redisData);
                if (redisItems != null)
                {
                    _memoryCache.Set(cacheKey, redisItems, L1Ttl);
                    return redisItems;
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Lỗi đọc Redis cache cho MasterData key {CacheKey}", cacheKey);
        }

        // 3. Tier 3: SQL Server Query with Covering Index (< 10ms)
        var items = await _dbContext.MasterItems
            .AsNoTracking()
            .Where(i => i.Category.CategoryKey == categoryKey && i.ParentId == parentId && i.IsActive)
            .OrderBy(i => i.DisplayOrder)
            .ThenBy(i => i.Name)
            .Select(i => new MasterItemDto(
                i.Id,
                i.CategoryId,
                i.ParentId,
                i.Code,
                i.Name,
                i.DisplayOrder,
                i.CustomAttributesJson))
            .ToListAsync(cancellationToken);

        // Nạp L1 MemoryCache
        _memoryCache.Set(cacheKey, items, L1Ttl);

        // Nạp L2 Redis Cache
        try
        {
            var options = new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = L2Ttl };
            var serialized = JsonSerializer.Serialize(items);
            await _distributedCache.SetStringAsync(cacheKey, serialized, options, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Lỗi ghi Redis cache cho MasterData key {CacheKey}", cacheKey);
        }

        return items;
    }

    public async Task<bool> ValidateItemBelongsToParentAsync(Guid childId, Guid parentId, CancellationToken cancellationToken = default)
    {
        var child = await _dbContext.MasterItems
            .AsNoTracking()
            .Where(i => i.Id == childId && i.IsActive)
            .Select(i => new { i.Id, i.ParentId })
            .FirstOrDefaultAsync(cancellationToken);

        if (child == null)
        {
            throw new KeyNotFoundException($"Không tìm thấy mục danh mục phụ thuộc: {childId}");
        }

        // Anti-Tampering Check
        AntiTamperingItemValidator.Validate(childId, parentId, _ => child.ParentId);
        return true;
    }

    public async Task<Guid> CreateItemAsync(CreateMasterItemCommand command, CancellationToken cancellationToken = default)
    {
        var category = await _dbContext.MasterCategories
            .FirstOrDefaultAsync(c => c.CategoryKey == command.CategoryKey && c.IsActive, cancellationToken)
            ?? throw new KeyNotFoundException($"Không tìm thấy nhóm danh mục: {command.CategoryKey}");

        var item = new MasterItem
        {
            Id = Guid.NewGuid(),
            CategoryId = category.Id,
            ParentId = command.ParentId,
            Code = command.Code,
            Name = command.Name,
            DisplayOrder = command.DisplayOrder,
            CustomAttributesJson = command.CustomAttributesJson,
            IsActive = true
        };

        _dbContext.MasterItems.Add(item);
        await _dbContext.SaveChangesAsync(cancellationToken);

        // Xóa cache của danh mục này
        var cacheKey = GetCacheKey(command.CategoryKey, command.ParentId);
        _memoryCache.Remove(cacheKey);
        try
        {
            await _distributedCache.RemoveAsync(cacheKey, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Không thể xóa Redis cache cho key {CacheKey}", cacheKey);
        }

        return item.Id;
    }
}

