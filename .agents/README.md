# Enterprise BPMN Platform - AI Agent Skills & Development Package

Bộ tài liệu kỹ thuật, Skill hướng dẫn và Source Code Examples chuẩn hóa dành cho AI Coding Agents (Cursor, Claude Code, Windsurf, Copilot, v.v.) phát triển nền tảng **Enterprise BPMN/Workflow Platform**.

## 1. Cấu trúc Gói 6 Skills Doanh nghiệp (Enterprise Skills)

- **`skills/01-backend-modular-monolith/`**: Hướng dẫn kiến trúc Backend ASP.NET Core (.NET 8/9), Elsa Workflows v3, SQL Server Multi-Schema (10 schemas), MediatR Domain Events, Base Entities (`ISoftDeletable`, `IAuditable`).
- **`skills/02-frontend-adminlte-formio/`**: Hướng dẫn Frontend AdminLTE 3 / SPA, Form.io Dynamic Forms kèm Cascading Loading, Nhúng Elsa Studio Designer, Dashboard BI (ApexCharts/Chart.js).
- **`skills/03-security-and-governance/`**: Tiêu chuẩn Bảo mật phân tầng (RBAC/ABAC), Chống giả mạo dữ liệu (Anti-Tampering FluentValidation), Audit Trail Ký thay Ủy quyền và Ranh giới bảo mật AI Context.
- **`skills/04-local-mcp-ai-copilot/`**: Thiết kế Local MCP Server, C# MCP Tools cho báo cáo thống kê tự nhiên, Pipeline RAG tra cứu quy chế với Qdrant Vector DB.
- **`skills/05-high-performance-tuning/`**: *(MỚI)* Tối ưu hóa hiệu năng cao: Caching 2 tầng (L1 In-Memory + L2 Redis), CQRS đường đọc tách biệt với Dapper, Tối ưu SQL Server Indexing (Covering & Filtered Index), Connection Pooling.
- **`skills/06-reliability-and-resilience-engineering/`**: *(MỚI)* Kỹ thuật ổn định hệ thống: Transactional Outbox Pattern cho Integration Events (chống mất sự kiện), Polly v8 Resilience Pipelines (Circuit Breaker, Exponential Retry), Distributed Lock chống tranh chấp tác vụ ngầm, Health Checks.

---

## 2. Cách Nạp vào AI Agent

- **Cursor**: Copy các thư mục trong `skills/` vào `.cursor/rules/` hoặc nạp file `SKILL.md` tương ứng vào ngữ cảnh prompt.
- **Claude Code / Windsurf**: Đặt các file `SKILL.md` tương ứng vào project instructions / rules.
- **Antigravity / Gemini CLI**: Tự động phát hiện và kích hoạt skills thông qua cấu hình `.agents/skills` và rules trong `.agents/rules`.

---

## 3. Quy tắc Bắt buộc dành cho AI Agents (Agent Rules)

### Quy tắc 1: Quản trị Báo cáo Sprint (Sprint Reporting Governance)
- **Vị trí lưu trữ:** Toàn bộ báo cáo nghiệm thu hoàn tất từng sprint **BẮT BUỘC** lưu vào thư mục `d:\My Project\TK.BPMN\reports/`.
- **Quy ước đặt tên:** `reports/sprint-{XX}-completion-report.md`.
- **Nội dung bắt buộc:** 
  1. Tiến độ chi tiết (Delivered Features, Story Points, Completed Tasks).
  2. Kết quả kiểm thử (Build status, NetArchTest, Unit Tests, Acceptance Criteria).
  3. Review tổng thể dự án (Architecture Compliance, Health, Technical Debt & Readiness cho sprint kế tiếp).

### Quy tắc 2: Quản trị Cấu hình & Nguyên lý Thất bại Sớm (Configuration & Fail-Fast Governance)
- **Cấm tuyệt đối:** Không hardcode connection string hoặc fallback về chuỗi cục bộ `(localdb)`.
- **Lấy trực tiếp từ Cấu hình:** Mọi kết nối Database, Redis, Services phải đọc từ `IConfiguration`. Nếu thiếu thì ném `InvalidOperationException` ngay lập tức (Fail Fast).
- **Không nuốt lỗi (No Try-Fail):** Không bọc try-catch để ngầm bỏ qua lỗi cấu hình/migration hệ thống.
- **Không dữ liệu giả:** `ICurrentUserService` không tự ý fallback về email giả (`admin@enterprise.bpm`). Thiếu claim thì trả về `null`.
- **Chi tiết xem tại:** `.agents/rules/configuration-and-failfast-governance.md`.
