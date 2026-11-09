using System.Security.Cryptography;
using EnterpriseBpm.Document.Application.Contracts;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace EnterpriseBpm.Document.Infrastructure.Services;

public class ExternalStorageClient : IExternalStorageClient
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<ExternalStorageClient> _logger;
    private readonly string _storageRoot;

    public ExternalStorageClient(
        IConfiguration configuration,
        ILogger<ExternalStorageClient> logger)
    {
        _configuration = configuration;
        _logger = logger;
        
        // Cấu hình thư mục lưu trữ nội bộ cho môi trường dev/local hoặc fallback
        var localDir = _configuration["OneDrive:LocalMockPath"] ?? "App_Data/OneDriveStorage";
        _storageRoot = Path.IsPathRooted(localDir) 
            ? localDir 
            : Path.Combine(Directory.GetCurrentDirectory(), localDir);

        if (!Directory.Exists(_storageRoot))
        {
            Directory.CreateDirectory(_storageRoot);
        }
    }

    public async Task<StorageUploadResult> UploadStreamAsync(
        string relativePath,
        Stream stream,
        string contentType,
        CancellationToken cancellationToken = default)
    {
        var sanitizedRelative = relativePath.TrimStart('/', '\\').Replace('/', Path.DirectorySeparatorChar);
        var fullPath = Path.Combine(_storageRoot, sanitizedRelative);
        var dir = Path.GetDirectoryName(fullPath);
        if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
        {
            Directory.CreateDirectory(dir);
        }

        await using var fileStream = new FileStream(fullPath, FileMode.Create, FileAccess.Write, FileShare.None, 4096, true);
        await stream.CopyToAsync(fileStream, cancellationToken);

        var itemId = $"drv_{Guid.NewGuid():N}";
        var webUrl = $"/api/documents/preview-file/{itemId}";

        _logger.LogInformation("Đã tải tệp lên External Storage thành công tại: {Path}, ItemId: {ItemId}", sanitizedRelative, itemId);
        return new StorageUploadResult(itemId, webUrl);
    }

    public Task<string> CreateSharingLinkAsync(
        string itemId,
        TimeSpan expiration,
        CancellationToken cancellationToken = default)
    {
        // Sinh Signed Preview Token có thời hạn hết hạn (VD: 15 phút)
        var expiresUnix = DateTimeOffset.UtcNow.Add(expiration).ToUnixTimeSeconds();
        var rawPayload = $"{itemId}:{expiresUnix}";
        var signature = Convert.ToHexString(SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(rawPayload))).ToLowerInvariant();

        var secureUrl = $"/api/documents/secure-preview?item={itemId}&exp={expiresUnix}&sig={signature[..16]}";
        return Task.FromResult(secureUrl);
    }
}

