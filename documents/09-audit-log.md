# Module 09: AuditLog (Kiểm toán & Truy vết Hành động Hệ thống)

## 1. Tổng quan & Phạm vi Bounded Context
Module `AuditLog` chịu trách nhiệm ghi nhận tự động mọi thao tác quan trọng trên toàn hệ thống, phục vụ:
1. **Truy vết Hành động (Action Auditing):** Ghi nhận tất cả thao tác CUD (Create/Update/Delete) trên mọi Entity quan trọng.
2. **Giám sát Bảo mật (Security Monitoring):** Ghi nhận đăng nhập/đăng xuất, thay đổi quyền, phân quyền, ủy quyền.
3. **Tuân thủ Kiểm toán (Compliance):** Cung cấp API tra cứu lịch sử thay đổi phục vụ kiểm toán nội bộ/bên ngoài.
4. **Export & Báo cáo:** Hỗ trợ xuất dữ liệu kiểm toán theo định dạng Excel/CSV cho đoàn kiểm toán.

### Ranh giới kiến trúc
- **Database Schema riêng biệt:** `[audit]` trên SQL Server.
- **Cơ chế:** Event-Driven — lắng nghe `AuditableActionEvent` từ tất cả module qua MediatR + tự động ghi nhận qua EF Core `SaveChangesInterceptor`.
- **Nguyên tắc bất biến:** Dữ liệu kiểm toán sau khi ghi nhận TUYỆT ĐỐI KHÔNG được sửa hoặc xóa (Immutable Append-Only Log).
- **Không phụ thuộc ngược:** Module AuditLog không gọi ngược vào bất kỳ module nào.

---

## 2. Thiết kế Cơ sở dữ liệu (SQL Server - Schema [audit])

```sql
CREATE SCHEMA [audit];
GO

-- 1. Bảng Nhật ký Kiểm toán Chính
CREATE TABLE [audit].[AuditEntries] (
    [Id] BIGINT IDENTITY(1,1) PRIMARY KEY,
    [Timestamp] DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    [UserId] UNIQUEIDENTIFIER NOT NULL,
    [UserName] NVARCHAR(255) NOT NULL,
    [Action] NVARCHAR(50) NOT NULL, -- Create, Update, Delete, Login, Logout, Approve, Reject, Delegate, Publish
    [EntityType] NVARCHAR(200) NOT NULL,
    [EntityId] NVARCHAR(100) NOT NULL,
    [ModuleName] NVARCHAR(50) NOT NULL,
    [OldValues] NVARCHAR(MAX) NULL,
    [NewValues] NVARCHAR(MAX) NULL,
    [IpAddress] NVARCHAR(50) NULL,
    [UserAgent] NVARCHAR(500) NULL,
    [AdditionalData] NVARCHAR(MAX) NULL
);

-- Index tối ưu truy vấn theo Entity
CREATE INDEX [IX_AuditEntries_EntityLookup]
ON [audit].[AuditEntries]([EntityType], [EntityId], [Timestamp] DESC);

-- Index tối ưu truy vấn theo User
CREATE INDEX [IX_AuditEntries_UserTimeline]
ON [audit].[AuditEntries]([UserId], [Timestamp] DESC);

-- Index tối ưu truy vấn theo Module
CREATE INDEX [IX_AuditEntries_ModuleLookup]
ON [audit].[AuditEntries]([ModuleName], [Timestamp] DESC);

-- Index tối ưu truy vấn theo Action type
CREATE INDEX [IX_AuditEntries_ActionLookup]
ON [audit].[AuditEntries]([Action], [Timestamp] DESC);
GO

-- 2. Bảng Nhật ký Đăng nhập / Bảo mật
CREATE TABLE [audit].[SecurityEvents] (
    [Id] BIGINT IDENTITY(1,1) PRIMARY KEY,
    [Timestamp] DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    [UserId] UNIQUEIDENTIFIER NULL,
    [UserName] NVARCHAR(255) NULL,
    [EventType] NVARCHAR(50) NOT NULL, -- Login, Logout, LoginFailed, PasswordChanged, RoleChanged, DelegationCreated
    [IpAddress] NVARCHAR(50) NULL,
    [UserAgent] NVARCHAR(500) NULL,
    [IsSuccess] BIT NOT NULL DEFAULT 1,
    [FailureReason] NVARCHAR(500) NULL,
    [AdditionalData] NVARCHAR(MAX) NULL
);
CREATE INDEX [IX_SecurityEvents_UserTimeline]
ON [audit].[SecurityEvents]([UserId], [Timestamp] DESC);
CREATE INDEX [IX_SecurityEvents_Type]
ON [audit].[SecurityEvents]([EventType], [Timestamp] DESC);
GO
```

---

## 3. Tích hợp Tự động qua EF Core SaveChangesInterceptor

```csharp
public class AuditSaveChangesInterceptor : SaveChangesInterceptor
{
    private readonly IAuditLogService _auditService;
    private readonly ICurrentUserService _currentUser;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public AuditSaveChangesInterceptor(
        IAuditLogService auditService,
        ICurrentUserService currentUser,
        IHttpContextAccessor httpContextAccessor)
    {
        _auditService = auditService;
        _currentUser = currentUser;
        _httpContextAccessor = httpContextAccessor;
    }

    public override async ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken)
    {
        var context = eventData.Context;
        if (context == null) return result;

        var httpContext = _httpContextAccessor.HttpContext;
        var ipAddress = httpContext?.Connection?.RemoteIpAddress?.ToString();
        var userAgent = httpContext?.Request?.Headers["User-Agent"].ToString();

        var entries = context.ChangeTracker.Entries()
            .Where(e => e.State is EntityState.Added or EntityState.Modified or EntityState.Deleted)
            .ToList();

        foreach (var entry in entries)
        {
            var auditEntry = new AuditEntry
            {
                UserId = _currentUser.UserId,
                UserName = _currentUser.UserName,
                Action = entry.State switch
                {
                    EntityState.Added => "Create",
                    EntityState.Modified => "Update",
                    EntityState.Deleted => "Delete",
                    _ => "Unknown"
                },
                EntityType = entry.Entity.GetType().Name,
                EntityId = entry.Property("Id").CurrentValue?.ToString() ?? "",
                ModuleName = GetModuleName(entry.Entity.GetType()),
                OldValues = entry.State != EntityState.Added
                    ? JsonSerializer.Serialize(GetOriginalValues(entry))
                    : null,
                NewValues = entry.State != EntityState.Deleted
                    ? JsonSerializer.Serialize(GetCurrentValues(entry))
                    : null,
                IpAddress = ipAddress,
                UserAgent = userAgent
            };

            await _auditService.LogAsync(auditEntry, cancellationToken);
        }

        return result;
    }

    private static Dictionary<string, object?> GetOriginalValues(EntityEntry entry)
    {
        return entry.OriginalValues.Properties
            .ToDictionary(p => p.Name, p => entry.OriginalValues[p]);
    }

    private static Dictionary<string, object?> GetCurrentValues(EntityEntry entry)
    {
        return entry.CurrentValues.Properties
            .ToDictionary(p => p.Name, p => entry.CurrentValues[p]);
    }

    private static string GetModuleName(Type entityType)
    {
        var ns = entityType.Namespace ?? "";
        if (ns.Contains("Org")) return "OrgManagement";
        if (ns.Contains("Form")) return "FormMasterData";
        if (ns.Contains("Workflow")) return "WorkflowEngine";
        if (ns.Contains("Document")) return "DocumentManagement";
        if (ns.Contains("Notification")) return "Notification";
        if (ns.Contains("Ai")) return "AiCopilot";
        if (ns.Contains("Settings")) return "SystemSettings";
        if (ns.Contains("News")) return "InternalNews";
        return "Unknown";
    }
}
```

---

## 4. API Tra cứu Kiểm toán

```csharp
[ApiController]
[Route("api/audit")]
[Authorize(Policy = "CanViewAuditLogs")]
public class AuditController : ControllerBase
{
    private readonly IAuditQueryService _auditQuery;

    [HttpGet("entity/{entityType}/{entityId}")]
    public async Task<IActionResult> GetEntityHistory(
        string entityType, string entityId,
        [FromQuery] int page = 1, [FromQuery] int pageSize = 50,
        CancellationToken cancellationToken = default)
    {
        var result = await _auditQuery.GetEntityHistoryAsync(
            entityType, entityId, page, pageSize, cancellationToken);
        return Ok(result);
    }

    [HttpGet("user/{userId}")]
    public async Task<IActionResult> GetUserActivity(
        Guid userId,
        [FromQuery] DateTime? from, [FromQuery] DateTime? to,
        [FromQuery] int page = 1, [FromQuery] int pageSize = 50,
        CancellationToken cancellationToken = default)
    {
        var result = await _auditQuery.GetUserActivityAsync(
            userId, from, to, page, pageSize, cancellationToken);
        return Ok(result);
    }

    [HttpGet("export")]
    [Authorize(Policy = "CanExportAuditLogs")]
    public async Task<IActionResult> ExportAuditLogs(
        [FromQuery] DateTime from, [FromQuery] DateTime to,
        [FromQuery] string? moduleName, [FromQuery] string format = "xlsx",
        CancellationToken cancellationToken = default)
    {
        var fileBytes = await _auditQuery.ExportAsync(
            from, to, moduleName, format, cancellationToken);
        var contentType = format == "csv" ? "text/csv" : "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
        return File(fileBytes, contentType, $"audit_log_{from:yyyyMMdd}_{to:yyyyMMdd}.{format}");
    }
}
```

---

## 5. Cấu trúc Dự án & Phân tầng Mã nguồn

```
EnterpriseBpm.AuditLog/
├── EnterpriseBpm.AuditLog.Domain/
│   ├── Entities/ (AuditEntry.cs, SecurityEvent.cs)
│   └── Enums/ (AuditAction.cs, SecurityEventType.cs)
├── EnterpriseBpm.AuditLog.Application/
│   ├── Contracts/ (IAuditLogService.cs, IAuditQueryService.cs)
│   ├── Queries/ (GetEntityHistoryQuery.cs, GetUserActivityQuery.cs, ExportAuditLogsQuery.cs)
│   └── Services/ (AuditLogService.cs)
├── EnterpriseBpm.AuditLog.Infrastructure/
│   ├── Persistence/ (AuditDbContext.cs)
│   ├── Interceptors/ (AuditSaveChangesInterceptor.cs)
│   └── Export/ (ExcelAuditExporter.cs, CsvAuditExporter.cs)
└── EnterpriseBpm.AuditLog.Presentation/
    └── Controllers/ (AuditController.cs, SecurityEventsController.cs)
```

---

## 6. Điểm kiểm soát trọng yếu (Critical Control Points)

1. **Tính Bất biến Dữ liệu Kiểm toán (Immutable Audit Log):**
   - **Rủi ro:** Quản trị viên hoặc kẻ xấu có quyền DB cố tình xóa/sửa log kiểm toán để xóa dấu vết.
   - **Kiểm soát:** Sử dụng `BIGINT IDENTITY` làm Primary Key (không dùng GUID để tránh xóa chọn lọc). Cấu hình SQL Server DB User cho module AuditLog chỉ có quyền `INSERT` và `SELECT` trên schema `[audit]`, KHÔNG có quyền `UPDATE` hoặc `DELETE`. Bổ sung Database Trigger chặn mọi thao tác UPDATE/DELETE:
   ```sql
   CREATE TRIGGER [audit].[TR_AuditEntries_PreventModification]
   ON [audit].[AuditEntries]
   INSTEAD OF UPDATE, DELETE
   AS
   BEGIN
       RAISERROR('Audit entries cannot be modified or deleted.', 16, 1);
       ROLLBACK TRANSACTION;
   END;
   GO
   ```

2. **Phân quyền Truy cập Kiểm toán (Audit Access Control):**
   - **Rủi ro:** Nhân viên thường truy cập log kiểm toán để dò xét hoạt động của đồng nghiệp hoặc lãnh đạo.
   - **Kiểm soát:** API tra cứu audit bắt buộc yêu cầu Policy `CanViewAuditLogs` (chỉ cấp cho Kiểm toán Nội bộ, Ban Giám đốc). API export bắt buộc yêu cầu Policy `CanExportAuditLogs` (chỉ cấp cho Trưởng phòng Kiểm toán).

3. **Hiệu năng Ghi Nhận (Write Performance Optimization):**
   - **Rủi ro:** Mỗi thao tác CRUD đều ghi thêm 1 dòng audit → tăng latency và tải cho SQL Server.
   - **Kiểm soát:** Sử dụng Buffered Write: gom các audit entry trong `System.Threading.Channels.Channel` và flush batch INSERT (50-100 dòng/batch) mỗi 5 giây, thay vì insert từng dòng. Bảng AuditEntries cấu hình Partition theo tháng (`RANGE RIGHT FOR VALUES`) để tối ưu hiệu năng truy vấn dài hạn.

4. **Chính sách Lưu trữ Dữ liệu (Data Retention Policy):**
   - **Rủi ro:** Bảng audit tăng trưởng vô hạn sau nhiều năm hoạt động, ảnh hưởng hiệu năng.
   - **Kiểm soát:** Dữ liệu audit được giữ trực tuyến (online) tối thiểu 2 năm. Sau 2 năm, tự động chuyển sang bảng Archive (`[audit].[AuditEntries_Archive]`) hoặc xuất ra Azure Blob Storage/OneDrive dạng Parquet. SQL Agent Job chạy hàng tháng để thực hiện partition switching.
