using EnterpriseBpm.SystemSettings.Domain.Entities;
using EnterpriseBpm.SystemSettings.Domain.Enums;
using EnterpriseBpm.SystemSettings.Infrastructure.Services;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace EnterpriseBpm.SharedKernel.UnitTests;

public class SystemSettingsTests
{
    [Fact]
    public void SystemSetting_Creation_ShouldSetDefaultProperties()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var setting = new SystemSetting(
            key: "Brand:AppName",
            value: "Enterprise BPM Platform",
            description: "Tên ứng dụng",
            dataType: SettingDataType.String,
            isEncrypted: false,
            userId: userId
        );

        // Assert
        Assert.Equal("Brand:AppName", setting.SettingKey);
        Assert.Equal("Enterprise BPM Platform", setting.SettingValue);
        Assert.False(setting.IsEncrypted);
        Assert.Equal(userId, setting.CreatedBy);
    }

    [Fact]
    public void SettingChangeHistory_ShouldRecordOldAndNewValueWithReason()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var history = new SettingChangeHistory(
            key: "Brand:PrimaryColor",
            oldValue: "#007bff",
            newValue: "#dc3545",
            userId: userId,
            userName: "SuperAdmin",
            reason: "Đổi màu thương hiệu dịp Tết"
        );

        // Assert
        Assert.Equal("Brand:PrimaryColor", history.SettingKey);
        Assert.Equal("#007bff", history.OldValue);
        Assert.Equal("#dc3545", history.NewValue);
        Assert.Equal("SuperAdmin", history.ChangedByUserName);
        Assert.Equal("Đổi màu thương hiệu dịp Tết", history.Reason);
        Assert.True(history.ChangedAt <= DateTime.UtcNow);
    }

    [Fact]
    public void DataProtectionEncryptionService_ShouldEncryptAndDecryptCorrectly()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddDataProtection();
        var sp = services.BuildServiceProvider();
        var provider = sp.GetRequiredService<IDataProtectionProvider>();
        var encryptionService = new DataProtectionEncryptionService(provider);

        var secretValue = "MySecretMailApiKey2026!";

        // Act
        var cipher = encryptionService.Encrypt(secretValue);
        var decrypted = encryptionService.Decrypt(cipher);

        // Assert
        Assert.NotEqual(secretValue, cipher);
        Assert.Equal(secretValue, decrypted);
    }
}

