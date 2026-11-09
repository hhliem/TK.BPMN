# Kiến trúc Modular Monolith & Phân rã Schema

## Bounded Contexts & Schema Mapping (10 Modules)

| Module | Schema SQL Server | Nhiệm vụ chính |
|---|---|---|
| OrgManagement | `[org]` | Cây cơ cấu tổ chức, nhân sự, chức danh (`Positions`), quan hệ báo cáo, ủy quyền, danh bạ nội bộ (`Directory`) |
| FormMasterData | `[form]` | Form.io JSON Schema, Form Versioning (`IsLatest`), Centralized Master Data, Cascading Lookups |
| WorkflowEngine | `[workflow]` | Quản lý vòng đời đơn, Elsa v3 runtime, Custom Activities, Bookmarks, Lịch sử phê duyệt (`ApprovalHistory`) |
| DocumentManagement | `[document]` | Metadata file đính kèm, liên kết OneDrive qua Graph API, sinh tài liệu tự động từ Template |
| Notification | `[notification]` | Template đa kênh, Queue gửi email O365, Teams Webhook, ZNS, SMS Gateway |
| AiCopilot | `[ai]` | Local MCP Server kết nối Local LLM (Ollama), RAG Qdrant quy chế nội bộ, lịch sử hội thoại |
| SystemSettings | `[settings]` | Cấu hình thương hiệu, white-label, tham số hệ thống, lịch sử thay đổi cấu hình |
| InternalNews | `[news]` | Bản tin nội bộ, chính sách tập đoàn, thông báo chung, bài viết hẹn giờ đăng, xác nhận đã đọc |
| AuditLog | `[audit]` | Nhật ký kiểm toán CUD toàn hệ thống, bảo mật đăng nhập, trigger cấm sửa/xóa (Append-Only) |
| AnalyticsReporting | `[analytics]` | Bảng tổng hợp KPI (Summary Metrics), Materialized Views báo cáo, Executive BI Dashboard, Quản trị SLA |
| AccessGovernance | `[auth]` | Quản trị Phân quyền toàn hệ thống (RBAC/ABAC), Permissions, Roles, Data Scopes, Hạn mức phê duyệt |

## Quy định Code & Ranh giới (Architecture Rules)
- Không project nào thuộc Module A được tham chiếu đến project `Infrastructure` hay DbContext của Module B.
- Giao tiếp đồng bộ chỉ cho phép thông qua Read-only Query Interface đặt trong `Application/Contracts`.
- Mọi tương tác gây side-effect phải dùng MediatR Integration Events kế thừa từ `BaseIntegrationEvent`.
- Tất cả Entity áp dụng xóa mềm phải triển khai `ISoftDeletable` và Global Query Filter.
- Tất cả Entity áp dụng kiểm toán phải triển khai `IAuditable` và SaveChangesInterceptor.
