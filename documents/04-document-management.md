# Module 04: DocumentManagement (Quản lý Tài liệu, Template & Tích hợp OneDrive)

## 1. Tổng quan & Phạm vi Bounded Context
Module `DocumentManagement` loại bỏ hoàn toàn việc lưu trữ file nhị phân (Binary Blobs) nặng nề trong SQL Server. Thay vào đó, toàn bộ file đính kèm của Form.io (hợp đồng PDF, báo giá Excel, hình ảnh) được đẩy trực tiếp lên nền tảng **OneDrive for Business** của doanh nghiệp thông qua **Microsoft Graph API**.

Ngoài ra, module còn chịu trách nhiệm **sinh tài liệu tự động từ template** (Quyết định, Hợp đồng, Biên bản) khi quy trình hoàn tất, giúp chuẩn hóa đầu ra văn bản toàn tập đoàn.

### Ranh giới kiến trúc
- **Database Schema riêng biệt:** `[document]` trên SQL Server (chỉ lưu Metadata, ID tham chiếu và thông tin Template).
- **Hạ tầng lưu trữ:** OneDrive/SharePoint Document Library thuộc Microsoft 365 Tenant của tập đoàn.
- **Bảo mật truy cập:** Cung cấp Time-limited Signed URLs cho người dùng xem trước tài liệu, tuyệt đối không mở Public truy cập file.
- **Sinh tài liệu tự động:** Sinh tài liệu tự động từ template (Quyết định, Hợp đồng, Biên bản) khi quy trình hoàn tất.

---

## 2. Thiết kế Cơ sở dữ liệu (SQL Server - Schema [document])

```sql
CREATE SCHEMA [document];
GO

CREATE TABLE [document].[WorkflowAttachments] (
    [Id] UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    [WorkflowInstanceId] NVARCHAR(100) NOT NULL,
    [FormKey] NVARCHAR(100) NOT NULL,
    [OriginalFileName] NVARCHAR(255) NOT NULL,
    [FileExtension] NVARCHAR(20) NOT NULL,
    [ContentType] NVARCHAR(100) NOT NULL,
    [FileSizeBytes] BIGINT NOT NULL,
    [OneDriveItemId] NVARCHAR(255) NOT NULL, -- ID định danh tài liệu trên Graph API
    [OneDriveWebUrl] NVARCHAR(1000) NOT NULL, -- Đường dẫn xem trực tiếp nội bộ
    [UploadedByUserId] UNIQUEIDENTIFIER NOT NULL,
    [CreatedAt] DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    [DeletedAt] DATETIME2 NULL,
    [DeletedBy] UNIQUEIDENTIFIER NULL
);
CREATE INDEX [IX_Attachments_Workflow] 
ON [document].[WorkflowAttachments]([WorkflowInstanceId]);
GO

-- Bảng Template Tài liệu
CREATE TABLE [document].[DocumentTemplates] (
    [Id] UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    [TemplateKey] NVARCHAR(100) NOT NULL UNIQUE,
    [Title] NVARCHAR(255) NOT NULL,
    [Description] NVARCHAR(500) NULL,
    [TemplateFormat] NVARCHAR(20) NOT NULL, -- "docx", "pdf", "xlsx"
    [OneDriveTemplateItemId] NVARCHAR(255) NOT NULL,
    [PlaceholderSchema] NVARCHAR(MAX) NOT NULL, -- JSON: {"TenNguoiKy": "string", "NgayKy": "date"}
    [IsActive] BIT NOT NULL DEFAULT 1,
    [CreatedAt] DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    [CreatedBy] UNIQUEIDENTIFIER NULL,
    [UpdatedAt] DATETIME2 NULL,
    [UpdatedBy] UNIQUEIDENTIFIER NULL
);
GO

-- Bảng Tài liệu Đã sinh từ Template
CREATE TABLE [document].[GeneratedDocuments] (
    [Id] UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    [TemplateId] UNIQUEIDENTIFIER NOT NULL,
    [WorkflowInstanceId] NVARCHAR(100) NOT NULL,
    [GeneratedFileName] NVARCHAR(255) NOT NULL,
    [OneDriveItemId] NVARCHAR(255) NOT NULL,
    [OneDriveWebUrl] NVARCHAR(1000) NOT NULL,
    [GeneratedByUserId] UNIQUEIDENTIFIER NOT NULL,
    [GeneratedAt] DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    CONSTRAINT [FK_GeneratedDocs_Template] FOREIGN KEY ([TemplateId])
        REFERENCES [document].[DocumentTemplates]([Id])
);
CREATE INDEX [IX_GeneratedDocs_Workflow]
ON [document].[GeneratedDocuments]([WorkflowInstanceId]);
GO
```

---

## 3. Triển khai Kỹ thuật với Microsoft.Graph SDK

### 3.1. Hợp đồng Tích hợp Nội bộ (Public Contract) - Quản lý File đính kèm
```csharp
public interface IDocumentStorageService
{
    Task<AttachmentDto> UploadFileAsync(
        string workflowInstanceId, 
        string fileName, 
        Stream fileStream, 
        string contentType, 
        Guid uploadedByUserId, 
        CancellationToken cancellationToken);

    Task<string> GetSecurePreviewUrlAsync(Guid attachmentId, CancellationToken cancellationToken);
}
```

### 3.2. Hợp đồng Sinh Tài liệu từ Template
```csharp
public interface IDocumentGeneratorService
{
    Task<GeneratedDocumentDto> GenerateFromTemplateAsync(
        string templateKey,
        string workflowInstanceId,
        Dictionary<string, object> placeholderValues,
        Guid generatedByUserId,
        CancellationToken cancellationToken);
}
```

### 3.3. Cấu trúc Thư mục Tổ chức trên OneDrive
Tập tin được lưu trữ tự động theo cây phân cấp logic để tránh giới hạn số lượng file trên một thư mục của SharePoint/OneDrive:
```
/BPMN_Enterprise_Storage/
└── {Year}/
    └── {Month}/
        └── {WorkflowInstanceId}/
            └── [Guid]_{OriginalFileName}.pdf
```

### 3.4. Xử lý Upload qua GraphServiceClient
```csharp
public class OneDriveStorageService : IDocumentStorageService
{
    private readonly GraphServiceClient _graphClient;
    private readonly IDocumentDbContext _dbContext;

    public OneDriveStorageService(GraphServiceClient graphClient, IDocumentDbContext dbContext)
    {
        _graphClient = graphClient;
        _dbContext = dbContext;
    }

    public async Task<AttachmentDto> UploadFileAsync(
        string workflowInstanceId, string fileName, Stream fileStream, 
        string contentType, Guid uploadedByUserId, CancellationToken cancellationToken)
    {
        var year = DateTime.UtcNow.Year;
        var month = DateTime.UtcNow.Month.ToString("D2");
        var targetPath = $"/BPMN_Enterprise_Storage/{year}/{month}/{workflowInstanceId}/{Guid.NewGuid()}_{fileName}";

        // Tải file lên OneDrive doanh nghiệp thông qua Graph API
        var driveItem = await _graphClient.Drives["{drive-id}"].Root
            .ItemWithPath(targetPath)
            .Content
            .PutAsync(fileStream, cancellationToken: cancellationToken);

        // Lưu metadata vào SQL Server
        var attachment = new WorkflowAttachment
        {
            WorkflowInstanceId = workflowInstanceId,
            OriginalFileName = fileName,
            FileExtension = Path.GetExtension(fileName).ToLower(),
            ContentType = contentType,
            FileSizeBytes = fileStream.Length,
            OneDriveItemId = driveItem.Id,
            OneDriveWebUrl = driveItem.WebUrl,
            UploadedByUserId = uploadedByUserId
        };

        _dbContext.WorkflowAttachments.Add(attachment);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return new AttachmentDto(attachment.Id, attachment.OriginalFileName, attachment.OneDriveWebUrl);
    }
}
```

---

## 4. Xử lý Sự kiện: Tự động Sinh Tài liệu khi Duyệt (WorkflowApprovedEvent)

Khi một phiếu trình ký được duyệt hoàn tất, hệ thống tự động kiểm tra cấu hình template tương ứng và sinh tài liệu đầu ra (Quyết định, Hợp đồng, Biên bản) lưu trực tiếp lên OneDrive.

```csharp
public class AutoGenerateDocumentOnApprovalHandler : INotificationHandler<WorkflowApprovedEvent>
{
    private readonly IDocumentGeneratorService _generator;
    private readonly IFormDataService _formDataService;

    public AutoGenerateDocumentOnApprovalHandler(
        IDocumentGeneratorService generator, IFormDataService formDataService)
    {
        _generator = generator;
        _formDataService = formDataService;
    }

    public async Task Handle(WorkflowApprovedEvent notification, CancellationToken cancellationToken)
    {
        // Kiểm tra form này có cấu hình template tự động sinh không
        var templateKey = await _formDataService.GetAutoGenerateTemplateKeyAsync(
            notification.FormKey, cancellationToken);
        
        if (templateKey == null) return;

        var formData = await _formDataService.GetFormDataAsync(
            notification.WorkflowInstanceId, cancellationToken);

        await _generator.GenerateFromTemplateAsync(
            templateKey,
            notification.WorkflowInstanceId,
            formData,
            notification.ApprovedByUserId,
            cancellationToken);
    }
}
```

---

## 5. Xử lý Sự kiện: Xử lý Tài liệu khi Phiếu bị Hủy (WorkflowCancelledEvent)

Khi một phiếu trình ký bị hủy, các tài liệu đính kèm liên quan được đánh dấu soft-delete (archive) để bảo toàn tính truy vết nhưng không hiển thị trong luồng nghiệp vụ thông thường.

```csharp
public class HandleDocumentOnWorkflowCancelledHandler : INotificationHandler<WorkflowCancelledEvent>
{
    private readonly IDocumentDbContext _dbContext;

    public async Task Handle(WorkflowCancelledEvent notification, CancellationToken cancellationToken)
    {
        // Đánh dấu tài liệu đính kèm của phiếu bị hủy (soft-delete hoặc archive)
        var attachments = await _dbContext.WorkflowAttachments
            .Where(a => a.WorkflowInstanceId == notification.WorkflowInstanceId)
            .ToListAsync(cancellationToken);

        foreach (var attachment in attachments)
        {
            attachment.DeletedAt = DateTime.UtcNow;
            attachment.DeletedBy = notification.CancelledByUserId;
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}
```

---

## 6. Điểm kiểm soát trọng yếu (Critical Control Points)

1. **Zero-Byte Database Storage (Tuyệt đối không lưu Blob vào DB):**
   - **Rủi ro:** Khi tập đoàn hoạt động lâu năm, hàng triệu file đính kèm hợp đồng sẽ làm phình dữ liệu SQL Server lên hàng Terabyte, làm chậm toàn bộ hệ thống và tăng chi phí sao lưu.
   - **Kiểm soát:** Database SQL Server chỉ được phép lưu metadata. Mọi payload nhị phân phải đẩy thẳng sang OneDrive qua stream mà không lưu buffer đệm trên đĩa cứng local quá lâu.
2. **Whitelist Định dạng & Quét Dung lượng (File Validation Security):**
   - **Rủi ro:** Người dùng tải lên mã độc (`.exe`, `.sh`, `.bat`) hoặc file dung lượng quá lớn gây DoS băng thông.
   - **Kiểm soát:** Kiểm tra phần mở rộng file (chỉ chấp nhận: `.pdf`, `.docx`, `.xlsx`, `.pptx`, `.jpg`, `.jpeg`, `.png`) và giới hạn dung lượng tối đa cho mỗi file là $25\text{ MB}$.
3. **Quản lý Token & Circuit Breaker với Microsoft Graph:**
   - **Rủi ro:** Token xác thực Azure Entra ID hết hạn giữa chừng hoặc Microsoft Graph API bị nghẽn (HTTP 429 Too Many Requests).
   - **Kiểm soát:** Đăng ký Graph SDK sử dụng `ClientSecretCredential` với cơ chế tự động refresh token ngầm, kết hợp thư viện `Polly` để retry theo chuẩn Exponential Backoff.
4. **Template Placeholder Injection Prevention (Chống tiêm Mã độc qua Placeholder):**
   - **Rủi ro:** Giá trị placeholder values được truyền vào template có thể chứa macro VBA độc hại, cho phép thực thi mã không kiểm soát khi mở tài liệu đã sinh.
   - **Kiểm soát:** Sanitize tất cả giá trị placeholder trước khi merge vào template. Strip toàn bộ macro VBA, ActiveX controls và external data connections khỏi tài liệu đầu ra. Sử dụng thư viện xử lý document (OpenXML SDK) để kiểm tra và loại bỏ nội dung nguy hiểm.
5. **Generated Document Integrity (Tính toàn vẹn Tài liệu Đã sinh):**
   - **Rủi ro:** Tài liệu đã sinh (Quyết định, Hợp đồng) bị sửa đổi trái phép trên OneDrive sau khi hệ thống tạo ra, dẫn đến sai lệch nội dung pháp lý.
   - **Kiểm soát:** Tính SHA-256 hash ngay tại thời điểm sinh tài liệu và lưu vào cơ sở dữ liệu. Triển khai scheduled job định kỳ kiểm tra tính toàn vẹn bằng cách so sánh hash hiện tại trên OneDrive với hash gốc trong DB. Phát cảnh báo nếu phát hiện bất kỳ thay đổi nào.
