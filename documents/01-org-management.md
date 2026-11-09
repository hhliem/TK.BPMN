# Module 01: OrgManagement (Cơ cấu Tổ chức, Nhân sự, Chức danh, Ủy quyền & Danh bạ Nội bộ)

## 1. Tổng quan & Phạm vi Bounded Context
Module `OrgManagement` chịu trách nhiệm quản trị toàn bộ sơ đồ cây phân cấp doanh nghiệp (Tập đoàn $\rightarrow$ Công ty con $\rightarrow$ Khối $\rightarrow$ Phòng ban $\rightarrow$ Nhóm), quản lý nhân sự, chức danh cấp bậc (Positions), xử lý cơ chế Ủy quyền phê duyệt (Delegation) và cung cấp Danh bạ Nội bộ (Employee Directory) cho toàn tổ chức.

> [!NOTE]
> **Phân định Ranh giới Phân quyền:** Phân hệ quản lý Phân quyền toàn hệ thống (Roles, Permissions, Security Groups, Data Scopes, Approval Limits) được quản trị tập trung tại **Module 11: AccessGovernance (Schema: `[auth]`)**. Module `OrgManagement` đóng vai trò cung cấp dữ liệu danh tính nhân sự, chức danh và cơ cấu tổ chức nguồn cho hệ thống phân quyền.

### Ranh giới kiến trúc (Architectural Boundaries)
- **Database Schema riêng biệt:** `[org]` trên SQL Server.
- **Không phụ thuộc ngược:** Tuyệt đối không tham chiếu DbContext hoặc Entity của các module khác.
- **Giao tiếp ra ngoài:** Cung cấp Public Contracts (`IOrgHierarchyService`, `IDelegationService`, `IEmployeeDirectoryService`) và phát Domain/Integration Events qua MediatR.
- **Tiêu chuẩn Tuân thủ:** Triển khai `IAuditable` và `ISoftDeletable` kế thừa từ `SharedKernel`.

---

## 2. Thiết kế Cơ sở dữ liệu (SQL Server - Schema [org])

```sql
CREATE SCHEMA [org];
GO

-- 1. Bảng Đơn vị Tổ chức (Hierarchical Tree)
CREATE TABLE [org].[OrgUnits] (
    [Id] UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    [ParentId] UNIQUEIDENTIFIER NULL,
    [Code] NVARCHAR(50) NOT NULL UNIQUE,
    [Name] NVARCHAR(255) NOT NULL,
    [UnitType] INT NOT NULL, -- 1: Group, 2: Company, 3: Division, 4: Department, 5: Team
    [DisplayOrder] INT NOT NULL DEFAULT 0,
    [IsActive] BIT NOT NULL DEFAULT 1,
    [CreatedAt] DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    [CreatedBy] UNIQUEIDENTIFIER NULL,
    [UpdatedAt] DATETIME2 NULL,
    [UpdatedBy] UNIQUEIDENTIFIER NULL,
    [DeletedAt] DATETIME2 NULL,           -- Soft Delete
    [DeletedBy] UNIQUEIDENTIFIER NULL,    -- Soft Delete
    CONSTRAINT [FK_OrgUnits_Parent] FOREIGN KEY ([ParentId]) REFERENCES [org].[OrgUnits]([Id])
);
CREATE INDEX [IX_OrgUnits_ParentId] ON [org].[OrgUnits]([ParentId]);
GO

-- 2. Bảng Chức danh (Positions)
CREATE TABLE [org].[Positions] (
    [Id] UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    [Code] NVARCHAR(50) NOT NULL UNIQUE,  -- Ví dụ: "GD", "PGD", "TP", "PP", "NV"
    [Title] NVARCHAR(255) NOT NULL,       -- Ví dụ: "Giám đốc", "Phó Giám đốc", "Trưởng phòng"
    [Level] INT NOT NULL DEFAULT 0,       -- Cấp bậc phục vụ sắp xếp phân quyền
    [IsActive] BIT NOT NULL DEFAULT 1,
    [CreatedAt] DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    [CreatedBy] UNIQUEIDENTIFIER NULL,
    [UpdatedAt] DATETIME2 NULL,
    [UpdatedBy] UNIQUEIDENTIFIER NULL
);
GO

-- 3. Bảng Nhân sự & Liên kết Hệ thống (mở rộng Danh bạ Nội bộ)
CREATE TABLE [org].[Employees] (
    [Id] UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    [UserPrincipalName] NVARCHAR(255) NOT NULL UNIQUE, -- Email đăng nhập O365 / Entra ID
    [EmployeeCode] NVARCHAR(50) NOT NULL UNIQUE,
    [FullName] NVARCHAR(255) NOT NULL,
    [OrgUnitId] UNIQUEIDENTIFIER NOT NULL,
    [PositionId] UNIQUEIDENTIFIER NOT NULL,     -- FK → Positions
    [DirectManagerId] UNIQUEIDENTIFIER NULL,     -- Quản lý trực tiếp
    -- Directory Fields (Danh bạ Nội bộ)
    [PhoneNumber] NVARCHAR(20) NULL,
    [Extension] NVARCHAR(10) NULL,               -- Số máy lẻ nội bộ
    [AvatarUrl] NVARCHAR(500) NULL,
    [JobTitle] NVARCHAR(255) NULL,               -- Chức danh hiển thị (có thể khác Position)
    [WorkLocation] NVARCHAR(255) NULL,           -- Vị trí làm việc (Tòa nhà, Tầng, Phòng)
    [DateOfBirth] DATE NULL,
    [JoinDate] DATE NULL,                        -- Ngày vào công ty
    [IsActive] BIT NOT NULL DEFAULT 1,
    [CreatedAt] DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    [CreatedBy] UNIQUEIDENTIFIER NULL,
    [UpdatedAt] DATETIME2 NULL,
    [UpdatedBy] UNIQUEIDENTIFIER NULL,
    [DeletedAt] DATETIME2 NULL,                  -- Soft Delete
    [DeletedBy] UNIQUEIDENTIFIER NULL,           -- Soft Delete
    CONSTRAINT [FK_Employees_OrgUnit] FOREIGN KEY ([OrgUnitId]) REFERENCES [org].[OrgUnits]([Id]),
    CONSTRAINT [FK_Employees_Position] FOREIGN KEY ([PositionId]) REFERENCES [org].[Positions]([Id]),
    CONSTRAINT [FK_Employees_Manager] FOREIGN KEY ([DirectManagerId]) REFERENCES [org].[Employees]([Id])
);
CREATE INDEX [IX_Employees_OrgUnitId] ON [org].[Employees]([OrgUnitId]);
CREATE INDEX [IX_Employees_DirectManagerId] ON [org].[Employees]([DirectManagerId]);
CREATE INDEX [IX_Employees_PositionId] ON [org].[Employees]([PositionId]);

-- Index phục vụ tìm kiếm Danh bạ Nội bộ (Directory Search)
CREATE INDEX [IX_Employees_DirectorySearch]
ON [org].[Employees]([IsActive], [FullName], [EmployeeCode])
INCLUDE ([PhoneNumber], [Extension], [JobTitle], [WorkLocation], [OrgUnitId], [AvatarUrl]);
GO

-- 4. Bảng Vai trò & Phân quyền (RBAC)
CREATE TABLE [org].[Roles] (
    [Id] UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    [RoleKey] NVARCHAR(100) NOT NULL UNIQUE, -- Ví dụ: "CFO", "IT_DIRECTOR", "LINE_MANAGER"
    [Name] NVARCHAR(255) NOT NULL,
    [Description] NVARCHAR(500) NULL,
    [CreatedAt] DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    [CreatedBy] UNIQUEIDENTIFIER NULL,
    [UpdatedAt] DATETIME2 NULL,
    [UpdatedBy] UNIQUEIDENTIFIER NULL
);

CREATE TABLE [org].[EmployeeRoles] (
    [EmployeeId] UNIQUEIDENTIFIER NOT NULL,
    [RoleId] UNIQUEIDENTIFIER NOT NULL,
    [CreatedAt] DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    [CreatedBy] UNIQUEIDENTIFIER NULL,
    PRIMARY KEY ([EmployeeId], [RoleId]),
    CONSTRAINT [FK_EmployeeRoles_Employee] FOREIGN KEY ([EmployeeId]) REFERENCES [org].[Employees]([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_EmployeeRoles_Role] FOREIGN KEY ([RoleId]) REFERENCES [org].[Roles]([Id]) ON DELETE CASCADE
);
GO

-- 5. Bảng Quản lý Ủy quyền (Delegation Management)
CREATE TABLE [org].[WorkflowDelegations] (
    [Id] UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    [DelegatorId] UNIQUEIDENTIFIER NOT NULL, -- Người ủy quyền (Ví dụ: Giám đốc)
    [DelegateeId] UNIQUEIDENTIFIER NOT NULL, -- Người được ủy quyền (Ví dụ: Phó Giám đốc)
    [FromDate] DATETIME2 NOT NULL,
    [ToDate] DATETIME2 NOT NULL,
    [ScopeFormKey] NVARCHAR(100) NULL, -- NULL: Ủy quyền mọi quy trình; Hoặc chỉ định form cụ thể
    [Reason] NVARCHAR(500) NOT NULL,
    [IsActive] BIT NOT NULL DEFAULT 1,
    [CreatedAt] DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    [CreatedBy] UNIQUEIDENTIFIER NULL,
    [UpdatedAt] DATETIME2 NULL,
    [UpdatedBy] UNIQUEIDENTIFIER NULL,
    CONSTRAINT [FK_Delegations_Delegator] FOREIGN KEY ([DelegatorId]) REFERENCES [org].[Employees]([Id]),
    CONSTRAINT [FK_Delegations_Delegatee] FOREIGN KEY ([DelegateeId]) REFERENCES [org].[Employees]([Id])
);
CREATE INDEX [IX_Delegations_ActiveLookup] 
ON [org].[WorkflowDelegations]([DelegatorId], [IsActive], [FromDate], [ToDate]);
GO
```

---

## 3. Cấu trúc Dự án & Phân tầng Mã nguồn (Clean Architecture)

```
EnterpriseBpm.Org/
├── EnterpriseBpm.Org.Domain/
│   ├── Entities/ (OrgUnit.cs, Employee.cs, Position.cs, Role.cs, WorkflowDelegation.cs)
│   └── Exceptions/ (CircularOrgException.cs, InvalidDelegationException.cs)
├── EnterpriseBpm.Org.Application/
│   ├── Contracts/ (IOrgHierarchyService.cs, IDelegationResolver.cs, IEmployeeDirectoryService.cs)
│   ├── Commands/ (CreateOrgUnitCommand.cs, AssignDelegationCommand.cs, DeactivateEmployeeCommand.cs)
│   └── Queries/ (GetDirectManagerQuery.cs, GetActiveDelegationQuery.cs, SearchEmployeesQuery.cs)
├── EnterpriseBpm.Org.Infrastructure/
│   ├── Persistence/ (OrgDbContext.cs)
│   └── Services/ (OrgHierarchyService.cs, DelegationResolver.cs, EmployeeDirectoryService.cs)
└── EnterpriseBpm.Org.Presentation/
    └── Controllers/ (OrgUnitsController.cs, DelegationsController.cs, EmployeeDirectoryController.cs)
```

---

## 4. Hợp đồng Tích hợp & Sự kiện (Shared Contracts & MediatR)

### 4.1. Public Contract (Dành cho Module Workflow tra cứu)
```csharp
public interface IDelegationResolver
{
    Task<Guid> ResolveActualApproverAsync(
        Guid configuredApproverId, 
        string formKey, 
        CancellationToken cancellationToken);
}

public interface IOrgHierarchyService
{
    Task<Guid?> GetDirectManagerIdAsync(Guid employeeId, CancellationToken cancellationToken);
    Task<IEnumerable<Guid>> GetApproversByRoleAsync(string roleKey, Guid requesterOrgUnitId, CancellationToken cancellationToken);
}
```

### 4.2. Public Contract — Danh bạ Nội bộ (Employee Directory)
```csharp
public interface IEmployeeDirectoryService
{
    /// <summary>
    /// Tìm kiếm nhân sự trong danh bạ theo từ khóa (Họ tên, Mã NV, SĐT, Chức danh).
    /// Hỗ trợ phân trang và lọc theo OrgUnitId.
    /// </summary>
    Task<PagedResult<EmployeeDirectoryDto>> SearchEmployeesAsync(
        string? keyword,
        Guid? orgUnitId,
        int page,
        int pageSize,
        CancellationToken cancellationToken);

    /// <summary>
    /// Lấy thông tin hồ sơ chi tiết của một nhân sự (bao gồm cả thông tin danh bạ).
    /// </summary>
    Task<EmployeeProfileDto?> GetEmployeeProfileAsync(
        Guid employeeId,
        CancellationToken cancellationToken);
}
```

### 4.3. Outgoing Integration Events

```csharp
// Phát khi có Ủy quyền mới
public record DelegationAssignedEvent(
    Guid DelegationId,
    Guid DelegatorId,
    Guid DelegateeId,
    DateTime FromDate,
    DateTime ToDate,
    string? ScopeFormKey
) : BaseIntegrationEvent;

// Phát khi vô hiệu hóa nhân sự (nghỉ việc, chuyển công tác)
public record EmployeeDeactivatedEvent(
    Guid EmployeeId,
    Guid OrgUnitId,
    string EmployeeCode,
    string FullName,
    DateTime DeactivatedAt,
    Guid DeactivatedBy
) : BaseIntegrationEvent;

// Phát khi tái cấu trúc sơ đồ tổ chức (thêm/xóa/di chuyển đơn vị)
public record OrgUnitRestructuredEvent(
    Guid OrgUnitId,
    Guid? OldParentId,
    Guid? NewParentId,
    string OrgUnitCode,
    string ActionType   // "Created", "Moved", "Deactivated"
) : BaseIntegrationEvent;
```

---

## 5. Điểm kiểm soát trọng yếu (Critical Control Points)

1. **Chống Vòng lặp Phân cấp Cây Tổ chức (Anti-Circular Hierarchy):**
   - **Rủi ro:** Khi Admin cập nhật `ParentId` của phòng ban $A$ trỏ về đơn vị con $B$ của chính nó, cây tổ chức bị lặp vô tận, gây sập thuật toán duyệt đệ quy.
   - **Kiểm soát:** Trong `CreateOrgUnitCommand` / `UpdateOrgUnitCommand`, bắt buộc chạy thuật toán kiểm tra đệ quy ngược (Ancestry Traversal). Nếu `ParentId` mới nằm trong tập con cháu của nút hiện tại, lập tức ném lỗi `CircularOrgException`.
2. **Chống Ủy quyền Chéo / Vòng tròn (Anti-Circular Delegation):**
   - **Rủi ro:** User $A$ ủy quyền cho User $B$, sau đó User $B$ lại ủy quyền cho User $A$ trong cùng khoảng thời gian $\rightarrow$ luồng duyệt bị kẹt lặp.
   - **Kiểm soát:** Kiểm tra trước khi lưu: Nếu tồn tại bản ghi ủy quyền đang hiệu lực theo chiều ngược lại ($B \rightarrow A$) trong khoảng thời gian `[FromDate, ToDate]`, từ chối tạo mới.
3. **Audit Trail Phê duyệt Ký thay:**
   - **Rủi ro:** Người được ủy quyền ký duyệt nhưng sau này phát sinh tranh chấp pháp lý nội bộ.
   - **Kiểm soát:** Khi `IDelegationResolver` chuyển quyền từ Delegator sang Delegatee, hệ thống bắt buộc phải đính kèm Metadata: `"ApprovedByUserId": DelegateeId`, `"OnBehalfOfUserId": DelegatorId`, `"DelegationId": ID`.
4. **Toàn vẹn dữ liệu khi Vô hiệu hóa Nhân sự (Soft Delete Integrity):**
   - **Rủi ro:** Nhân sự bị xóa cứng (DELETE) dẫn đến mất dữ liệu lịch sử phê duyệt, FK bị gãy trong các bảng Workflow/Approval.
   - **Kiểm soát:** Nhân sự chỉ được Soft Delete (gán `DeletedAt`, `DeletedBy`). Khi deactivate nhân sự, hệ thống tự động: (a) thu hồi mọi Delegation đang hiệu lực, (b) phát `EmployeeDeactivatedEvent` để Module Workflow hủy/chuyển giao các task pending.
5. **Bảo mật Danh bạ Nội bộ (Directory Access Control):**
   - **Rủi ro:** Rò rỉ thông tin cá nhân nhân sự (SĐT, ngày sinh) cho người ngoài tổ chức.
   - **Kiểm soát:** API Danh bạ Nội bộ yêu cầu xác thực JWT và chỉ trả về thông tin cho nhân sự thuộc cùng tổ chức. Thông tin nhạy cảm (`DateOfBirth`) chỉ hiển thị cho HR Admins hoặc chính nhân sự đó.
