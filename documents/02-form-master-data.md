# Module 02: FormMasterData (Form.io & Master Data Tập trung)

## 1. Tổng quan & Phạm vi Bounded Context
Module `FormMasterData` chịu trách nhiệm:
1. Lưu trữ và phân phiên bản JSON Schema của Form.io với hệ thống **quản lý phiên bản (Form Versioning)** hoàn chỉnh.
2. Cung cấp API danh mục Master Data tập trung (Phòng ban, Chức danh, Dự án, Trung tâm chi phí, Ngân sách) có hỗ trợ **Cascading Loading** (đổ dữ liệu phụ thuộc).
3. Thực thi Server-side Validation nghiêm ngặt chống giả mạo dữ liệu.

### Ranh giới kiến trúc
- **Database Schema riêng biệt:** `[form]` trên SQL Server.
- **Tầng Cache:** Tích hợp Redis Distributed Cache để cache dữ liệu danh mục đọc nhiều.
- **Client Render Engine:** Form.io JavaScript Core / Angular/React/Vue Form Renderer.

---

## 2. Thiết kế Cơ sở dữ liệu (SQL Server - Schema [form])

```sql
CREATE SCHEMA [form];
GO

-- 1. Bảng Lưu trữ Định nghĩa Form.io (hỗ trợ Multi-Version)
CREATE TABLE [form].[FormDefinitions] (
    [Id] UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    [FormKey] NVARCHAR(100) NOT NULL,         -- Ví dụ: "payment_request", "leave_application"
    [Title] NVARCHAR(255) NOT NULL,
    [SchemaJson] NVARCHAR(MAX) NOT NULL,      -- JSON Schema hoàn chỉnh của Form.io
    [Version] INT NOT NULL DEFAULT 1,
    [IsLatest] BIT NOT NULL DEFAULT 1,        -- Đánh dấu phiên bản hiện hành
    [IsActive] BIT NOT NULL DEFAULT 1,
    [ChangeNotes] NVARCHAR(500) NULL,         -- Ghi chú thay đổi khi tạo version mới
    [PublishedBy] UNIQUEIDENTIFIER NULL,      -- Người xuất bản phiên bản này
    [CreatedAt] DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    [CreatedBy] UNIQUEIDENTIFIER NULL,
    [UpdatedAt] DATETIME2 NULL,
    [UpdatedBy] UNIQUEIDENTIFIER NULL
);

-- Composite Unique Filtered Index: đảm bảo mỗi FormKey chỉ có 1 bản ghi IsLatest=1
CREATE UNIQUE INDEX [UX_FormDefinitions_LatestPerFormKey]
ON [form].[FormDefinitions]([FormKey])
WHERE [IsLatest] = 1;

-- Index phục vụ tra cứu lịch sử phiên bản (sắp xếp Version giảm dần)
CREATE INDEX [IX_FormDefinitions_VersionHistory]
ON [form].[FormDefinitions]([FormKey], [Version] DESC);
GO

-- 2. Bảng Danh mục Master Data Cấp 1
CREATE TABLE [form].[MasterCategories] (
    [Id] UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    [CategoryKey] NVARCHAR(50) NOT NULL UNIQUE, -- "CostCenters", "ProjectCodes", "Vendors"
    [Name] NVARCHAR(255) NOT NULL,
    [Description] NVARCHAR(500) NULL,
    [IsActive] BIT NOT NULL DEFAULT 1
);
GO

-- 3. Bảng Dữ liệu Danh mục Cấp con (Hỗ trợ Cây phân cấp Cascading)
CREATE TABLE [form].[MasterItems] (
    [Id] UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    [CategoryId] UNIQUEIDENTIFIER NOT NULL,
    [ParentId] UNIQUEIDENTIFIER NULL, -- Phục vụ lọc Cascading (ví dụ: Chọn Dự án -> Tải Hạng mục con)
    [Code] NVARCHAR(50) NOT NULL,
    [Name] NVARCHAR(255) NOT NULL,
    [DisplayOrder] INT NOT NULL DEFAULT 0,
    [CustomAttributesJson] NVARCHAR(MAX) NULL, -- Lưu thêm hạn mức, metadata JSON
    [IsActive] BIT NOT NULL DEFAULT 1,
    CONSTRAINT [FK_MasterItems_Category] FOREIGN KEY ([CategoryId]) REFERENCES [form].[MasterCategories]([Id]),
    CONSTRAINT [FK_MasterItems_Parent] FOREIGN KEY ([ParentId]) REFERENCES [form].[MasterItems]([Id])
);
-- Non-clustered Index tối ưu hóa truy vấn Cascading Loading < 10ms
CREATE INDEX [IX_MasterItems_CascadingQuery] 
ON [form].[MasterItems]([CategoryId], [ParentId], [IsActive]) 
INCLUDE ([Code], [Name], [DisplayOrder]);
GO
```

---

## 3. Quản lý Phiên bản Form (Form Version Management)

### 3.1. Public Contract — IFormVersionService
```csharp
public interface IFormVersionService
{
    /// <summary>
    /// Tạo phiên bản mới từ phiên bản hiện hành. Tự động tăng Version, 
    /// đặt IsLatest=0 cho bản cũ, IsLatest=1 cho bản mới.
    /// </summary>
    Task<FormDefinitionDto> CreateNewVersionAsync(
        string formKey,
        string schemaJson,
        string? changeNotes,
        CancellationToken cancellationToken);

    /// <summary>
    /// Lấy phiên bản cụ thể của form theo FormKey + Version.
    /// Nếu không truyền version, trả về bản IsLatest=1.
    /// </summary>
    Task<FormDefinitionDto?> GetVersionAsync(
        string formKey,
        int? version,
        CancellationToken cancellationToken);

    /// <summary>
    /// Rollback về phiên bản cũ: tạo bản sao của version chỉ định thành version mới nhất.
    /// KHÔNG xóa bản ghi cũ để bảo toàn lịch sử.
    /// </summary>
    Task<FormDefinitionDto> RollbackToVersionAsync(
        string formKey,
        int targetVersion,
        string? rollbackReason,
        CancellationToken cancellationToken);
}
```

### 3.2. Logic xử lý Version trong Application Layer
```csharp
public class FormVersionService : IFormVersionService
{
    private readonly IFormDefinitionRepository _repo;
    private readonly IMediator _mediator;
    private readonly ICurrentUserService _currentUser;

    public async Task<FormDefinitionDto> CreateNewVersionAsync(
        string formKey, string schemaJson, string? changeNotes,
        CancellationToken cancellationToken)
    {
        // 1. Lấy phiên bản hiện hành
        var current = await _repo.GetLatestByFormKeyAsync(formKey, cancellationToken)
            ?? throw new FormNotFoundException(formKey);

        // 2. Đánh dấu phiên bản cũ không còn là Latest
        current.IsLatest = false;
        await _repo.UpdateAsync(current, cancellationToken);

        // 3. Tạo phiên bản mới
        var newVersion = new FormDefinition
        {
            FormKey = formKey,
            Title = current.Title,
            SchemaJson = schemaJson,
            Version = current.Version + 1,
            IsLatest = true,
            ChangeNotes = changeNotes,
            PublishedBy = _currentUser.UserId
        };
        await _repo.AddAsync(newVersion, cancellationToken);

        // 4. Phát Integration Event thông báo Module Workflow
        await _mediator.Publish(new FormDefinitionUpdatedEvent(
            FormKey: formKey,
            OldVersion: current.Version,
            NewVersion: newVersion.Version
        ), cancellationToken);

        return newVersion.ToDto();
    }

    // ... GetVersionAsync, RollbackToVersionAsync implementation ...
}
```

---

## 4. Cơ chế Cascading Loading & Triển khai Caching

### 4.1. API Cascading Controller (.NET Core)
```csharp
[ApiController]
[Route("api/master-data")]
public class MasterDataController : ControllerBase
{
    private readonly IMasterDataService _masterDataService;

    public MasterDataController(IMasterDataService masterDataService)
    {
        _masterDataService = masterDataService;
    }

    [HttpGet("items")]
    public async Task<IActionResult> GetItems(
        [FromQuery] string categoryKey, 
        [FromQuery] Guid? parentId, 
        CancellationToken cancellationToken)
    {
        var items = await _masterDataService.GetItemsAsync(categoryKey, parentId, cancellationToken);
        return Ok(items);
    }
}
```

### 4.2. Cấu hình Redis Cache Layer
- **Cache Key Template:** `masterdata:{categoryKey}:parent:{parentId ?? "root"}`
- **TTL (Time to Live):** 30 phút.
- **Cache Invalidation:** Khi có thêm/sửa/xóa trong bảng `MasterItems`, tự động xóa sạch các pattern key liên quan đến `categoryKey` đó.

### 4.3. Cấu hình JSON Component trên Form.io
```json
{
  "type": "select",
  "key": "subProjectId",
  "label": "Hạng Mục Chi Tiết",
  "dataSrc": "url",
  "data": {
    "url": "https://api.domain.com/api/master-data/items?categoryKey=SubProjects&parentId={{ data.projectId }}"
  },
  "valueProperty": "id",
  "template": "<span>{{ item.name }} ({{ item.code }})</span>",
  "refreshOn": "projectId",
  "clearOnRefresh": true,
  "validate": {
    "required": true
  }
}
```

---

## 5. Outgoing Integration Events

```csharp
/// <summary>
/// Phát khi có phiên bản Form mới được tạo hoặc cập nhật.
/// Module Workflow lắng nghe để quyết định có cần cập nhật schema render cho workflow đang chạy.
/// </summary>
public record FormDefinitionUpdatedEvent(
    string FormKey,
    int OldVersion,
    int NewVersion
) : BaseIntegrationEvent;
```

---

## 6. Điểm kiểm soát trọng yếu (Critical Control Points)

1. **Anti-Tampering Validation (Chống Giả mạo Dữ liệu Phụ thuộc):**
   - **Rủi ro:** Người dùng mở Chrome DevTools sửa giá trị `subProjectId` thành một ID không thuộc về `projectId` đã chọn trước khi nhấn Gửi duyệt.
   - **Kiểm soát:** Viết `FluentValidation` rule ở tầng Application. Khi submit form, bắt buộc truy vấn Database kiểm tra điều kiện logic:
     ```csharp
     RuleFor(x => x).MustAsync(async (submission, cancellation) => 
     {
         return await _repo.ValidateItemBelongsToParentAsync(
             submission.SubProjectId, submission.ProjectId, cancellation);
     }).WithMessage("Hạng mục đã chọn không hợp lệ đối với dự án hiện tại.");
     ```
2. **Schema Sanitization (Chống XSS trong Form.io Schema):**
   - **Rủi ro:** Admin cấu hình form chèn mã JavaScript độc hại vào thuộc tính `customDefaultValue` hoặc `HTML component`.
   - **Kiểm soát:** Chạy qua bộ lọc làm sạch JSON và chỉ cho phép lưu trữ các thuộc tính hợp lệ theo whitelist schema của Form.io trước khi lưu vào `FormDefinitions`.
3. **Tối ưu hóa Truy vấn Đọc:**
   - **Rủi ro:** Khi form mở ra, 5-10 dropdown cascading đồng loạt gọi API làm nghẽn connection pool của SQL Server.
   - **Kiểm soát:** 100% truy vấn Master Data phải sử dụng `.AsNoTracking()` trong Entity Framework Core kết hợp Index `IX_MasterItems_CascadingQuery`.
4. **An toàn Rollback Phiên bản Form (Version Rollback Safety):**
   - **Rủi ro:** Rollback về phiên bản cũ có thể làm mất tương thích với dữ liệu `FormDataJson` đã submit trên phiên bản mới hơn, gây lỗi khi render lại các phiếu trình ký đang xử lý.
   - **Kiểm soát:** (a) Rollback KHÔNG xóa bản ghi version cũ mà tạo version mới (copy nội dung). (b) Trước khi rollback, hệ thống phải kiểm tra xem có Workflow đang `InProgress` sử dụng form này không — nếu có, cảnh báo Admin và yêu cầu xác nhận rõ ràng. (c) Workflow đang chạy giữ chừng vẫn render theo Version đã submit ban đầu (snapshot), không bị ảnh hưởng bởi rollback.
