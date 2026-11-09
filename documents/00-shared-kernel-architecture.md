# Tổng quan Kiến trúc Modular Monolith & Shared Kernel

## 1. Cây Thư mục Toàn bộ Solution (.NET Core)

```
EnterpriseBpmPlatform/
├── src/
│   ├── Shared/
│   │   ├── EnterpriseBpm.SharedKernel/             # Base Entity, Guard Clauses, Result Pattern, ISoftDeletable, IAuditable
│   │   └── EnterpriseBpm.EventBus/                 # MediatR Notifications & Integration Event Base
│   │
│   ├── Modules/
│   │   ├── OrgManagement/                          # Module 01 (Schema: [org]) — Cơ cấu tổ chức, Nhân sự, Chức danh, Danh bạ
│   │   ├── FormMasterData/                         # Module 02 (Schema: [form]) — Form.io Schema, Versioning, Master Data
│   │   ├── WorkflowEngine/                         # Module 03 (Schema: [workflow]) — Elsa Workflows v3, Approval Tasks, History
│   │   ├── DocumentManagement/                     # Module 04 (Schema: [document]) — OneDrive/Graph API, Document Templates
│   │   ├── Notification/                           # Module 05 (Schema: [notification]) — Gateway đa kênh (Email, Teams, Zalo, SMS)
│   │   ├── AiCopilot/                              # Module 06 (Schema: [ai]) — Local MCP Server, RAG Qdrant Quy chế
│   │   ├── SystemSettings/                         # Module 07 (Schema: [settings]) — Cấu hình hệ thống, White-labeling
│   │   ├── InternalNews/                           # Module 08 (Schema: [news]) — Bản tin, Truyền thông nội bộ, Hẹn giờ đăng
│   │   ├── AuditLog/                               # Module 09 (Schema: [audit]) — Kiểm toán CUD, Bảo mật, Trigger bất biến
│   │   ├── AnalyticsReporting/                     # Module 10 (Schema: [analytics]) — Báo cáo toàn hệ thống, Performance KPI, SLA
│   │   └── AccessGovernance/                       # Module 11 (Schema: [auth]) — Quản trị Phân quyền toàn hệ thống, RBAC/ABAC
│   │
│   └── Host/
│       └── EnterpriseBpm.ApiHost/                  # ASP.NET Core Web API Host duy nhất
│
├── tests/
│   ├── EnterpriseBpm.ArchTests/                    # NetArchTest kiểm soát ranh giới Module
│   └── EnterpriseBpm.IntegrationTests/
└── docs/
    └── modules/                                    # Bộ tài liệu đặc tả 11 module chuẩn quốc tế
```

---

## 2. Shared Kernel — Base Entity & Auditable Contracts

### 2.1. ISoftDeletable Interface (Xóa mềm thống nhất)

Mọi Entity quan trọng trong hệ thống cần triển khai interface `ISoftDeletable` để hỗ trợ xóa mềm thay vì xóa vật lý khỏi database:

```csharp
namespace EnterpriseBpm.SharedKernel.Contracts;

public interface ISoftDeletable
{
    DateTime? DeletedAt { get; set; }
    Guid? DeletedBy { get; set; }
}
```

Cấu hình Global Query Filter trong mỗi DbContext Module:

```csharp
// Trong OnModelCreating() của mỗi Module DbContext
foreach (var entityType in modelBuilder.Model.GetEntityTypes())
{
    if (typeof(ISoftDeletable).IsAssignableFrom(entityType.ClrType))
    {
        var parameter = Expression.Parameter(entityType.ClrType, "e");
        var property = Expression.Property(parameter, nameof(ISoftDeletable.DeletedAt));
        var condition = Expression.Equal(property, Expression.Constant(null, typeof(DateTime?)));
        var lambda = Expression.Lambda(condition, parameter);
        
        modelBuilder.Entity(entityType.ClrType).HasQueryFilter(lambda);
    }
}
```

### 2.2. IAuditable Interface (Cột kiểm toán tự động)

Mọi Entity cần ghi nhận thông tin người tạo/sửa phải triển khai `IAuditable`:

```csharp
namespace EnterpriseBpm.SharedKernel.Contracts;

public interface IAuditable
{
    DateTime CreatedAt { get; set; }
    Guid? CreatedBy { get; set; }
    DateTime? UpdatedAt { get; set; }
    Guid? UpdatedBy { get; set; }
}
```

Tự động gán giá trị qua `SaveChangesInterceptor`:

```csharp
public class AuditableFieldsInterceptor : SaveChangesInterceptor
{
    private readonly ICurrentUserService _currentUser;

    public AuditableFieldsInterceptor(ICurrentUserService currentUser)
    {
        _currentUser = currentUser;
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        var context = eventData.Context;
        if (context == null) return ValueTask.FromResult(result);

        foreach (var entry in context.ChangeTracker.Entries<IAuditable>())
        {
            switch (entry.State)
            {
                case EntityState.Added:
                    entry.Entity.CreatedAt = DateTime.UtcNow;
                    entry.Entity.CreatedBy = _currentUser.UserId;
                    break;
                case EntityState.Modified:
                    entry.Entity.UpdatedAt = DateTime.UtcNow;
                    entry.Entity.UpdatedBy = _currentUser.UserId;
                    break;
            }
        }

        return ValueTask.FromResult(result);
    }
}
```

---

## 3. Shared Kernel — Base Integration Events

Mọi sự kiện giao tiếp giữa các module kế thừa từ `BaseIntegrationEvent`:

```csharp
namespace EnterpriseBpm.SharedKernel.Events;

public interface IIntegrationEvent : INotification
{
    Guid EventId { get; }
    DateTime OccurredOn { get; }
}

public abstract record BaseIntegrationEvent : IIntegrationEvent
{
    public Guid EventId { get; init; } = Guid.NewGuid();
    public DateTime OccurredOn { get; init; } = DateTime.UtcNow;
}
```

---

## 4. Kiểm soát Ranh giới Kiến trúc bằng Architecture Tests (NetArchTest)

Để đảm bảo các lập trình viên hoặc AI Coding Agent không vi phạm nguyên tắc Modular (ví dụ: Module Workflow vô tình trỏ reference sang tầng Infrastructure của Module Org), chúng ta thiết lập bộ kiểm tra kiến trúc tự động:

```csharp
[Fact]
public void Modules_Should_Not_Reference_Other_Module_Infrastructure()
{
    var result = Types.InNamespace("EnterpriseBpm.Workflow")
        .ShouldNot()
        .HaveDependencyOn("EnterpriseBpm.Org.Infrastructure")
        .GetResult();

    Assert.True(result.IsSuccessful, "Module Workflow không được phụ thuộc trực tiếp vào Infrastructure của Module Org!");
}

[Fact]
public void AiCopilot_Should_Not_Reference_Analytics_Infrastructure()
{
    var result = Types.InNamespace("EnterpriseBpm.AiCopilot")
        .ShouldNot()
        .HaveDependencyOn("EnterpriseBpm.Analytics.Infrastructure")
        .GetResult();

    Assert.True(result.IsSuccessful, "Module AiCopilot không được phụ thuộc trực tiếp vào Infrastructure của Module Analytics!");
}

[Fact]
public void All_Modules_Must_Only_Depend_On_AccessGovernance_Contracts()
{
    var result = Types.InNamespace("EnterpriseBpm.Modules")
        .That().DoNotResideInNamespace("EnterpriseBpm.AccessGovernance")
        .ShouldNot()
        .HaveDependencyOn("EnterpriseBpm.AccessGovernance.Infrastructure")
        .GetResult();

    Assert.True(result.IsSuccessful, "Các module chỉ được phép tham chiếu Contracts của AccessGovernance, không được trỏ vào tầng Infrastructure!");
}
```

---

## 5. Ma trận Integration Events Toàn hệ thống (11 Modules)

| Module Nguồn | Sự kiện | Module Đích | Mục đích Nghiệp vụ |
|---|---|---|---|
| OrgManagement | `EmployeeDeactivatedEvent` | WorkflowEngine, Notification, AccessGovernance | Thu hồi Active Tasks, vô hiệu hóa quyền hạn, thông báo Quản lý |
| OrgManagement | `OrgUnitRestructuredEvent` | AnalyticsReporting, AccessGovernance | Tái tính toán số liệu BI, cập nhật Data Scope Policies |
| FormMasterData | `FormDefinitionUpdatedEvent` | WorkflowEngine, AccessGovernance | Thông báo biểu mẫu có phiên bản mới, cập nhật Form Permissions |
| WorkflowEngine | `WorkflowApprovedEvent` | Notification, AnalyticsReporting, DocumentManagement | Gửi email thông báo, tính metrics tài chính, sinh tài liệu template |
| WorkflowEngine | `WorkflowRejectedEvent` | Notification, AnalyticsReporting | Thông báo người nộp đơn, cập nhật chỉ số từ chối |
| WorkflowEngine | `WorkflowCancelledEvent` | Notification, DocumentManagement | Thông báo người liên quan, hủy/archive file đính kèm |
| WorkflowEngine | `TaskAssignedEvent` | Notification | Gửi thông báo có nhiệm vụ mới cần xử lý |
| WorkflowEngine | `TaskSlaBreachedEvent` | Notification, AnalyticsReporting | Gửi cảnh báo khẩn cấp, ghi nhận SLA Breach KPI |
| WorkflowEngine | `WorkflowCompletedEvent` | AnalyticsReporting | Cập nhật tổng thể Lead Time và First-Time-Right Rate |
| InternalNews | `NewsPublishedEvent` | Notification | Gửi email/Teams thông báo tin tức quan trọng |
| InternalNews | `NewsArchivedEvent` | AiCopilot | Gỡ bỏ vectors bài viết khỏi RAG Knowledge Base |
| AccessGovernance | `RolePermissionsChangedEvent` | AccessGovernance.Cache | Làm mới Redis Cache quyền của toàn bộ user thuộc vai trò |
| Tất cả Module | `AuditableActionEvent` | AuditLog | Ghi nhận nhật ký kiểm toán bất biến (Append-Only) |

---

## 6. Kiến trúc Caching Đa tầng (Two-Tier Caching) & Quản trị Redis Server Độc lập

### 6.1. Chiến lược Hai tầng Cache (L1 In-Memory + L2 Distributed Redis)
Nhằm đạt mục tiêu độ trễ cực thấp ($P99 < 30\text{ ms}$) và thông lượng cao ($> 1,000\text{ RPS}$), toàn bộ hệ thống sử dụng mô hình cache 2 tầng chuẩn mực:
- **Tầng 1 (L1 Cache - In-Memory `IMemoryCache`):** 
  - Nằm trực tiếp trên RAM của tiến trình API Host.
  - Phục vụ các dữ liệu đọc liên tục với độ trễ nano-second/micro-second (danh mục Form tĩnh, quyền hạn vừa kiểm tra, SystemSettings).
  - TTL ngắn (thường từ 1 - 5 phút).
- **Tầng 2 (L2 Cache - Distributed `StackExchange.Redis`):** 
  - Đặt trên cụm Redis Server / Docker độc lập, dùng chung giữa nhiều instances/pods của API Host.
  - Phục vụ chia sẻ trạng thái phân tán: Cascading Master Data (`masterdata:{category}:{parentId}`), Phân quyền người dùng (`user_perms:{userId}`), và Distributed Lock (`lock:approval_task:{taskId}`).
  - TTL dài hơn (thường từ 15 - 60 phút).

### 6.2. Nguyên tắc Quản trị Tập trung tại Tầng Host (Host-Managed Caching Governance)
- **Ranh giới:** Cấu hình và đăng ký `IDistributedCache` được thực hiện **DUY NHẤT một lần tại tầng Host (`EnterpriseBpm.ApiHost/Program.cs`)**.
- **Không tự đăng ký ở Module con:** Các module con (`AccessGovernance`, `Form`, `Workflow`, `Document`) chỉ được phép inject trừu tượng `IDistributedCache` hoặc `IMemoryCache`, tuyệt đối không tự gọi `AddStackExchangeRedisCache()` hoặc `AddDistributedMemoryCache()` trong `DependencyInjection.cs` của module để tránh ghi đè cấu hình và xung đột tiền tố cache.

### 6.3. Cấu hình Máy chủ Redis Riêng biệt (Dedicated Redis Server & Docker)
Hệ thống hỗ trợ trỏ đến bất kỳ máy chủ Redis độc lập nào (Local Docker, Remote VM, PaaS Redis trên Cloud, Redis Cluster) thông qua `appsettings.json` hoặc Biến môi trường:

```json
{
  "ConnectionStrings": {
    "Redis": "redis-server.internal:6379,password=MySecretPass,abortConnect=false,connectTimeout=5000"
  },
  "Redis": {
    "ConnectionString": "redis-server.internal:6379,password=MySecretPass,abortConnect=false,connectTimeout=5000",
    "InstanceName": "EnterpriseBpm:Prod:"
  }
}
```

- **Hỗ trợ Đa topology:**
  1. **Local Docker Desktop:** `localhost:6379,abortConnect=false`
  2. **Docker Custom Port:** `localhost:6380,abortConnect=false`
  3. **Dedicated Remote VM / IP riêng:** `192.168.1.150:6379,password=***,abortConnect=false,connectTimeout=5000`
  4. **Namespace Isolation (`InstanceName`):** Tự động gắn tiền tố (`EnterpriseBpm:Dev:`, `EnterpriseBpm:Prod:`) cho toàn bộ key cache, cho phép nhiều môi trường hoặc hệ thống dùng chung 1 Redis Server mà không bị ghi đè dữ liệu.
  5. **Graceful Fallback:** Tự động chuyển đổi sang `DistributedMemoryCache` khi không có chuỗi kết nối Redis, giúp phát triển và chạy unit tests offline trơn tru.
