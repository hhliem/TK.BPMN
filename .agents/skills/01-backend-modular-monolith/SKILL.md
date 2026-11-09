---
name: bpmn-backend-modular-monolith
description: Hướng dẫn chi tiết triển khai Backend Modular Monolith trên .NET 8/9 cho nền tảng Enterprise BPMN, Elsa Workflows v3, MediatR Events, SQL Server Multi-Schema.
---

# Skill: Backend Modular Monolith (.NET Core & Elsa Workflows v3)

## 1. Nguyên tắc Bắt buộc
1. **Module Isolation**: Mỗi module sở hữu DbContext riêng biệt, ánh xạ vào Schema SQL Server riêng biệt:
   - `[org]`: OrgManagement (Cơ cấu, nhân sự, chức danh, ủy quyền, danh bạ)
   - `[form]`: FormMasterData (Form.io JSON schema, Form versioning, danh mục cascading)
   - `[workflow]`: WorkflowEngine (Elsa v3 runtime, Approval tasks, ApprovalHistory bất biến)
   - `[document]`: DocumentManagement (OneDrive/Graph API, file đính kèm, sinh tài liệu template)
   - `[notification]`: Notification (Template đa kênh, hàng đợi gửi mail, teams, zalo, sms)
   - `[ai]`: AiCopilot (Local MCP Server, RAG Qdrant quy chế nội bộ, lịch sử chat)
   - `[settings]`: SystemSettings (Cấu hình hệ thống, white-label, lịch sử đổi cấu hình)
   - `[news]`: InternalNews (Bản tin nội bộ, tin khẩn, hẹn giờ đăng, xác nhận đã đọc)
   - `[audit]`: AuditLog (Ghi nhận CUD tự động, bảo mật, trigger cấm sửa/xóa)
   - `[analytics]`: AnalyticsReporting (Materialized metrics, Executive BI Dashboard, KPI phòng ban, SLA Business Hours)
   - `[auth]`: AccessGovernance (Quản trị Phân quyền toàn hệ thống, RBAC/ABAC, Data Scope, Hạn mức phê duyệt)
   *Tuyệt đối không JOIN bảng chéo schema qua Entity Framework.*

2. **Giao tiếp Bất đồng bộ & Hợp đồng**:
   - Giao tiếp đồng bộ chỉ qua Public Contracts (Read-only Query Interface đặt trong Application/Contracts).
   - Giao tiếp side-effects bắt buộc sử dụng MediatR Integration Events kế thừa từ `BaseIntegrationEvent` trong `SharedKernel`.

3. **Shared Kernel Standards**:
   - Mọi Entity áp dụng `ISoftDeletable` phải có Global Query Filter lọc `DeletedAt == null`.
   - Mọi Entity áp dụng `IAuditable` phải được tự động gán `CreatedAt`, `CreatedBy`, `UpdatedAt`, `UpdatedBy` qua `AuditableFieldsInterceptor`.

4. **Elsa Workflows v3 Task Lifecycle**:
   - Dùng Custom Activity kế thừa `Activity<string>` tạo Approval Task và tạo Bookmark (`context.CreateBookmark(...)`) tạm dừng workflow.
   - Resume workflow qua API bằng cách nạp payload `ApprovalResultPayload` và ghi nhận `ApprovalHistory` bất biến.

5. **Zero Binary in Database**: File đính kèm đẩy trực tiếp lên Microsoft OneDrive qua Microsoft Graph API, chỉ lưu URL và DriveItemId vào cơ sở dữ liệu.

## 2. Quy trình Thực thi cho AI Agent
1. **Scaffold Layering**: Mỗi module gồm 4 project: `Domain`, `Application`, `Infrastructure`, `Presentation`.
2. **Cấu hình DbContext**: Gọi `modelBuilder.HasDefaultSchema("<schema_name>")`. Đánh Non-Clustered Index trên các khóa ngoại và cột lọc cascading.
3. **Đăng ký Services**: Tại `ApiHost/Program.cs`, quét assemblies cho MediatR, nạp Elsa Workflows services, đăng ký các DbContext với cùng connection string.
