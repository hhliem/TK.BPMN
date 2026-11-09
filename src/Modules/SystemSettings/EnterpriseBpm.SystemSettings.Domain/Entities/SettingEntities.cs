using EnterpriseBpm.SharedKernel;
using EnterpriseBpm.SharedKernel.Contracts;
using EnterpriseBpm.SystemSettings.Domain.Enums;

namespace EnterpriseBpm.SystemSettings.Domain.Entities;

public class SystemSetting : BaseEntity
{
    public string SettingKey { get; set; } = string.Empty;
    public string SettingValue { get; set; } = string.Empty;
    public string? Description { get; set; }
    public SettingDataType DataType { get; set; } = SettingDataType.String;
    public bool IsEncrypted { get; set; } = false;
    public Guid? CreatedBy { get; set; }
    public Guid? UpdatedBy { get; set; }
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public SystemSetting() { }

    public SystemSetting(string key, string value, string? description = null, SettingDataType dataType = SettingDataType.String, bool isEncrypted = false, Guid? userId = null)
    {
        SettingKey = key;
        SettingValue = value;
        Description = description;
        DataType = dataType;
        IsEncrypted = isEncrypted;
        CreatedBy = userId;
        UpdatedBy = userId;
        UpdatedAt = DateTime.UtcNow;
    }
}

public class SettingChangeHistory
{
    public long Id { get; set; }
    public string SettingKey { get; set; } = string.Empty;
    public string? OldValue { get; set; }
    public string NewValue { get; set; } = string.Empty;
    public Guid ChangedByUserId { get; set; }
    public string ChangedByUserName { get; set; } = string.Empty;
    public string? Reason { get; set; }
    public DateTime ChangedAt { get; set; } = DateTime.UtcNow;

    public SettingChangeHistory() { }

    public SettingChangeHistory(string key, string? oldValue, string newValue, Guid userId, string userName, string? reason)
    {
        SettingKey = key;
        OldValue = oldValue;
        NewValue = newValue;
        ChangedByUserId = userId;
        ChangedByUserName = userName;
        Reason = reason;
        ChangedAt = DateTime.UtcNow;
    }
}
