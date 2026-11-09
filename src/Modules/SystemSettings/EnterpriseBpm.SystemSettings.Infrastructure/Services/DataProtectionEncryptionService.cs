using EnterpriseBpm.SystemSettings.Application.Contracts;
using Microsoft.AspNetCore.DataProtection;

namespace EnterpriseBpm.SystemSettings.Infrastructure.Services;

public class DataProtectionEncryptionService : ISettingEncryptionService
{
    private readonly IDataProtector _protector;

    public DataProtectionEncryptionService(IDataProtectionProvider provider)
    {
        _protector = provider.CreateProtector("EnterpriseBpm.SystemSettings.SecurityProtector.v1");
    }

    public string Encrypt(string plainText)
    {
        if (string.IsNullOrEmpty(plainText)) return plainText;
        return _protector.Protect(plainText);
    }

    public string Decrypt(string cipherText)
    {
        if (string.IsNullOrEmpty(cipherText)) return cipherText;
        try
        {
            return _protector.Unprotect(cipherText);
        }
        catch
        {
            return "[Encrypted Value - Cannot Decrypt]";
        }
    }
}

