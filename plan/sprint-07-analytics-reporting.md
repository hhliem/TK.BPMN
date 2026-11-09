# 🚀 Sprint 07: Enterprise System-Wide Reporting, Performance KPI & SLA Analytics

---

## 1. Thông tin Chung về Sprint 07

- **Tên Sprint:** Sprint 07 — Phân hệ Báo cáo Toàn hệ thống, Đo lường Hiệu suất KPI & Quản trị SLA
- **Thời gian Thực hiện:** Tuần 13 - Tuần 14 (10 ngày làm việc)
- **Mục tiêu Sprint (Sprint Goal):**
  > Xây dựng hoàn chỉnh Phân hệ Báo cáo Toàn hệ thống (`AnalyticsReporting`): triển khai động cơ tính toán SLA theo giờ làm việc hành chính thực tế (`BusinessHoursSlaEngine`), phát hiện nút thắt cổ chai quy trình, đo lường thước đo năng suất nhân sự (Scorecard cá nhân/phòng ban), phân tích dòng tiền chi phí trình ký; xây dựng Công cụ tạo báo cáo động (Dynamic Custom Report Builder) cho phép kéo thả trường dữ liệu và lên lịch tự động xuất báo cáo định kỳ gửi qua Email/Teams.
- **Tổng Story Points cam kết:** **73 SP**
- **Tổng Giờ Kỹ thuật khả dụng (Capacity):** **340 Giờ** (Team 5 Dev + 1 QA)

---

## 2. Sprint Backlog & Acceptance Criteria (Gherkin)

### US-60 & US-61: Bảng Materialized Metrics & Thuật toán SLA Giờ Hành chính (21 SP)
- **Acceptance Criteria:**
  ```gherkin
  Scenario: Tính toán thời hạn SLA loại trừ ngày nghỉ cuối tuần
    Given một phiếu nộp lúc 16:30 chiều Thứ Sáu với thời hạn SLA 4 giờ
    When BusinessHoursSlaEngine tính toán thời điểm hết hạn DueDate
    Then hệ thống tính 1 giờ làm việc chiều Thứ Sáu (16:30 - 17:30)
    And bỏ qua toàn bộ Thứ Bảy và Chủ Nhật
    And hạn chót hoàn tất được ấn định chính xác là 11:00 trưa Thứ Hai tuần kế tiếp.
  ```

### US-62 & US-63: Dashboard Hiệu suất SLA & Phát hiện Nút thắt Cổ chai (16 SP)
- **Acceptance Criteria:**
  ```gherkin
  Scenario: Tự động phát hiện bước duyệt làm nghẽn tiến độ quy trình
    Given quy trình thanh toán có bước "Kế toán trưởng ký" có thời gian chờ trung bình 36 giờ (trong khi SLA cho phép 12 giờ)
    When hệ thống tổng hợp số liệu vào bảng StepSlaAnalytics
    Then bước này được tự động gán cờ IsCurrentBottleneck = 1
    And trên Dashboard quản trị xuất hiện cảnh báo điểm nghẽn màu đỏ kèm gợi ý phân bổ thêm nhân sự.
  ```

### US-64 & US-65: Năng suất Nhân sự & Báo cáo Dòng tiền Trình ký (16 SP)
- **Acceptance Criteria:**
  ```gherkin
  Scenario: Xem bảng đo năng suất nhân viên (Employee Scorecard)
    Given Trưởng phòng mở xem hiệu suất làm việc tháng hiện tại của phòng mình
    When xem chi tiết nhân viên Nguyễn Văn A
    Then hiển thị: Tổng 45 tasks đã giao, 42 tasks hoàn thành đúng hạn (SLA Compliance 93.3%), 3 tasks tồn đọng và First-Time-Right Rate đạt 88%.
  ```

### US-66 & US-67: Dynamic Report Builder & Lập lịch Gửi Tự động (20 SP)
- **Acceptance Criteria:**
  ```gherkin
  Scenario: Lên lịch gửi báo cáo tự động sáng Thứ Hai hàng tuần
    Given một mẫu báo cáo tùy chỉnh "Tổng hợp Chi phí Tuần" đã được cấu hình Cron "0 8 * * 1"
    When đồng hồ hệ thống điểm 08:00 sáng Thứ Hai
    Then ScheduledReportDistributionWorker tự động xuất file Excel và gửi qua email cho Ban Giám đốc.
  ```

---

## 3. Danh mục Task Kỹ thuật Chi tiết (Tasks Breakdown)

| Mã Task | Module | Hạng mục Kỹ thuật | Mô tả Chi tiết Công việc Cần Code | Giờ (h) | Người Phụ trách |
|:---:|:---:|---|---|:---:|:---:|
| **TSK-701** | Analytics | Database Schema [analytics] Ext | Tạo 6 bảng schema `[analytics]` (WorkingCalendars, WorkflowSummaryMetrics, StepSlaAnalytics, EmployeeProductivityMetrics, CustomReportTemplates, ScheduledReportSubscriptions). | 8 | Backend Dev 1 |
| **TSK-702** | Analytics | Business Hours SLA Engine | Triển khai `ISlaCalculationEngine` tính toán thời gian làm việc chính xác đến từng phút, loại trừ giờ nghỉ trưa, cuối tuần và ngày lễ. | 14 | Backend Dev 1 |
| **TSK-703** | Analytics | Metrics Aggregation Worker | Triển khai Background Worker quét mỗi giờ, gom dữ liệu vào bảng `WorkflowSummaryMetrics` và `DepartmentKpiMonthly`. | 10 | Backend Dev 2 |
| **TSK-704** | Analytics | Bottleneck Detection Algorithm | Viết thuật toán phân tích Lead Time, Wait Time và tính tỷ lệ vi phạm SLA từng bước để xác định cờ `IsCurrentBottleneck`. | 10 | Backend Dev 2 |
| **TSK-705** | Analytics | Productivity Scorecard Service | Xây dựng dịch vụ tổng hợp năng suất nhân sự theo tháng: tính toán Backlog, SLA Compliance Rate và First-Time-Right Rate. | 10 | Backend Dev 3 |
| **TSK-706** | Analytics | Financial Expenditure Aggregator | Viết service tổng hợp số tiền duyệt chi theo Dự án, Trung tâm Chi phí (Cost Center) từ payload `FormDataJson` của Form.io. | 10 | Backend Dev 3 |
| **TSK-707** | Analytics | Dynamic Query Builder Engine | Xây dựng Engine biên dịch cấu hình JSON kéo thả thành câu lệnh SQL Parameterized an toàn qua Dapper, chống SQL Injection. | 16 | Backend Dev 1 |
| **TSK-708** | Analytics | ClosedXML Multi-Tab Exporter | Tích hợp thư viện `ClosedXML` xuất file Excel chuyên nghiệp (có định dạng màu sắc tiêu đề, auto-fit cột, công thức tính tổng, kẻ bảng). | 10 | Backend Dev 2 |
| **TSK-709** | Analytics | Scheduled Report Worker | Triển khai `ScheduledReportDistributionWorker` kiểm tra lịch Cron, sinh file báo cáo và phát sự kiện gửi qua Email/Teams. | 10 | Backend Dev 3 |
| **TSK-710** | UI | Executive BI Dashboard | Dựng Dashboard tổng quan cho Ban Giám đốc với ApexCharts (Biểu đồ tròn tỷ lệ SLA, Biểu đồ cột dòng tiền chi phí, Biểu đồ đường xu hướng). | 18 | Frontend Dev 1 |
| **TSK-711** | UI | SLA Bottleneck Heatmap | Xây dựng biểu đồ nhiệt (Heatmap) trực quan thể hiện thời gian xử lý trung bình theo từng bước duyệt của từng loại biểu mẫu. | 16 | Frontend Dev 1 |
| **TSK-712** | UI | Employee Productivity Grid | Dựng màn hình bảng đo năng suất nhân sự, có bộ lọc theo Khối/Phòng ban và chức năng xuất bảng đánh giá thi đua. | 14 | Frontend Dev 2 |
| **TSK-713** | UI | Dynamic Report Builder UI | Dựng giao diện kéo thả tạo báo cáo: Danh sách trường dữ liệu bên trái, khung chọn cột hiển thị, thiết lập điều kiện lọc và nhóm dữ liệu. | 22 | Frontend Dev 2 |
| **TSK-714** | UI | Report Schedule Subscription Modal | Dựng modal cho phép người dùng chọn tần suất nhận báo cáo (Hàng ngày, Hàng tuần, Đầu tháng) và nhập danh sách email nhận. | 10 | Frontend Dev 2 |
| **TSK-715** | QA | Test Thuật toán SLA Giờ Làm việc | Viết 40 Unit Tests kiểm tra tính toán SLA qua các mốc thời gian đặc biệt: Đơn nộp giáp Tết, nộp chiều thứ 6, nộp trong giờ nghỉ trưa. | 16 | QA Engineer |
| **TSK-716** | QA | Test Chống SQL Injection Báo cáo Động | Kiểm thử độ an toàn của Dynamic Query Builder với các chuỗi tấn công SQL Injection phức tạp. | 14 | QA Engineer |
| **TSK-717** | QA | Test Báo cáo Định kỳ & Xuất File | Kiểm tra worker lập lịch xuất báo cáo Excel, kiểm tra định dạng file mở trên Microsoft Excel không bị lỗi font hay công thức. | 12 | QA Engineer |
| **TỔNG** | | | **Tổng số 17 Tasks Kỹ thuật** | **220h** | |

---

## 4. Kế hoạch Đánh giá & Tiêu chí Release Sprint 07

### 4.1. Sprint Review / Demo Checklist
- [ ] **Demo 1:** Mở Executive BI Dashboard $\rightarrow$ Các widget số liệu (Tổng đơn nộp, Tỷ lệ tuân thủ SLA toàn tập đoàn, Tổng tiền đã duyệt) tải tức thì dưới 50ms nhờ bảng Materialized Metrics.
- [ ] **Demo 2:** Xem Biểu đồ Heatmap Quy trình $\rightarrow$ Hệ thống tự động highlight màu đỏ ở bước "Kế toán trưởng phê duyệt" kèm cảnh báo "Phát hiện điểm nghẽn SLA vượt 200%".
- [ ] **Demo 3:** Trưởng phòng mở xem Scorecard nhân viên $\rightarrow$ Xem chi tiết từng nhân viên: ai xử lý nhanh nhất, ai có nhiều task tồn đọng nhất.
- [ ] **Demo 4:** Tự tạo một báo cáo động: Chọn nguồn "Đề nghị Thanh toán", kéo thả cột "Mã phiếu", "Người nộp", "Số tiền", "Trạng thái", chọn điều kiện "Số tiền > 100 triệu" $\rightarrow$ Bấm Thực thi, dữ liệu lọc chuẩn xác $\rightarrow$ Bấm Xuất Excel tải file định dạng đẹp mắt.
- [ ] **Demo 5:** Kích hoạt trigger chạy Scheduled Report $\rightarrow$ Hòm thư email nhận được thư kèm file Excel báo cáo tổng hợp.

### 4.2. Đánh giá Rủi ro Sprint 07
| Rủi ro Kỹ thuật | Mức độ | Biện pháp Phòng ngừa |
|---|:---:|---|
| Báo cáo động người dùng chọn query quá nặng gây chậm DB | Cao | Giới hạn tối đa 5,000 dòng/lần xuất trực tiếp; nếu lớn hơn buộc chuyển sang Export Job chạy ngầm. |
| HR quên cập nhật lịch nghỉ lễ trong năm mới | Trung bình | Có cảnh báo nhắc nhở Quản trị viên trên Dashboard vào ngày 15/12 hàng năm để nhập lịch nghỉ lễ năm kế tiếp. |

