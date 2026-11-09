---
name: bpmn-reliability-and-resilience-engineering
description: Hướng dẫn kỹ thuật bảo đảm tính ổn định cao (High Reliability), khả năng chịu lỗi (Resilience), Transactional Outbox Pattern cho Integration Events, Polly v8 Pipelines, Distributed Lock và Health Checks.
---

# Skill: Reliability & Resilience Engineering (.NET Core & Distributed Systems)

## 1. Mục tiêu Chất lượng Vận hành (Operational Reliability Goals)

Trong nền tảng Enterprise BPMN, việc mất mát dữ liệu hoặc dừng dịch vụ có thể làm gián đoạn toàn bộ hoạt động ký duyệt văn bản, hồ sơ thầu, thanh toán của doanh nghiệp. Hệ thống phải đảm bảo:
- **Availability Target:** Tối thiểu **99.9% Uptime** (chỉ cho phép downtime $< 8.7$ giờ/năm).
- **At-Least-Once Delivery:** Mọi Integration Event giữa các module không bao giờ bị rơi rớt khi gặp sự cố mạng hoặc server khởi động lại.
- **Fault-Tolerant Integrations:** Tích hợp với dịch vụ ngoài (OneDrive Graph API, Mail O365, ZNS, SMS Gateway) phải có cơ chế tự phục hồi lỗi tức thời.

### 4 Trụ cột Kỹ thuật Bắt buộc

1. **Transactional Outbox Pattern (Chống Mất Sự kiện):**
   - Không được gọi trực tiếp `_mediator.Publish()` ngay trong HTTP Request nếu sự kiện đó yêu cầu các module khác thay đổi dữ liệu quan trọng.
   - Thay vào đó, serialize sự kiện thành bản ghi `OutboxMessages` và lưu trong cùng Database Transaction với Entity.
   - Một Background Worker (`OutboxProcessorWorker`) quét bảng Outbox và publish sự kiện với cơ chế Retry lũy tiến. Đảm bảo tính nhất quán cuối cùng (**Eventual Consistency**) dù ứng dụng bị sập bất ngờ.

2. **Polly v8 Resilience Pipelines (Khả năng Tự phục hồi):**
   - Áp dụng bộ 3 chiến lược: **Retry with Exponential Backoff + Jitter**, **Circuit Breaker** (ngắt mạch khi dịch vụ ngoài chết quá $50\%$ trong 30 giây), và **Timeout / Rate Limiter**.
   - Bắt buộc áp dụng cho mọi HTTP Client gọi sang: Microsoft Graph API, Mail Gateway, SMS/ZNS Gateway, Qdrant Vector DB.

3. **Distributed Locking & Concurrency Control (Chống Tranh chấp Dữ liệu):**
   - Các tác vụ Background Jobs (quét SLA 15 phút, tổng hợp Metrics hàng giờ, đối soát số liệu ban đêm) khi triển khai trên mô hình cụm máy chủ nhiều instances (**Multi-Instance Horizontal Scaling**) bắt buộc phải sử dụng **Distributed Lock trên Redis** (`RedLock`) để chống chạy trùng lặp (Duplicate Execution).
   - Tác vụ duyệt task của người dùng áp dụng **Optimistic Concurrency Control** kết hợp Distributed Lock ngắn theo `ApprovalTaskId`.

4. **Health Checks & Deep Diagnostics:**
   - Cung cấp 2 endpoint:
     - `/healthz/live` (Liveness): Kiểm tra tiến trình ASP.NET Core có đang sống không (cho Kubernetes/IIS thăm dò).
     - `/healthz/ready` (Readiness): Kiểm tra kết nối thực tế tới SQL Server, Redis Cluster, OneDrive Graph API và Qdrant. Nếu DB mất kết nối, lập tức ngắt traffic đến node đó.

---

## 2. Quy trình Thực thi cho AI Agent khi Viết Code

1. **Khi gọi dịch vụ bên ngoài (External I/O):**
   - Luôn đăng ký `HttpClient` thông qua `AddResilienceHandler` của thư viện `Microsoft.Extensions.Http.Resilience` (Polly v8).
   - Cấu hình Timeout tối đa 30 giây và Retry tối đa 3 lần.
2. **Khi viết Sự kiện Tích hợp liên module quan trọng:**
   - Xác định xem mất sự kiện này có gây sai lệch nghiệp vụ không (ví dụ: `WorkflowApprovedEvent` cần sinh văn bản hoặc `EmployeeDeactivatedEvent` cần thu hồi task).
   - Nếu có, bắt buộc lưu qua Outbox Pattern thay vì bắn In-memory Event thô.
3. **Khi viết Background Worker định kỳ:**
   - Tuyệt đối không dùng `while(true) Task.Delay` thuần túy nếu không có try-catch bọc quanh toàn bộ vòng lặp (tránh unhandled exception làm chết ngầm BackgroundService).
   - Bọc logic chạy bằng Redis Distributed Lock với thời gian TTL hợp lý.

