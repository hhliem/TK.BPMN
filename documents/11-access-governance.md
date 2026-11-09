# Module 11: AccessGovernance (Quản trị Phân quyền & Kiểm soát Truy cập Toàn hệ thống)

## 1. Tổng quan & Phạm vi Bounded Context
Module `AccessGovernance` là phân hệ bảo mật hạt nhân chịu trách nhiệm quản trị danh tính, vai trò và phân quyền tập trung cho toàn bộ nền tảng Enterprise BPMN Platform. Phân hệ kết hợp mô hình lai **RBAC (Role-Based Access Control)** và **ABAC (Attribute-Based Access Control)** để giải quyết trọn vẹn bài toán kiểm soát truy cập phức tạp trong các tập đoàn đa chi nhánh:

1. **Ma trận Phân quyền Hạt nhân (Fine-Grained Permissions):** Quản lý danh mục quyền chi tiết đến từng tài nguyên và hành động (`resource:action`, ví dụ: `workflow:submission:create`, `workflow:task:approve`, `document:file:download`, `analytics:report:export`).
2. **Vai trò Đa tầng (Multi-Tier Roles & Security Groups):** Hỗ trợ Vai trò Hệ thống (System Roles), Vai trò Nghiệp vụ (Business Roles), và Nhóm Người dùng (Security Groups). Một nhân sự có thể có nhiều vai trò linh hoạt.
3. **Kiểm soát Phạm vi Dữ liệu (ABAC Data Scoping):** Kiểm soát dữ liệu mà người dùng được phép nhìn thấy dựa trên thuộc tính ngữ cảnh:
   - `Scope.Self`: Chỉ xem dữ liệu do chính mình tạo ra.
   - `Scope.Department`: Xem toàn bộ dữ liệu trong phòng ban/đơn vị trực thuộc.
   - `Scope.Division`: Xem dữ liệu khối/chi nhánh cấp trên.
   - `Scope.Global`: Xem toàn bộ dữ liệu tập đoàn.
4. **Hạn mức Phê duyệt Nghiệp vụ (Approval Limit Governance):** Phân quyền dựa trên hạn mức tài chính/nghiệp vụ (ví dụ: Trưởng phòng duyệt chi $\le 50\text{ triệu}$, Giám đốc chi nhánh $\le 500\text{ triệu}$, Tổng Giám đốc $> 500\text{ triệu}$).
5. **Phân quyền Biểu mẫu & Quy trình (Form & Workflow Authorization Matrix):** Cấu hình động ai được phép khởi tạo loại đơn nào, ai có quyền xem, ai có quyền can thiệp hủy/điều chỉnh quy trình.
6. **Phân quyền Giao diện & Menu (UI Navigation & Action Control):** Đồng bộ cây phân quyền xuống frontend AdminLTE để ẩn/hiện menu, nút bấm (Approve, Reject, Export) theo thời gian thực.

### Ranh giới kiến trúc (Architectural Boundaries)
- **Database Schema riêng biệt:** `[auth]` trên SQL Server.
- **Tầng Cache:** Tích hợp Redis Distributed Cache lưu trữ Token Permissions và Permission Sets của User (TTL: 15-30 phút), làm mới ngay lập tức (Cache Eviction) khi có thay đổi quyền.
- **Giao tiếp Đồng bộ:** Cung cấp Public Contracts (`IPermissionEvaluator`, `IDataScopeEvaluator`, `IApprovalLimitChecker`) để tất cả các module khác tra cứu thẩm quyền trong thời gian $< 2\text{ ms}$.
- **Giao tiếp Sự kiện:** Lắng nghe `EmployeeDeactivatedEvent` từ OrgManagement để hủy toàn bộ phiên làm việc và thu hồi quyền truy cập tức thì.

---

## 2. Thiết kế Cơ sở dữ liệu (SQL Server - Schema [auth])

```sql
CREATE SCHEMA [auth];
GO

-- 1. Bảng Danh mục Quyền Hạt nhân Toàn hệ thống (System Permissions)
CREATE TABLE [auth].[Permissions] (
    [Id] UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    [PermissionCode] NVARCHAR(100) NOT NULL UNIQUE, -- "workflow:submission:create", "report:kpi:view"
    [ModuleName] NVARCHAR(50) NOT NULL,              -- "WorkflowEngine", "Analytics", "DocumentManagement"
    [Resource] NVARCHAR(50) NOT NULL,                -- "Submission", "Report", "Setting"
    [Action] NVARCHAR(50) NOT NULL,                  -- "Create", "Read", "Update", "Delete", "Approve", "Export"
    [Name] NVARCHAR(255) NOT NULL,
    [Description] NVARCHAR(500) NULL,
    [IsSystem] BIT NOT NULL DEFAULT 1                -- Quyền mặc định hệ thống không được xóa
);
CREATE INDEX [IX_Permissions_ModuleResource] 
ON [auth].[Permissions]([ModuleName], [Resource], [Action]);
GO

-- 2. Bảng Vai trò (Roles)
CREATE TABLE [auth].[Roles] (
    [Id] UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    [RoleCode] NVARCHAR(100) NOT NULL UNIQUE, -- "SUPER_ADMIN", "CFO", "DIVISION_DIRECTOR", "LINE_MANAGER", "STAFF"
    [Name] NVARCHAR(255) NOT NULL,
    [RoleType] NVARCHAR(50) NOT NULL DEFAULT 'Business', -- System, Business, Custom
    [Description] NVARCHAR(500) NULL,
    [IsActive] BIT NOT NULL DEFAULT 1,
    [CreatedAt] DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    [CreatedBy] UNIQUEIDENTIFIER NULL,
    [UpdatedAt] DATETIME2 NULL,
    [UpdatedBy] UNIQUEIDENTIFIER NULL
);
GO

-- 3. Bảng Ma trận Vai trò - Quyền (Role-Permission Matrix)
CREATE TABLE [auth].[RolePermissions] (
    [RoleId] UNIQUEIDENTIFIER NOT NULL,
    [PermissionId] UNIQUEIDENTIFIER NOT NULL,
    PRIMARY KEY ([RoleId], [PermissionId]),
    CONSTRAINT [FK_RolePermissions_Role] FOREIGN KEY ([RoleId]) 
        REFERENCES [auth].[Roles]([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_RolePermissions_Permission] FOREIGN KEY ([PermissionId]) 
        REFERENCES [auth].[Permissions]([Id]) ON DELETE CASCADE
);
GO

-- 4. Bảng Nhóm Người dùng Bảo mật (Security Groups)
CREATE TABLE [auth].[SecurityGroups] (
    [Id] UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    [GroupCode] NVARCHAR(100) NOT NULL UNIQUE, -- "GRP_FINANCE_APPROVERS", "GRP_BOARD_MEMBERS"
    [Name] NVARCHAR(255) NOT NULL,
    [Description] NVARCHAR(500) NULL,
    [IsActive] BIT NOT NULL DEFAULT 1,
    [CreatedAt] DATETIME2 NOT NULL DEFAULT GETUTCDATE()
);

CREATE TABLE [auth].[SecurityGroupRoles] (
    [GroupId] UNIQUEIDENTIFIER NOT NULL,
    [RoleId] UNIQUEIDENTIFIER NOT NULL,
    PRIMARY KEY ([GroupId], [RoleId]),
    CONSTRAINT [FK_SGRoles_Group] FOREIGN KEY ([GroupId]) 
        REFERENCES [auth].[SecurityGroups]([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_SGRoles_Role] FOREIGN KEY ([RoleId]) 
        REFERENCES [auth].[Roles]([Id]) ON DELETE CASCADE
);

CREATE TABLE [auth].[SecurityGroupMembers] (
    [GroupId] UNIQUEIDENTIFIER NOT NULL,
    [UserId] UNIQUEIDENTIFIER NOT NULL,
    [AssignedAt] DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    PRIMARY KEY ([GroupId], [UserId]),
    CONSTRAINT [FK_SGMembers_Group] FOREIGN KEY ([GroupId]) 
        REFERENCES [auth].[SecurityGroups]([Id]) ON DELETE CASCADE
);
CREATE INDEX [IX_SecurityGroupMembers_User] ON [auth].[SecurityGroupMembers]([UserId]);
GO

-- 5. Bảng Gán Vai trò Trực tiếp cho Người dùng (User Roles)
CREATE TABLE [auth].[UserRoles] (
    [UserId] UNIQUEIDENTIFIER NOT NULL,
    [RoleId] UNIQUEIDENTIFIER NOT NULL,
    [AssignedAt] DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    [AssignedBy] UNIQUEIDENTIFIER NULL,
    PRIMARY KEY ([UserId], [RoleId]),
    CONSTRAINT [FK_UserRoles_Role] FOREIGN KEY ([RoleId]) 
        REFERENCES [auth].[Roles]([Id]) ON DELETE CASCADE
);
CREATE INDEX [IX_UserRoles_User] ON [auth].[UserRoles]([UserId]);
GO

-- 6. Bảng Chính sách Phạm vi Dữ liệu (ABAC Data Scope Policies)
CREATE TABLE [auth].[DataScopePolicies] (
    [Id] UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    [RoleId] UNIQUEIDENTIFIER NOT NULL,
    [Resource] NVARCHAR(50) NOT NULL, -- "WorkflowSubmission", "AnalyticsDashboard", "EmployeeProfile"
    [ScopeType] NVARCHAR(50) NOT NULL, -- "Self", "Department", "Division", "Global", "CustomOrgFilter"
    [CustomFilterExpression] NVARCHAR(MAX) NULL, -- Biểu thức lọc JSON động nếu là Custom
    CONSTRAINT [FK_DataScopePolicies_Role] FOREIGN KEY ([RoleId]) 
        REFERENCES [auth].[Roles]([Id]) ON DELETE CASCADE
);
CREATE INDEX [IX_DataScopePolicies_RoleResource] 
ON [auth].[DataScopePolicies]([RoleId], [Resource]);
GO

-- 7. Bảng Quản trị Hạn mức Phê duyệt (Approval Limits)
CREATE TABLE [auth].[ApprovalLimits] (
    [Id] UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    [PositionId] UNIQUEIDENTIFIER NULL,       -- Tham chiếu Chức danh (Trưởng phòng, GĐ)
    [RoleId] UNIQUEIDENTIFIER NULL,           -- Hoặc tham chiếu Role cụ thể
    [FormKey] NVARCHAR(100) NOT NULL,         -- Áp dụng cho loại đơn (ví dụ: payment_request) hoặc "*" cho tất cả
    [Currency] NVARCHAR(10) NOT NULL DEFAULT 'VND',
    [MaxLimitAmount] DECIMAL(18, 2) NOT NULL, -- Hạn mức tối đa được phép ký duyệt
    [IsActive] BIT NOT NULL DEFAULT 1,
    [CreatedAt] DATETIME2 NOT NULL DEFAULT GETUTCDATE()
);
CREATE INDEX [IX_ApprovalLimits_Lookup] 
ON [auth].[ApprovalLimits]([FormKey], [PositionId], [RoleId]);
GO

-- 8. Bảng Phân quyền Khởi tạo & Xem Form Trình ký (Form Workflow Permissions)
CREATE TABLE [auth].[FormPermissions] (
    [Id] UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    [FormKey] NVARCHAR(100) NOT NULL,
    [SubjectType] NVARCHAR(20) NOT NULL, -- "Role", "Group", "OrgUnit", "User"
    [SubjectId] UNIQUEIDENTIFIER NOT NULL,
    [CanInitiate] BIT NOT NULL DEFAULT 1, -- Được phép tạo đơn
    [CanViewAll] BIT NOT NULL DEFAULT 0,   -- Được xem mọi đơn loại này trong đơn vị
    [CanExport] BIT NOT NULL DEFAULT 0,    -- Được xuất dữ liệu ra Excel
    [CreatedAt] DATETIME2 NOT NULL DEFAULT GETUTCDATE()
);
CREATE INDEX [IX_FormPermissions_Lookup] 
ON [auth].[FormPermissions]([FormKey], [SubjectType], [SubjectId]);
GO

-- 9. Bảng Phân quyền Cây Menu Giao diện (UI Menu Permissions)
CREATE TABLE [auth].[MenuPermissions] (
    [Id] UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    [MenuKey] NVARCHAR(100) NOT NULL, -- "menu_dashboard", "menu_workflows", "menu_reports"
    [ParentMenuKey] NVARCHAR(100) NULL,
    [Title] NVARCHAR(255) NOT NULL,
    [RouteUrl] NVARCHAR(500) NOT NULL,
    [IconClass] NVARCHAR(100) NULL,
    [RequiredPermissionCode] NVARCHAR(100) NULL, -- Quyền tối thiểu để menu hiển thị
    [DisplayOrder] INT NOT NULL DEFAULT 0,
    [IsActive] BIT NOT NULL DEFAULT 1
);
GO
```

---

## 3. Triển khai Kỹ thuật & Public Contracts

### 3.1. Public Contracts dành cho toàn hệ thống
```csharp
namespace EnterpriseBpm.AccessGovernance.Contracts;

public interface IPermissionEvaluator
{
    /// <summary>
    /// Kiểm tra người dùng có sở hữu quyền thao tác cụ thể hay không (< 2ms qua Redis/Memory cache)
    /// </summary>
    Task<bool> HasPermissionAsync(Guid userId, string permissionCode, CancellationToken cancellationToken = default);

    /// <summary>
    /// Lấy toàn bộ danh sách quyền của người dùng (tổng hợp từ UserRoles và SecurityGroups)
    /// </summary>
    Task<HashSet<string>> GetUserPermissionsAsync(Guid userId, CancellationToken cancellationToken = default);
}

public interface IDataScopeEvaluator
{
    /// <summary>
    /// Đánh giá phạm vi dữ liệu người dùng được phép truy vấn đối với một tài nguyên cụ thể
    /// </summary>
    Task<DataScopeResult> EvaluateScopeAsync(Guid userId, string resourceName, CancellationToken cancellationToken = default);
}

public interface IApprovalLimitChecker
{
    /// <summary>
    /// Kiểm tra người dùng có đủ hạn mức tài chính/nghiệp vụ để duyệt đơn hiện tại hay không
    /// </summary>
    Task<ApprovalLimitResult> CanApproveAmountAsync(
        Guid userId, 
        string formKey, 
        decimal amount, 
        string currency = "VND", 
        CancellationToken cancellationToken = default);
}

public record DataScopeResult(
    string ScopeType, // "Self", "Department", "Division", "Global"
    Guid? OrgUnitId,
    IReadOnlyList<Guid> AccessibleOrgUnitIds
);

public record ApprovalLimitResult(
    bool IsAllowed, 
    decimal MaxAllowedLimit, 
    string? ReasonMessage
);
```

### 3.2. Triển khai ASP.NET Core Custom Authorization Policy
```csharp
namespace EnterpriseBpm.AccessGovernance.Infrastructure.Authorization;

// Attribute đánh dấu API Controller / Action yêu cầu Permission
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = true)]
public class RequirePermissionAttribute : AuthorizeAttribute
{
    public RequirePermissionAttribute(string permissionCode) : base(policy: permissionCode)
    {
    }
}

// Authorization Policy Provider tự động sinh Policy theo PermissionCode
public class PermissionPolicyProvider : IAuthorizationPolicyProvider
{
    private readonly DefaultAuthorizationPolicyProvider _fallbackPolicyProvider;

    public PermissionPolicyProvider(IOptions<AuthorizationOptions> options)
    {
        _fallbackPolicyProvider = new DefaultAuthorizationPolicyProvider(options);
    }

    public Task<AuthorizationPolicy?> GetPolicyAsync(string policyName)
    {
        // Nếu policy chứa ký tự dấu hai chấm (VD: "workflow:task:approve") -> Xử lý bằng Permission Requirement
        if (policyName.Contains(':'))
        {
            var policy = new AuthorizationPolicyBuilder();
            policy.AddRequirements(new PermissionRequirement(policyName));
            return Task.FromResult<AuthorizationPolicy?>(policy.Build());
        }

        return _fallbackPolicyProvider.GetPolicyAsync(policyName);
    }

    public Task<AuthorizationPolicy> GetDefaultPolicyAsync() => _fallbackPolicyProvider.GetDefaultPolicyAsync();
    public Task<AuthorizationPolicy?> GetFallbackPolicyAsync() => _fallbackPolicyProvider.GetFallbackPolicyAsync();
}

// Authorization Handler kiểm tra quyền người dùng
public class PermissionAuthorizationHandler : AuthorizationHandler<PermissionRequirement>
{
    private readonly IPermissionEvaluator _evaluator;

    public PermissionAuthorizationHandler(IPermissionEvaluator evaluator)
    {
        _evaluator = evaluator;
    }

    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context, 
        PermissionRequirement requirement)
    {
        var userIdClaim = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!Guid.TryParse(userIdClaim, out var userId))
        {
            return;
        }

        var hasPermission = await _evaluator.HasPermissionAsync(userId, requirement.PermissionCode);
        if (hasPermission)
        {
            context.Succeed(requirement);
        }
    }
}
```

---

## 4. API Quản trị Phân quyền (Access Governance Portal)

```csharp
namespace EnterpriseBpm.AccessGovernance.Presentation.Controllers;

[ApiController]
[Route("api/auth/governance")]
[RequirePermission("auth:governance:manage")]
public class AccessGovernanceController : ControllerBase
{
    private readonly IAccessGovernanceService _governanceService;

    public AccessGovernanceController(IAccessGovernanceService governanceService)
    {
        _governanceService = governanceService;
    }

    [HttpGet("matrix/role/{roleId}")]
    public async Task<IActionResult> GetRolePermissionMatrix(Guid roleId, CancellationToken cancellationToken)
    {
        var matrix = await _governanceService.GetRolePermissionsAsync(roleId, cancellationToken);
        return Ok(matrix);
    }

    [HttpPost("matrix/role/{roleId}/update")]
    public async Task<IActionResult> UpdateRolePermissions(
        Guid roleId, 
        [FromBody] UpdateRolePermissionsDto dto, 
        CancellationToken cancellationToken)
    {
        await _governanceService.UpdateRolePermissionsAsync(roleId, dto.PermissionIds, cancellationToken);
        return NoContent();
    }

    [HttpGet("users/{userId}/effective-permissions")]
    public async Task<IActionResult> GetEffectiveUserPermissions(Guid userId, CancellationToken cancellationToken)
    {
        var permissions = await _governanceService.GetEffectivePermissionsAsync(userId, cancellationToken);
        return Ok(permissions);
    }

    [HttpPost("approval-limits")]
    public async Task<IActionResult> ConfigureApprovalLimit(
        [FromBody] CreateApprovalLimitCommand command, 
        CancellationToken cancellationToken)
    {
        var limitId = await _governanceService.CreateApprovalLimitAsync(command, cancellationToken);
        return CreatedAtAction(nameof(ConfigureApprovalLimit), new { id = limitId }, limitId);
    }

    [HttpGet("menu/user-navigation")]
    public async Task<IActionResult> GetAuthorizedNavigationMenu(CancellationToken cancellationToken)
    {
        var currentUserId = User.GetUserId();
        var authorizedMenu = await _governanceService.GetUserAuthorizedMenuAsync(currentUserId, cancellationToken);
        return Ok(authorizedMenu);
    }
}
```

---

## 5. Cấu trúc Dự án & Phân tầng Mã nguồn (Clean Architecture)

```
EnterpriseBpm.AccessGovernance/
├── EnterpriseBpm.AccessGovernance.Domain/
│   ├── Entities/ (Permission.cs, Role.cs, SecurityGroup.cs, DataScopePolicy.cs, ApprovalLimit.cs, FormPermission.cs)
│   └── Exceptions/ (ApprovalLimitExceededException.cs, UnauthorizedResourceAccessException.cs)
├── EnterpriseBpm.AccessGovernance.Application/
│   ├── Contracts/ (IPermissionEvaluator.cs, IDataScopeEvaluator.cs, IApprovalLimitChecker.cs)
│   ├── Commands/ (AssignUserRoleCommand.cs, UpdateRolePermissionMatrixCommand.cs, SetApprovalLimitCommand.cs)
│   └── Queries/ (GetEffectivePermissionsQuery.cs, GetUserNavigationMenuQuery.cs)
├── EnterpriseBpm.AccessGovernance.Infrastructure/
│   ├── Persistence/ (AuthDbContext.cs)
│   ├── Cache/ (CachedPermissionEvaluator.cs)
│   └── Authorization/ (PermissionPolicyProvider.cs, PermissionAuthorizationHandler.cs)
└── EnterpriseBpm.AccessGovernance.Presentation/
    └── Controllers/ (AccessGovernanceController.cs, RolesController.cs, SecurityGroupsController.cs)
```

---

## 6. Điểm kiểm soát trọng yếu (Critical Control Points)

1. **Bảo vệ Ma trận Phân quyền trước Quyền Tối cao (SuperAdmin Lockdown):**
   - **Rủi ro:** Một Admin cấp dưới tự gán quyền `auth:governance:manage` cho chính mình hoặc nâng vai trò lên thành `SuperAdmin`.
   - **Kiểm soát:** Chỉ duy nhất tài khoản gốc sở hữu `SuperAdmin` cố định (được bảo vệ trong cấu hình mã hóa) mới được phép phân quyền cho nhóm vai trò Quản trị hệ thống. Mọi thao tác gán vai trò `SuperAdmin` hay `CFO` đều bị bắt buộc xác thực 2 lớp (MFA) và tự động phát `AuditableActionEvent` sang Module 09: AuditLog.

2. **Cơ chế Hủy Cache Quyền Tức thì (Instant Permission Cache Eviction):**
   - **Rủi ro:** Khi Admin thu hồi quyền của một nhân viên nghi vấn có hành vi gian lận, nếu cache không bị xóa ngay, nhân viên đó vẫn có thể thực hiện duyệt hoặc tải tài liệu nhạy cảm trong suốt thời gian TTL còn lại.
   - **Kiểm soát:** Ngay khi lệnh cập nhật Role, Permission hoặc Security Group thành công, hệ thống lập tức phát lệnh xóa toàn bộ Redis key dạng `auth:user_perms:{userId}` của tất cả các User bị ảnh hưởng, buộc request kế tiếp phải đánh giá lại quyền.

3. **Thẩm định Kép Hạn mức Phê duyệt (Two-Tier Limit Validation):**
   - **Rủi ro:** Form nộp đơn có trường tiền gửi lên Client bị DevTools sửa lại (ví dụ đơn 1 tỷ sửa thành 10 triệu để qua mặt người duyệt hạn mức thấp).
   - **Kiểm soát:** Khi người duyệt bấm nút Duyệt, Module WorkflowEngine bắt buộc gọi sang `IApprovalLimitChecker` kiểm tra lại giá trị tiền thực tế được truy vấn trực tiếp từ bảng dữ liệu gốc, đối soát với `[auth].[ApprovalLimits]`. Nếu vượt hạn mức, hệ thống từ chối cho phép hoàn thành Bookmark và yêu cầu leo thang lên cấp có thẩm quyền cao hơn.
