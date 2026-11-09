using EnterpriseBpm.AccessGovernance.Domain.Entities;
using EnterpriseBpm.AccessGovernance.Domain.Enums;
using EnterpriseBpm.AccessGovernance.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace EnterpriseBpm.AccessGovernance.Infrastructure.Seed;

public static class AuthPermissionSeeder
{
    public static async Task SeedAsync(AuthDbContext db, ILogger logger, CancellationToken cancellationToken = default)
    {
        // 1. Seed System Permissions
        var permissions = new List<Permission>
        {
            // Workflow Engine
            new() { Id = Guid.Parse("10000000-0000-0000-0000-000000000001"), PermissionCode = "workflow:submission:create", ModuleName = "WorkflowEngine", Resource = "Submission", Action = "Create", Name = "Tạo mới hồ sơ trình ký", IsSystem = true },
            new() { Id = Guid.Parse("10000000-0000-0000-0000-000000000002"), PermissionCode = "workflow:submission:read", ModuleName = "WorkflowEngine", Resource = "Submission", Action = "Read", Name = "Xem hồ sơ trình ký", IsSystem = true },
            new() { Id = Guid.Parse("10000000-0000-0000-0000-000000000003"), PermissionCode = "workflow:task:approve", ModuleName = "WorkflowEngine", Resource = "Task", Action = "Approve", Name = "Phê duyệt bước xử lý", IsSystem = true },
            new() { Id = Guid.Parse("10000000-0000-0000-0000-000000000004"), PermissionCode = "workflow:task:reject", ModuleName = "WorkflowEngine", Resource = "Task", Action = "Reject", Name = "Từ chối / Yêu cầu giải trình", IsSystem = true },
            new() { Id = Guid.Parse("10000000-0000-0000-0000-000000000005"), PermissionCode = "workflow:process:admin", ModuleName = "WorkflowEngine", Resource = "Process", Action = "Admin", Name = "Can thiệp / Hủy luồng quy trình", IsSystem = true },

            // Org Management
            new() { Id = Guid.Parse("10000000-0000-0000-0000-000000000010"), PermissionCode = "org:employee:read", ModuleName = "OrgManagement", Resource = "Employee", Action = "Read", Name = "Xem danh bạ nhân sự", IsSystem = true },
            new() { Id = Guid.Parse("10000000-0000-0000-0000-000000000011"), PermissionCode = "org:employee:manage", ModuleName = "OrgManagement", Resource = "Employee", Action = "Manage", Name = "Quản lý nhân sự & chức danh", IsSystem = true },
            new() { Id = Guid.Parse("10000000-0000-0000-0000-000000000012"), PermissionCode = "org:department:manage", ModuleName = "OrgManagement", Resource = "Department", Action = "Manage", Name = "Quản lý cơ cấu phòng ban", IsSystem = true },

            // Form Master Data
            new() { Id = Guid.Parse("10000000-0000-0000-0000-000000000020"), PermissionCode = "form:template:design", ModuleName = "FormMasterData", Resource = "Template", Action = "Design", Name = "Thiết kế biểu mẫu Form.io", IsSystem = true },
            new() { Id = Guid.Parse("10000000-0000-0000-0000-000000000021"), PermissionCode = "form:template:publish", ModuleName = "FormMasterData", Resource = "Template", Action = "Publish", Name = "Phát hành phiên bản biểu mẫu", IsSystem = true },

            // Document Management
            new() { Id = Guid.Parse("10000000-0000-0000-0000-000000000030"), PermissionCode = "document:file:view", ModuleName = "DocumentManagement", Resource = "File", Action = "View", Name = "Xem tài liệu đính kèm", IsSystem = true },
            new() { Id = Guid.Parse("10000000-0000-0000-0000-000000000031"), PermissionCode = "document:file:download", ModuleName = "DocumentManagement", Resource = "File", Action = "Download", Name = "Tải xuống tài liệu đính kèm", IsSystem = true },
            new() { Id = Guid.Parse("10000000-0000-0000-0000-000000000032"), PermissionCode = "document:file:upload", ModuleName = "DocumentManagement", Resource = "File", Action = "Upload", Name = "Tải lên tài liệu", IsSystem = true },

            // Audit Log
            new() { Id = Guid.Parse("10000000-0000-0000-0000-000000000040"), PermissionCode = "audit:log:view", ModuleName = "AuditLog", Resource = "Log", Action = "View", Name = "Tra cứu nhật ký kiểm toán", IsSystem = true },
            new() { Id = Guid.Parse("10000000-0000-0000-0000-000000000041"), PermissionCode = "audit:security:view", ModuleName = "AuditLog", Resource = "SecurityEvent", Action = "View", Name = "Xem cảnh báo bảo mật & sự cố", IsSystem = true },
            new() { Id = Guid.Parse("10000000-0000-0000-0000-000000000042"), PermissionCode = "audit:log:export", ModuleName = "AuditLog", Resource = "Log", Action = "Export", Name = "Xuất nhật ký kiểm toán ra Excel", IsSystem = true },

            // Analytics & Reporting
            new() { Id = Guid.Parse("10000000-0000-0000-0000-000000000050"), PermissionCode = "analytics:kpi:view", ModuleName = "AnalyticsReporting", Resource = "Kpi", Action = "View", Name = "Xem Dashboard hiệu suất KPI", IsSystem = true },
            new() { Id = Guid.Parse("10000000-0000-0000-0000-000000000051"), PermissionCode = "analytics:sla:view", ModuleName = "AnalyticsReporting", Resource = "Sla", Action = "View", Name = "Xem báo cáo cảnh báo quá hạn SLA", IsSystem = true },
            new() { Id = Guid.Parse("10000000-0000-0000-0000-000000000052"), PermissionCode = "analytics:report:export", ModuleName = "AnalyticsReporting", Resource = "Report", Action = "Export", Name = "Xuất báo cáo phân tích BI", IsSystem = true },

            // Access Governance
            new() { Id = Guid.Parse("10000000-0000-0000-0000-000000000060"), PermissionCode = "auth:governance:manage", ModuleName = "AccessGovernance", Resource = "Governance", Action = "Manage", Name = "Quản trị phân quyền toàn hệ thống", IsSystem = true },
            new() { Id = Guid.Parse("10000000-0000-0000-0000-000000000061"), PermissionCode = "auth:role:assign", ModuleName = "AccessGovernance", Resource = "Role", Action = "Assign", Name = "Gán vai trò cho người dùng", IsSystem = true },
            new() { Id = Guid.Parse("10000000-0000-0000-0000-000000000062"), PermissionCode = "auth:limit:manage", ModuleName = "AccessGovernance", Resource = "ApprovalLimit", Action = "Manage", Name = "Cấu hình hạn mức phê duyệt", IsSystem = true }
        };

        foreach (var p in permissions)
        {
            if (!await db.Permissions.AnyAsync(x => x.PermissionCode == p.PermissionCode, cancellationToken))
            {
                db.Permissions.Add(p);
            }
        }
        await db.SaveChangesAsync(cancellationToken);

        // 2. Seed Default Roles
        var superAdminRole = await db.Roles.FirstOrDefaultAsync(r => r.RoleCode == "SUPER_ADMIN", cancellationToken);
        if (superAdminRole == null)
        {
            superAdminRole = new Role
            {
                Id = Guid.Parse("20000000-0000-0000-0000-000000000001"),
                RoleCode = "SUPER_ADMIN",
                Name = "Quản trị viên Cấp cao Toàn quyền",
                RoleType = RoleType.System,
                Description = "Sở hữu toàn bộ quyền hạn tối cao trong toàn bộ nền tảng Enterprise BPMN",
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };
            db.Roles.Add(superAdminRole);
            await db.SaveChangesAsync(cancellationToken);
        }

        var staffRole = await db.Roles.FirstOrDefaultAsync(r => r.RoleCode == "STAFF", cancellationToken);
        if (staffRole == null)
        {
            staffRole = new Role
            {
                Id = Guid.Parse("20000000-0000-0000-0000-000000000002"),
                RoleCode = "STAFF",
                Name = "Cán bộ / Nhân viên Nghiệp vụ",
                RoleType = RoleType.Business,
                Description = "Nhân viên khởi tạo hồ sơ trình ký và tra cứu dữ liệu cá nhân",
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };
            db.Roles.Add(staffRole);
            await db.SaveChangesAsync(cancellationToken);
        }

        // 3. Gán Full Permissions cho SUPER_ADMIN
        var allPermIds = await db.Permissions.Select(p => p.Id).ToListAsync(cancellationToken);
        var existingRolePermIds = await db.RolePermissions
            .Where(rp => rp.RoleId == superAdminRole.Id)
            .Select(rp => rp.PermissionId)
            .ToListAsync(cancellationToken);

        var missingPermIds = allPermIds.Except(existingRolePermIds).ToList();
        if (missingPermIds.Any())
        {
            var rolePermsToAdd = missingPermIds.Select(pId => new RolePermission
            {
                RoleId = superAdminRole.Id,
                PermissionId = pId
            });
            await db.RolePermissions.AddRangeAsync(rolePermsToAdd, cancellationToken);
            await db.SaveChangesAsync(cancellationToken);
        }

        // 4. Seed Navigation Menus
        var menus = new List<MenuPermission>
        {
            new() { Id = Guid.Parse("30000000-0000-0000-0000-000000000001"), MenuKey = "menu_dashboard", Title = "Bảng điều khiển", RouteUrl = "/dashboard", IconClass = "fas fa-tachometer-alt", DisplayOrder = 1, IsActive = true },
            new() { Id = Guid.Parse("30000000-0000-0000-0000-000000000002"), MenuKey = "menu_workflows", Title = "Hồ sơ Trình ký", RouteUrl = "/workflows", IconClass = "fas fa-tasks", DisplayOrder = 2, IsActive = true },
            new() { Id = Guid.Parse("30000000-0000-0000-0000-000000000003"), MenuKey = "menu_workflows_inbox", ParentMenuKey = "menu_workflows", Title = "Hộp thư Đến", RouteUrl = "/workflows/inbox", IconClass = "fas fa-inbox", DisplayOrder = 1, RequiredPermissionCode = "workflow:submission:read", IsActive = true },
            new() { Id = Guid.Parse("30000000-0000-0000-0000-000000000004"), MenuKey = "menu_workflows_create", ParentMenuKey = "menu_workflows", Title = "Tạo Hồ sơ mới", RouteUrl = "/workflows/create", IconClass = "fas fa-plus-circle", DisplayOrder = 2, RequiredPermissionCode = "workflow:submission:create", IsActive = true },
            new() { Id = Guid.Parse("30000000-0000-0000-0000-000000000005"), MenuKey = "menu_forms", Title = "Form Studio", RouteUrl = "/forms/designer", IconClass = "fas fa-file-invoice", DisplayOrder = 3, RequiredPermissionCode = "form:template:design", IsActive = true },
            new() { Id = Guid.Parse("30000000-0000-0000-0000-000000000006"), MenuKey = "menu_analytics", Title = "Báo cáo & Phân tích BI", RouteUrl = "/analytics/dashboard", IconClass = "fas fa-chart-line", DisplayOrder = 4, RequiredPermissionCode = "analytics:kpi:view", IsActive = true },
            new() { Id = Guid.Parse("30000000-0000-0000-0000-000000000007"), MenuKey = "menu_audit", Title = "Nhật ký Kiểm toán", RouteUrl = "/audit/logs", IconClass = "fas fa-history", DisplayOrder = 5, RequiredPermissionCode = "audit:log:view", IsActive = true },
            new() { Id = Guid.Parse("30000000-0000-0000-0000-000000000008"), MenuKey = "menu_governance", Title = "Quản trị Phân quyền", RouteUrl = "/governance/matrix", IconClass = "fas fa-user-shield", DisplayOrder = 6, RequiredPermissionCode = "auth:governance:manage", IsActive = true }
        };

        foreach (var m in menus)
        {
            if (!await db.MenuPermissions.AnyAsync(x => x.MenuKey == m.MenuKey, cancellationToken))
            {
                db.MenuPermissions.Add(m);
            }
        }
        await db.SaveChangesAsync(cancellationToken);

        logger.LogInformation("Hoàn tất khởi tạo dữ liệu mẫu hạt nhân cho phân hệ AccessGovernance.");
    }
}

