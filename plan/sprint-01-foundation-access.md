# 🚀 Sprint 01: Core Architecture, Shared Kernel, Access Governance & Audit

---

## 1. Thông tin Chung về Sprint 01

- **Tên Sprint:** Sprint 01 — Core Foundation, Access Governance & Audit Logging
- **Thời gian Thực hiện:** Tuần 1 - Tuần 2 (10 ngày làm việc)
- **Mục tiêu Sprint (Sprint Goal):**
  > Thiết lập toàn bộ khung kiến trúc Modular Monolith chuẩn Clean Architecture, hoàn thiện SharedKernel (`ISoftDeletable`, `IAuditable`, `BaseIntegrationEvent`), xây dựng phân hệ Quản trị Phân quyền toàn hệ thống `AccessGovernance` (RBAC/ABAC, Quyền hạt nhân, Hạn mức phê duyệt) và cơ chế Kiểm toán tự động `AuditLog` với Trigger bất biến.
- **Tổng Story Points cam kết:** **68 SP**
- **Tổng Giờ Kỹ thuật khả dụng (Capacity):** **340 Giờ** (Team 5 Dev + 1 QA)

---

## 2. Sprint Backlog & Acceptance Criteria (Gherkin)

### US-01: Khung Giải pháp Modular Monolith & SharedKernel (8 SP)
- **Acceptance Criteria:**
  ```gherkin
  Scenario: Kiểm tra phân tầng Clean Architecture và SharedKernel
    Given một project mới thuộc Module OrgManagement
    When project này chỉ tham chiếu đến EnterpriseBpm.SharedKernel
    Then project phải biên dịch thành công mà không phụ thuộc vào Infrastructure của module khác
    And bài test NetArchTest kiểm tra ranh giới kiến trúc phải Pass 100%.
  ```

### US-02 & US-03: ISoftDeletable & IAuditable Interceptors (10 SP)
- **Acceptance Criteria:**
  ```gherkin
  Scenario: Tự động ghi nhận thông tin kiểm toán khi SaveChanges
    Given một Entity kế thừa IAuditable và ISoftDeletable
    When người dùng thực hiện thao tác Thêm mới hoặc Cập nhật Entity
    Then SaveChangesInterceptor tự động gán CreatedAt, CreatedBy hoặc UpdatedAt, UpdatedBy từ ICurrentUserService
    And khi xóa Entity, DeletedAt được gán thời gian hiện tại và Entity bị ẩn khỏi truy vấn mặc định.
  ```

### US-04 & US-05: Tự động Ghi Log Kiểm toán & Trigger Bất biến (18 SP)
- **Acceptance Criteria:**
  ```gherkin
  Scenario: Chặn sửa đổi hoặc xóa bản ghi trong bảng AuditEntries
    Given một bản ghi nhật ký kiểm toán đã được lưu trong bảng [audit].[AuditEntries]
    When bất kỳ người dùng hoặc script nào thực hiện lệnh UPDATE hoặc DELETE trên bản ghi đó
    Then Database Trigger TR_AuditEntries_PreventModification phải chặn lại và trả lỗi "Audit entries cannot be modified or deleted".
  ```

### US-07, US-08 & US-10: Quản trị Phân quyền Toàn hệ thống & ABAC Data Scope (24 SP)
- **Acceptance Criteria:**
  ```gherkin
  Scenario: Đánh giá quyền hạn người dùng qua API Policy
    Given người dùng có vai trò "LINE_MANAGER" sở hữu quyền "workflow:task:approve"
    When người dùng gọi API có gắn thuộc tính [RequirePermission("workflow:task:approve")]
    Then hệ thống cho phép truy cập với thời gian phản hồi đánh giá quyền < 2ms qua Redis
    And dữ liệu trả về được tự động lọc theo phạm vi Scope.Department của người dùng đó.
  ```

### US-11 & US-14: Hạn mức Phê duyệt & Hủy Cache Quyền Tức thì (8 SP)
- **Acceptance Criteria:**
  ```gherkin
  Scenario: Hủy cache Redis tức thời khi quyền hạn bị thay đổi
    Given người dùng đang có quyền trong cache Redis
    When Quản trị viên cập nhật thu hồi quyền của vai trò người dùng đó
    Then toàn bộ cache "auth:user_perms:{userId}" tương ứng bị xóa ngay lập tức
    And request tiếp theo của người dùng bị từ chối truy cập (HTTP 403 Forbidden).
  ```

---

## 3. Danh mục Task Kỹ thuật Chi tiết (Tasks Breakdown)

| Mã Task | Module | Hạng mục Kỹ thuật | Mô tả Chi tiết Công việc Cần Code | Giờ (h) | Người Phụ trách |
|:---:|:---:|---|---|:---:|:---:|
| **TSK-101** | Kernel | Solution Scaffolding | Khởi tạo cấu trúc Solution .NET Core, thư mục `Shared/`, `Modules/`, `Host/`, cấu hình EditorConfig và Directory.Build.props. | 8 | Tech Lead |
| **TSK-102** | Kernel | SharedKernel Contracts | Viết `BaseEntity`, `Result<T>`, `GuardClauses`, `ISoftDeletable`, `IAuditable`, `BaseIntegrationEvent`. | 8 | Backend Dev 1 |
| **TSK-103** | Kernel | Auditable Interceptor | Triển khai `AuditableFieldsInterceptor` tự động gán ngày giờ và User ID vào các Entity. | 6 | Backend Dev 1 |
| **TSK-104** | Kernel | Global Query Filter | Cấu hình biểu thức Lambda Expression tự động áp dụng `DeletedAt == null` cho toàn bộ Entity thuộc `ISoftDeletable`. | 6 | Backend Dev 1 |
| **TSK-105** | Kernel | NetArchTest Suite | Thiết lập project `EnterpriseBpm.ArchTests` với các unit test kiểm tra ranh giới dependency giữa các module. | 8 | QA / Tech Lead |
| **TSK-106** | Audit | Database Schema [audit] | Viết script Migration tạo bảng `[audit].[AuditEntries]`, `[audit].[SecurityEvents]` kèm Clustered & Non-clustered Index. | 6 | Backend Dev 2 |
| **TSK-107** | Audit | Immutable Trigger | Viết Database Trigger `TR_AuditEntries_PreventModification` cấm hoàn toàn lệnh UPDATE/DELETE trên bảng AuditEntries. | 4 | Backend Dev 2 |
| **TSK-108** | Audit | AuditSaveChangesInterceptor | Viết EF Core Interceptor tự động bắt ChangeTracker để lấy OldValues/NewValues dạng JSON serialize. | 12 | Backend Dev 2 |
| **TSK-109** | Audit | Audit Query & Export API | Viết API tra cứu lịch sử audit theo Entity, User và xuất Excel qua thư viện ClosedXML. | 10 | Backend Dev 2 |
| **TSK-110** | Auth | Database Schema [auth] | Viết script Migration tạo 9 bảng thuộc schema `[auth]` (Permissions, Roles, RolePermissions, SecurityGroups, DataScopePolicies, ApprovalLimits, FormPermissions, MenuPermissions). | 12 | Backend Dev 3 |
| **TSK-111** | Auth | Seed Data Permissions | Viết seeder nạp danh mục quyền hạt nhân mặc định cho toàn bộ các module trong hệ thống. | 8 | Backend Dev 3 |
| **TSK-112** | Auth | Custom Policy Provider | Triển khai `IAuthorizationPolicyProvider` và `PermissionAuthorizationHandler` cho ASP.NET Core. | 10 | Backend Dev 3 |
| **TSK-113** | Auth | Cached Permission Evaluator | Triển khai `IPermissionEvaluator` với cơ chế cache 2 tầng: MemoryCache (L1) + StackExchange.Redis (L2). | 12 | Backend Dev 1 |
| **TSK-114** | Auth | ABAC Data Scope Evaluator | Triển khai `IDataScopeEvaluator` đánh giá phạm vi dữ liệu (`Self`, `Department`, `Division`, `Global`). | 12 | Backend Dev 1 |
| **TSK-115** | Auth | Approval Limit Checker | Triển khai `IApprovalLimitChecker` thẩm định hạn mức tài chính trước khi cho phép ký duyệt. | 8 | Backend Dev 2 |
| **TSK-116** | Auth | Instant Cache Eviction | Viết MediatR Handler xóa cache Redis key pattern `auth:user_perms:*` khi vai trò bị thay đổi. | 6 | Backend Dev 1 |
| **TSK-117** | UI | AdminLTE Layout & Navigation | Dựng khung layout AdminLTE 3 / SPA, cấu hình dynamic sidebar menu nạp theo quyền từ API. | 16 | Frontend Dev 1 |
| **TSK-118** | UI | Role-Permission Matrix UI | Dựng màn hình giao diện ma trận phân quyền (Tree view danh mục quyền, checkbox gán quyền cho vai trò). | 20 | Frontend Dev 2 |
| **TSK-119** | UI | Security Groups & Users UI | Dựng giao diện quản lý nhóm bảo mật, gán người dùng vào nhóm và gán vai trò. | 14 | Frontend Dev 2 |
| **TSK-120** | UI | Approval Limits UI | Dựng giao diện cấu hình hạn mức phê duyệt tài chính theo chức danh và loại đơn. | 12 | Frontend Dev 1 |
| **TSK-121** | QA | Unit Test SharedKernel & Interceptors | Viết 30 Unit Tests kiểm tra tính chính xác của Interceptors, Result Pattern và Guard Clauses. | 16 | QA Engineer |
| **TSK-122** | QA | Integration Test Auth & Audit | Viết 25 Integration Tests kiểm tra phân quyền RBAC/ABAC, chặn truy cập trái phép và kiểm tra ghi nhận audit log. | 20 | QA Engineer |
| **TỔNG** | | | **Tổng số 22 Tasks Kỹ thuật** | **234h** | |

---

## 4. Kế hoạch Đánh giá & Tiêu chí Release Sprint 01

### 4.1. Sprint Review / Demo Checklist
- [ ] **Demo 1:** Chạy test `NetArchTest` chứng minh Module Workflow cố tình trỏ sang Infrastructure của Org sẽ bị fail build.
- [ ] **Demo 2:** Thực hiện Thêm / Sửa / Xóa một bản ghi thử nghiệm $\rightarrow$ Bảng `[audit].[AuditEntries]` tự động ghi log với JSON OldValues và NewValues chính xác.
- [ ] **Demo 3:** Thử chạy lệnh `DELETE FROM [audit].[AuditEntries]` trên SQL Server Management Studio $\rightarrow$ Bị chặn bởi Database Trigger.
- [ ] **Demo 4:** Đăng nhập tài khoản User thường $\rightarrow$ Menu Quản trị bị ẩn, gọi API trực tiếp qua Swagger bị trả về HTTP 403 Forbidden.
- [ ] **Demo 5:** Quản trị viên tích chọn cấp quyền trên Giao diện Ma trận Phân quyền $\rightarrow$ User F5 trang là thấy menu và gọi được API ngay lập tức nhờ Cache Eviction.

### 4.2. Đánh giá Rủi ro Sprint 01
| Rủi ro Kỹ thuật | Mức độ | Biện pháp Phòng ngừa |
|---|:---:|---|
| Cache Redis bị trễ làm người dùng vẫn giữ quyền cũ | Trung bình | Bắt buộc test kỹ thuật Redis Key Eviction đồng bộ ngay trong transaction đổi quyền. |
| ChangeTracker trong AuditSaveChangesInterceptor làm chậm hiệu năng | Cao | Chỉ chụp snapshot các entity quan trọng, dùng AsNoTracking cho các truy vấn đọc. |

