namespace EnterpriseBpm.SystemSettings.Application.Contracts;

public record SystemSettingDto(
    Guid Id,
    string SettingKey,
    string SettingValue,
    string? Description,
    string DataType,
    bool IsEncrypted,
    DateTime UpdatedAt
);

public record SettingChangeHistoryDto(
    long Id,
    string SettingKey,
    string? OldValue,
    string NewValue,
    Guid ChangedByUserId,
    string ChangedByUserName,
    string? Reason,
    DateTime ChangedAt
);

public record BrandThemeDto(
    string AppName,
    string CorporationName,
    string LogoUrl,
    string FaviconUrl,
    string PrimaryColor,
    string AccentColor
);

public record UpdateSettingRequest(
    string Value,
    string? Reason
);

public interface ISettingEncryptionService
{
    string Encrypt(string plainText);
    string Decrypt(string cipherText);
}

public interface ISystemSettingService
{
    Task<string> GetSettingValueAsync(string key, string defaultValue = "", CancellationToken cancellationToken = default);
    Task<IReadOnlyList<SystemSettingDto>> GetAllSettingsAsync(CancellationToken cancellationToken = default);
    Task<SystemSettingDto> UpdateSettingAsync(string key, string value, string? reason, Guid userId, string userName, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<SettingChangeHistoryDto>> GetChangeHistoryAsync(string key, CancellationToken cancellationToken = default);
    Task<BrandThemeDto> GetBrandThemeAsync(CancellationToken cancellationToken = default);
    Task<BrandThemeDto> UpdateBrandThemeAsync(BrandThemeDto dto, string? reason, Guid userId, string userName, CancellationToken cancellationToken = default);
}

