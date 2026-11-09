using EnterpriseBpm.AiAssistant.Domain.Enums;
using EnterpriseBpm.SharedKernel;
using EnterpriseBpm.SharedKernel.Contracts;

namespace EnterpriseBpm.AiAssistant.Domain.Entities;

public class AiProviderConfiguration : BaseEntity<Guid>, IAuditable
{
    public AiProviderType ProviderType { get; private set; }
    public string DisplayName { get; private set; } = default!;
    public string ModelName { get; private set; } = default!;
    public string? ApiEndpoint { get; private set; }
    public string? EncryptedApiKey { get; private set; }
    public double Temperature { get; private set; } = 0.1;
    public int MaxTokens { get; private set; } = 2048;
    public bool IsDefault { get; private set; } = false;
    public bool IsActive { get; private set; } = true;

    // IAuditable
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public Guid? CreatedBy { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public Guid? UpdatedBy { get; set; }

    protected AiProviderConfiguration() { }

    public static AiProviderConfiguration Create(
        AiProviderType providerType,
        string displayName,
        string modelName,
        string? apiEndpoint = null,
        string? encryptedApiKey = null,
        double temperature = 0.1,
        int maxTokens = 2048,
        bool isDefault = false,
        Guid? createdBy = null)
    {
        return new AiProviderConfiguration
        {
            Id = Guid.NewGuid(),
            ProviderType = providerType,
            DisplayName = displayName?.Trim() ?? providerType.ToString(),
            ModelName = modelName?.Trim() ?? "default",
            ApiEndpoint = apiEndpoint?.Trim(),
            EncryptedApiKey = encryptedApiKey,
            Temperature = Math.Clamp(temperature, 0.0, 1.0),
            MaxTokens = maxTokens <= 0 ? 2048 : maxTokens,
            IsDefault = isDefault,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = createdBy
        };
    }

    public void Update(
        string displayName,
        string modelName,
        string? apiEndpoint,
        string? encryptedApiKey,
        double temperature,
        int maxTokens,
        Guid? updatedBy = null)
    {
        DisplayName = displayName.Trim();
        ModelName = modelName.Trim();
        ApiEndpoint = apiEndpoint?.Trim();
        if (!string.IsNullOrWhiteSpace(encryptedApiKey))
        {
            EncryptedApiKey = encryptedApiKey;
        }
        Temperature = Math.Clamp(temperature, 0.0, 1.0);
        MaxTokens = maxTokens <= 0 ? 2048 : maxTokens;
        UpdatedAt = DateTime.UtcNow;
        UpdatedBy = updatedBy;
    }

    public void SetDefault(bool isDefault)
    {
        IsDefault = isDefault;
        UpdatedAt = DateTime.UtcNow;
    }

    public void SetActive(bool isActive)
    {
        IsActive = isActive;
        UpdatedAt = DateTime.UtcNow;
    }
}

