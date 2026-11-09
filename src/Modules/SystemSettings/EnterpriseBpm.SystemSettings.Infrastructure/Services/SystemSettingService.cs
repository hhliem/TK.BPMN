using EnterpriseBpm.SystemSettings.Application.Contracts;
using EnterpriseBpm.SystemSettings.Domain.Entities;
using EnterpriseBpm.SystemSettings.Domain.Enums;
using EnterpriseBpm.SystemSettings.Domain.Exceptions;
using EnterpriseBpm.SystemSettings.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace EnterpriseBpm.SystemSettings.Infrastructure.Services;

public class SystemSettingService : ISystemSettingService
{
    private readonly SettingsDbContext _dbContext;
    private readonly IMemoryCache _memoryCache;
    private readonly ISettingEncryptionService _encryptionService;
    private readonly ILogger<SystemSettingService> _logger;
    private const string CacheKeyPrefix = "sys_setting_";
    private static readonly TimeSpan CacheTtl = TimeSpan.FromHours(24);

    public SystemSettingService(
        SettingsDbContext dbContext,
        IMemoryCache memoryCache,
        ISettingEncryptionService encryptionService,
        ILogger<SystemSettingService> logger)
    {
        _dbContext = dbContext;
        _memoryCache = memoryCache;
        _encryptionService = encryptionService;
        _logger = logger;
    }

    public async Task<string> GetSettingValueAsync(string key, string defaultValue = "", CancellationToken cancellationToken = default)
    {
        var cacheKey = $"{CacheKeyPrefix}{key}";
        if (_memoryCache.TryGetValue(cacheKey, out string? cached) && cached != null)
        {
            return cached;
        }

        var setting = await _dbContext.SystemSettings
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.SettingKey == key, cancellationToken);

        if (setting == null)
        {
            return defaultValue;
        }

        var val = setting.IsEncrypted ? _encryptionService.Decrypt(setting.SettingValue) : setting.SettingValue;
        _memoryCache.Set(cacheKey, val, CacheTtl);
        return val;
    }

    public async Task<IReadOnlyList<SystemSettingDto>> GetAllSettingsAsync(CancellationToken cancellationToken = default)
    {
        var settings = await _dbContext.SystemSettings
            .AsNoTracking()
            .OrderBy(s => s.SettingKey)
            .ToListAsync(cancellationToken);

        return settings.Select(s => new SystemSettingDto(
            s.Id,
            s.SettingKey,
            s.IsEncrypted ? "********" : s.SettingValue,
            s.Description,
            s.DataType.ToString(),
            s.IsEncrypted,
            s.UpdatedAt
        )).ToList();
    }

    public async Task<SystemSettingDto> UpdateSettingAsync(
        string key,
        string value,
        string? reason,
        Guid userId,
        string userName,
        CancellationToken cancellationToken = default)
    {
        var setting = await _dbContext.SystemSettings
            .FirstOrDefaultAsync(s => s.SettingKey == key, cancellationToken);

        string? oldValue = null;
        var storedValue = value;

        if (setting == null)
        {
            setting = new SystemSetting(key, storedValue, userId: userId);
            _dbContext.SystemSettings.Add(setting);
        }
        else
        {
            oldValue = setting.IsEncrypted ? "********" : setting.SettingValue;
            if (setting.IsEncrypted)
            {
                storedValue = _encryptionService.Encrypt(value);
            }
            setting.SettingValue = storedValue;
            setting.UpdatedBy = userId;
            setting.UpdatedAt = DateTime.UtcNow;
        }

        // Ghi nhận Audit Trail
        var history = new SettingChangeHistory(
            key: key,
            oldValue: oldValue,
            newValue: setting.IsEncrypted ? "********" : value,
            userId: userId,
            userName: userName,
            reason: reason ?? "Cập nhật tham số hệ thống"
        );
        _dbContext.SettingChangeHistories.Add(history);

        await _dbContext.SaveChangesAsync(cancellationToken);

        // Xóa L1 Cache ngay lập tức
        _memoryCache.Remove($"{CacheKeyPrefix}{key}");
        _logger.LogInformation("Đã cập nhật tham số cấu hình '{Key}' bởi {User}. Cache đã được xóa.", key, userName);

        return new SystemSettingDto(
            setting.Id,
            setting.SettingKey,
            setting.IsEncrypted ? "********" : setting.SettingValue,
            setting.Description,
            setting.DataType.ToString(),
            setting.IsEncrypted,
            setting.UpdatedAt
        );
    }

    public async Task<IReadOnlyList<SettingChangeHistoryDto>> GetChangeHistoryAsync(string key, CancellationToken cancellationToken = default)
    {
        var histories = await _dbContext.SettingChangeHistories
            .AsNoTracking()
            .Where(h => h.SettingKey == key)
            .OrderByDescending(h => h.ChangedAt)
            .Take(50)
            .ToListAsync(cancellationToken);

        return histories.Select(h => new SettingChangeHistoryDto(
            h.Id,
            h.SettingKey,
            h.OldValue,
            h.NewValue,
            h.ChangedByUserId,
            h.ChangedByUserName,
            h.Reason,
            h.ChangedAt
        )).ToList();
    }

    public async Task<BrandThemeDto> GetBrandThemeAsync(CancellationToken cancellationToken = default)
    {
        var appName = await GetSettingValueAsync("Brand:AppName", "Enterprise BPM Platform", cancellationToken);
        var corpName = await GetSettingValueAsync("Brand:CorporationName", "Tập đoàn Công nghệ & Sản xuất Quốc gia", cancellationToken);
        var logoUrl = await GetSettingValueAsync("Brand:LogoUrl", "https://adminlte.io/themes/v3/dist/img/AdminLTELogo.png", cancellationToken);
        var faviconUrl = await GetSettingValueAsync("Brand:FaviconUrl", "/favicon.ico", cancellationToken);
        var primaryColor = await GetSettingValueAsync("Brand:PrimaryColor", "#007bff", cancellationToken);
        var accentColor = await GetSettingValueAsync("Brand:AccentColor", "#17a2b8", cancellationToken);

        return new BrandThemeDto(appName, corpName, logoUrl, faviconUrl, primaryColor, accentColor);
    }

    public async Task<BrandThemeDto> UpdateBrandThemeAsync(
        BrandThemeDto dto,
        string? reason,
        Guid userId,
        string userName,
        CancellationToken cancellationToken = default)
    {
        await UpdateSettingAsync("Brand:AppName", dto.AppName, reason, userId, userName, cancellationToken);
        await UpdateSettingAsync("Brand:CorporationName", dto.CorporationName, reason, userId, userName, cancellationToken);
        await UpdateSettingAsync("Brand:LogoUrl", dto.LogoUrl, reason, userId, userName, cancellationToken);
        await UpdateSettingAsync("Brand:FaviconUrl", dto.FaviconUrl, reason, userId, userName, cancellationToken);
        await UpdateSettingAsync("Brand:PrimaryColor", dto.PrimaryColor, reason, userId, userName, cancellationToken);
        await UpdateSettingAsync("Brand:AccentColor", dto.AccentColor, reason, userId, userName, cancellationToken);

        return dto;
    }
}

