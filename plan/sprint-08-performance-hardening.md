# 🚀 Sprint 08: High-Performance Tuning, Resilience Engineering & Production Go-Live

---

## 1. Thông tin Chung về Sprint 08

- **Tên Sprint:** Sprint 08 — Tối ưu Hiệu năng Cao, Độ Ổn định Hệ thống & Cắt chuyển Vận hành Sản xuất (Go-Live)
- **Thời gian Thực hiện:** Tuần 15 - Tuần 16 (10 ngày làm việc)
- **Mục tiêu Sprint (Sprint Goal):**
  > Hoàn thiện toàn bộ các tiêu chuẩn kỹ thuật nâng cao theo **Skill 05 (High Performance Tuning)** và **Skill 06 (Reliability & Resilience Engineering)**: triển khai Transactional Outbox Pattern cho Integration Events (bảo đảm At-Least-Once Delivery), tích hợp Polly v8 Resilience Pipelines (Circuit Breaker, Exponential Retry), tối ưu hóa Caching 2 tầng (Memory L1 + Redis L2), chuyển đổi các API đọc tải cao sang Dapper CQRS, rà soát cấu hình SQL Server Indexing; tiến hành kiểm thử tải (Load Test), rà soát bảo mật tổng thể (Security Audit), nghiệm thu UAT toàn tập đoàn và thực hiện cắt chuyển vận hành sản xuất chính thức (Production Go-Live).
- **Tổng Story Points cam kết:** **55 SP**
- **Tổng Giờ Kỹ thuật khả dụng (Capacity):** **340 Giờ** (Team 5 Dev + 1 QA + DevOps)

---

## 2. Sprint Backlog & Acceptance Criteria (Gherkin)

### US-68, US-69 & US-70: Caching 2 Tầng, Dapper CQRS & SQL Server Indexing (24 SP)
- **Acceptance Criteria:**
  ```gherkin
  Scenario: Đo lường tốc độ phản hồi của API Master Data và Danh bạ tải cao
    Given hệ thống đang chịu tải 1,000 requests/giây
    When người dùng gọi API /api/master-data/items hoặc /api/org/directory
    Then thời gian phản hồi P99 đạt dưới 30ms nhờ L1/L2 Cache và Dapper AsNoTracking
    And SQL Server không phát sinh Key Lookups nhờ Covering Index.
  ```

### US-71: Transactional Outbox Pattern cho Integration Events (13 SP)
- **Acceptance Criteria:**
  ```gherkin
  Scenario: Bảo đảm không mất sự kiện khi máy chủ bị khởi động lại đột ngột
    Given một đơn vừa được duyệt và lưu vào Database trong cùng transaction với OutboxMessages
    When máy chủ API bị crash ngay trước khi phát sự kiện ra ngoài RAM
    Then khi máy chủ khởi động lại, OutboxProcessorWorker tự động quét lại các message chưa xử lý
    And sự kiện WorkflowApprovedEvent được phát lại thành công (At-Least-Once Delivery).
  ```

### US-72: Polly v8 Resilience Pipeline & Circuit Breaker (8 SP)
- **Acceptance Criteria:**
  ```gherkin
  Scenario: Tự động ngắt mạch (Circuit Breaker) khi dịch vụ ngoài bị sự cố
    Given Microsoft Graph API bị lỗi 503 liên tục với tỷ lệ lỗi > 50% trong 30 giây
    When các request tiếp theo được gửi tới Graph API
    Then Circuit Breaker chuyển sang trạng thái "Open" và từ chối request ngay tại máy chủ
    And bảo vệ ứng dụng khỏi nguy cơ treo toàn bộ luồng xử lý (Thread Exhaustion).
  ```

### US-73: Health Checks, Security Hardening & Go-Live Cutover (10 SP)
- **Acceptance Criteria:**
  ```gherkin
  Scenario: Kiểm tra độ sẵn sàng trước khi tiếp nhận traffic
    Given máy chủ triển khai trên Kubernetes / IIS Cluster
    When Load Balancer thăm dò endpoint /healthz/ready
    Then endpoint trả về HTTP 200 OK chỉ khi SQL Server, Redis Cluster, OneDrive và Qdrant đều kết nối thành công.
  ```

---

## 3. Danh mục Task Kỹ thuật Chi tiết (Tasks Breakdown)

| Mã Task | Module | Hạng mục Kỹ thuật | Mô tả Chi tiết Công việc Cần Code | Giờ (h) | Người Phụ trách |
|:---:|:---:|---|---|:---:|:---:|
| **TSK-801** | Core | Transactional Outbox Interceptor | Triển khai `OutboxMessageSaveChangesInterceptor` tự động bắt `DomainEvents` và lưu vào bảng `OutboxMessages`. | 10 | Backend Dev 1 |
| **TSK-802** | Core | Outbox Processor Background Worker | Xây dựng Worker quét bảng Outbox mỗi 3 giây, publish qua MediatR, đánh dấu `ProcessedAt` và cơ chế retry khi lỗi. | 10 | Backend Dev 1 |
| **TSK-803** | Core | Polly v8 Resilience Registration | Đăng ký `AddStandardResilienceHandler` cho toàn bộ HttpClient: MS Graph API, Mail O365, Teams Webhook, Qdrant gRPC. | 8 | Backend Dev 2 |
| **TSK-804** | Core | Redis Multi-Level Caching L1/L2 | Triển khai `MultiLevelMasterDataCacheService` kết hợp `IMemoryCache` (L1) và `IDistributedCache` Redis (L2). | 10 | Backend Dev 2 |
| **TSK-805** | Core | Dapper Read Optimization | Chuyển đổi 100% các API truy vấn danh sách phân trang trong Workflow, Org, Form sang Dapper `.QueryMultipleAsync()`. | 14 | Backend Dev 3 |
| **TSK-806** | DB | Covering & Filtered Index Audit | Rà soát toàn bộ Execution Plans trên SQL Server, bổ sung các mệnh đề `INCLUDE` và Filtered Index `WHERE [Status] = 'Pending'`. | 10 | Tech Lead / DBA |
| **TSK-807** | DB | DbContext Pooling Setup | Chuyển đổi cấu hình `AddDbContextPool<TDbContext>` trong `Program.cs`, cấu hình MaxPoolSize = 256. | 6 | Tech Lead |
| **TSK-808** | Core | ASP.NET Core Health Checks | Cấu hình `/healthz/live` và `/healthz/ready` kiểm tra trạng thái SQL Server, Redis, MS Graph, Qdrant Vector DB. | 8 | Backend Dev 1 |
| **TSK-809** | Core | Serilog Structured Logging | Cấu hình Serilog đẩy log có cấu trúc (JSON), gắn `CorrelationId` và `TraceId` theo dõi xuyên suốt giữa các module. | 8 | Backend Dev 2 |
| **TSK-810** | Sec | Security Audit & Pen-Test | Rà soát lỗ hổng OWASP Top 10: XSS, SQLi, CSRF, BOLA, kiểm tra bảo vệ Token JWT và mã hóa At-Rest. | 14 | QA / Tech Lead |
| **TSK-811** | Perf | K6 / BenchmarkDotNet Load Test | Viết kịch bản kiểm thử tải bằng `k6`: Giả lập 1,000 CCU nộp đơn và duyệt đơn đồng thời, đo lường P99 latency và RAM/CPU usage. | 16 | QA Engineer |
| **TSK-812** | Ops | CI/CD Production Pipeline | Thiết lập pipeline GitHub Actions / Azure DevOps tự động build, test, scan SonarQube và deploy lên cụm Production. | 12 | DevOps / Tech Lead |
| **TSK-813** | Ops | Idempotent DB Migration Script | Xuất script SQL Migration tự động có cờ kiểm tra `IF NOT EXISTS` cho toàn bộ 11 schemas, chạy thử nghiệm trên Staging. | 8 | Tech Lead / DBA |
| **TSK-814** | Ops | User Acceptance Testing (UAT) | Phối hợp với Product Owner và người dùng đại diện các phòng ban thực hiện nghiệm thu toàn bộ kịch bản nghiệp vụ. | 16 | Toàn đội ngũ |
| **TSK-815** | Ops | Data Seeding & Migration Cutover | Nạp dữ liệu danh mục ban đầu: Cây tổ chức tập đoàn, 1,000 nhân sự mẫu, chức danh, các biểu mẫu chuẩn và ma trận phân quyền. | 10 | Backend Dev 3 |
| **TSK-816** | Ops | Production Go-Live & Monitoring | Thực hiện cắt chuyển Go-Live chính thức, trực giám sát APM (Application Performance Monitoring) và nhật ký lỗi ngày đầu vận hành. | 16 | Toàn đội ngũ |
| **TỔNG** | | | **Tổng số 16 Tasks Kỹ thuật** | **176h** | |

---

## 4. Kế hoạch Đánh giá & Tiêu chí Release Sprint 08 (Final Go-Live)

### 4.1. Sprint Review / Final Production Checklist
- [ ] **Tiêu chí 1: Hiệu năng Tải (Load Performance):** Chạy bài test `k6` với 1,000 người dùng đồng thời $\rightarrow$ P99 Read Latency $< 28\text{ ms}$, P99 Write Latency $< 85\text{ ms}$, CPU máy chủ $< 65\%$, không có request nào bị lỗi HTTP 500.
- [ ] **Tiêu chí 2: Khả năng Chịu lỗi (Resilience Verification):** Cố tình ngắt nguồn Redis hoặc SQL Server tạm thời $\rightarrow$ Hệ thống tự động phục hồi kết nối trong vòng 10 giây; Outbox Worker gửi bù toàn bộ sự kiện còn tồn mà không mất dữ liệu.
- [ ] **Tiêu chí 3: An toàn Bảo mật (Security Clearance):** Báo cáo quét mã nguồn SonarQube đạt tiêu chuẩn Quality Gate (0 Vulnerabilities, 0 Security Hotspots, Code Coverage đạt $\ge 82\%$).
- [ ] **Tiêu chí 4: Nghiệm thu Nghiệp vụ (UAT Sign-Off):** 100% kịch bản UAT của 11 modules được Product Owner và đại diện doanh nghiệp ký biên bản nghiệm thu đạt yêu cầu.
- [ ] **Tiêu chí 5: Cắt chuyển Thành công (Go-Live Cutover):** Hệ thống chính thức mở cổng truy cập nội bộ cho toàn thể nhân viên tập đoàn, sẵn sàng vận hành $24/7$.

