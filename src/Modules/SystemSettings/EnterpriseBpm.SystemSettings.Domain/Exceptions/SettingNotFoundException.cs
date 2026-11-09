namespace EnterpriseBpm.SystemSettings.Domain.Exceptions;

public class SettingNotFoundException : Exception
{
    public string SettingKey { get; }

    public SettingNotFoundException(string settingKey)
        : base($"Không tìm thấy tham số cấu hình với khóa: '{settingKey}'.")
    {
        SettingKey = settingKey;
    }
}

