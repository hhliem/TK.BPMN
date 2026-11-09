using System.Text.Json;
using EnterpriseBpm.AccessGovernance.Application.Contracts;
using EnterpriseBpm.AccessGovernance.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace EnterpriseBpm.AccessGovernance.Infrastructure.Cache;

public class CachedPermissionEvaluator : IPermissionEvaluator
{
    private readonly AuthDbContext _dbContext;
    private readonly IMemoryCache _memoryCache;
    private readonly IDistributedCache _distributedCache;
    private readonly ILogger<CachedPermissionEvaluator> _logger;

    private static readonly TimeSpan L1Ttl = TimeSpan.FromMinutes(5);
    private static readonly TimeSpan L2Ttl = TimeSpan.FromMinutes(30);

    public CachedPermissionEvaluator(
        AuthDbContext dbContext,
        IMemoryCache memoryCache,
        IDistributedCache distributedCache,
        ILogger<CachedPermissionEvaluator> logger)
    {
        _dbContext = dbContext;
        _memoryCache = memoryCache;
        _distributedCache = distributedCache;
        _logger = logger;
    }

    private static string GetCacheKey(Guid userId) => $"auth:perms:{userId}";

    public async Task<bool> HasPermissionAsync(Guid userId, string permissionCode, CancellationToken cancellationToken = default)
    {
        var permissions = await GetUserPermissionsAsync(userId, cancellationToken);
        return permissions.Contains(permissionCode);
    }

    public async Task<HashSet<string>> GetUserPermissionsAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var cacheKey = GetCacheKey(userId);

        // Tier 1: Local Memory Cache (< 0.1ms)
        if (_memoryCache.TryGetValue(cacheKey, out HashSet<string>? cachedPerms) && cachedPerms != null)
        {
            return cachedPerms;
        }

        // Tier 2: Distributed Redis Cache (< 2ms)
        try
        {
            var redisData = await _distributedCache.GetStringAsync(cacheKey, cancellationToken);
            if (!string.IsNullOrEmpty(redisData))
            {
                var permsFromRedis = JsonSerializer.Deserialize<HashSet<string>>(redisData);
                if (permsFromRedis != null)
                {
                    _memoryCache.Set(cacheKey, permsFromRedis, L1Ttl);
                    return permsFromRedis;
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to read permission cache from Redis for user {UserId}. Falling back to DB.", userId);
        }

        // Tier 3: SQL Server Database Query
        var permissions = await LoadUserPermissionsFromDbAsync(userId, cancellationToken);

        // Update Tier 1 MemoryCache
        _memoryCache.Set(cacheKey, permissions, L1Ttl);

        // Update Tier 2 Redis Cache
        try
        {
            var options = new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = L2Ttl
            };
            var serialized = JsonSerializer.Serialize(permissions);
            await _distributedCache.SetStringAsync(cacheKey, serialized, options, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to write permission cache to Redis for user {UserId}", userId);
        }

        return permissions;
    }

    private async Task<HashSet<string>> LoadUserPermissionsFromDbAsync(Guid userId, CancellationToken cancellationToken)
    {
        // 1. Lấy quyền từ Vai trò gán trực tiếp (Direct UserRoles)
        var directRolePermissions = await _dbContext.UserRoles
            .AsNoTracking()
            .Where(ur => ur.UserId == userId && ur.Role.IsActive)
            .SelectMany(ur => ur.Role.RolePermissions.Select(rp => rp.Permission.PermissionCode))
            .ToListAsync(cancellationToken);

        // 2. Lấy quyền từ Nhóm Người dùng (Security Groups)
        var groupRolePermissions = await _dbContext.SecurityGroupMembers
            .AsNoTracking()
            .Where(sgm => sgm.UserId == userId && sgm.Group.IsActive)
            .SelectMany(sgm => sgm.Group.SecurityGroupRoles
                .Where(sgr => sgr.Role.IsActive)
                .SelectMany(sgr => sgr.Role.RolePermissions.Select(rp => rp.Permission.PermissionCode)))
            .ToListAsync(cancellationToken);

        var allPermissions = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var p in directRolePermissions)
        {
            allPermissions.Add(p);
        }
        foreach (var p in groupRolePermissions)
        {
            allPermissions.Add(p);
        }

        return allPermissions;
    }

    public async Task InvalidateUserCacheAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var cacheKey = GetCacheKey(userId);

        // Xóa L1
        _memoryCache.Remove(cacheKey);

        // Xóa L2
        try
        {
            await _distributedCache.RemoveAsync(cacheKey, cancellationToken);
            _logger.LogInformation("Invalidated permission cache for user {UserId}", userId);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to remove cache key {CacheKey} from Redis", cacheKey);
        }
    }
}

