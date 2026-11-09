# Module 07: SystemSettings (Cấu hình Vận hành & White-labeling)

## 1. Tổng quan & Phạm vi Bounded Context
Module `SystemSettings` cho phép hệ thống triển khai linh hoạt cho nhiều doanh nghiệp hoặc tùy biến thương hiệu tập đoàn (White-labeling):
1. Quản lý cấu hình Tên ứng dụng, Tên Tập đoàn, Logo URL, Favicon, Gam màu chủ đạo (Brand Theme).
2. Quản lý các thông số kỹ thuật (Giới hạn dung lượng upload, thời gian hết hạn phiên đăng nhập, cấu hình bảo mật).
3. Hỗ trợ In-Memory Cache và tự động làm mới khi có thay đổi.

### Ranh giới kiến trúc
- **Database Schema riêng biệt:** `[settings]` trên SQL Server.
- **Tần suất truy cập:** Cực cao (mỗi request giao diện đều cần đọc Logo, Tên App).
- **Cơ chế:** Sử dụng `IMemoryCache` kết hợp cơ chế phát tín hiệu làm mới (Cache Eviction Signal).

---

## 2. Thiết kế Cơ sở dữ liệu (SQL Server - Schema [settings])

```sql
CREATE SCHEMA [settings];
GO

CREATE TABLE [settings].[SystemSettings] (
    [Id] UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    [SettingKey] NVARCHAR(100) NOT NULL UNIQUE, -- "Brand:AppName", "Brand:LogoUrl", "Security:MaxUploadSizeMb"
    [SettingValue] NVARCHAR(MAX) NOT NULL,
    [Description] NVARCHAR(500) NULL,
    [DataType] NVARCHAR(50) NOT NULL DEFAULT 'String', -- String, Number, Boolean, Json
    [IsEncrypted] BIT NOT NULL DEFAULT 0, -- Dành cho các cấu hình nhạy cảm (API Keys)
    [CreatedBy] UNIQUEIDENTIFIER NULL,
    [UpdatedBy] UNIQUEIDENTIFIER NULL,
    [UpdatedAt] DATETIME2 NOT NULL DEFAULT GETUTCDATE()
);
GO

-- Bảng Lịch sử Thay đổi Cấu hình
CREATE TABLE [settings].[SettingChangeHistory] (
    [Id] BIGINT IDENTITY(1,1) PRIMARY KEY,
    [SettingKey] NVARCHAR(100) NOT NULL,
    [OldValue] NVARCHAR(MAX) NULL,
    [NewValue] NVARCHAR(MAX) NOT NULL,
    [ChangedByUserId] UNIQUEIDENTIFIER NOT NULL,
    [ChangedByUserName] NVARCHAR(255) NOT NULL,
    [Reason] NVARCHAR(500) NULL,
    [ChangedAt] DATETIME2 NOT NULL DEFAULT GETUTCDATE()
);
CREATE INDEX [IX_SettingChangeHistory_Key]
ON [settings].[SettingChangeHistory]([SettingKey], [ChangedAt] DESC);
GO
```

---

## 3. Quản lý Bộ nhớ đệm Tối ưu (High-Performance Caching)

```csharp
public class SystemSettingService : ISystemSettingService
{
    private readonly IMemoryCache _cache;
    private readonly ISettingsRepository _repo;
    private readonly ISettingChangeHistoryRepository _historyRepo;
    private readonly ICurrentUserService _currentUser;
    private const string CacheKeyPrefix = "sys_setting_";

    public SystemSettingService(
        IMemoryCache cache,
        ISettingsRepository repo,
        ISettingChangeHistoryRepository historyRepo,
        ICurrentUserService currentUser)
    {
        _cache = cache;
        _repo = repo;
        _historyRepo = historyRepo;
        _currentUser = currentUser;
    }

    public async Task<string> GetSettingAsync(string key, string defaultValue = "")
    {
        var cacheKey = $"{CacheKeyPrefix}{key}";
        if (_cache.TryGetValue(cacheKey, out string? cachedValue) && cachedValue != null)
        {
            return cachedValue;
        }

        var setting = await _repo.GetByKeyAsync(key);
        var value = setting?.SettingValue ?? defaultValue;

        _cache.Set(cacheKey, value, TimeSpan.FromHours(24));
        return value;
    }

    public async Task UpdateSettingAsync(string key, string value, string? reason, CancellationToken cancellationToken)
    {
        var oldSetting = await _repo.GetByKeyAsync(key);
        var oldValue = oldSetting?.SettingValue;
        
        await _repo.UpdateValueAsync(key, value, cancellationToken);
        _cache.Remove($"{CacheKeyPrefix}{key}"); // Xóa cache ngay lập tức
        
        // Ghi nhận lịch sử thay đổi
        await _historyRepo.AddAsync(new SettingChangeHistory
        {
            SettingKey = key,
            OldValue = oldValue,
            NewValue = value,
            ChangedByUserId = _currentUser.UserId,
            ChangedByUserName = _currentUser.UserName,
            Reason = reason
        }, cancellationToken);
    }
}
```

---

## 4. Điểm kiểm soát trọng yếu (Critical Control Points)

1. **Mã hóa Cấu hình Nhạy cảm (At-Rest Encryption):**
   - **Rủi ro:** Các tham số như Client Secret của Office 365 hoặc API Key gửi SMS bị lộ khi truy cập trực tiếp vào Database.
   - **Kiểm soát:** Nếu cột `IsEncrypted = 1`, hệ thống bắt buộc sử dụng `IDataProtectionProvider` của ASP.NET Core để mã hóa giá trị trước khi `INSERT`/`UPDATE` vào SQL Server.
2. **Phân quyền Thay đổi Cấu hình (Configuration Lockdown):**
   - **Rủi ro:** Nhân viên hoặc Quản lý cấp phòng vô tình thay đổi thông số hệ thống gây sập toàn bộ dịch vụ.
   - **Kiểm soát:** Chỉ duy nhất Role `SuperAdmin` (Quản trị viên tối cao) mới có quyền gọi API chỉnh sửa cấu hình hệ thống. Mọi thao tác cập nhật đều phải ghi nhận log vào bảng kiểm toán.
3. **Setting Change Audit Trail:**
   - **Rủi ro:** Không thể truy vết ai đã thay đổi cấu hình hệ thống, thay đổi lúc nào, và giá trị trước đó là gì — gây khó khăn khi cần rollback hoặc điều tra sự cố.
   - **Kiểm soát:** Mọi thay đổi cấu hình phải được ghi nhận vào bảng `SettingChangeHistory` bao gồm giá trị cũ, giá trị mới, người thay đổi và lý do. Hỗ trợ rollback cấu hình khi có sự cố.
