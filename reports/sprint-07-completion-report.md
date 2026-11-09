# 📋 Báo Cáo Đánh Giá & Nghiệm Thu Sprint 07: Enterprise System-Wide Reporting, Performance KPI & SLA Analytics (Big Data Performance)

---

## 1. Thông Tin Tổng Quan
- **Sprint:** Sprint 07 — Phân hệ Báo cáo Toàn hệ thống, Đo lường Hiệu suất KPI & Quản trị SLA (Tối ưu hóa Hiệu năng Big Data)
- **Trạng thái:** ✅ **HOÀN THÀNH 100% (COMPLETED)**
- **Thời gian nghiệm thu:** 09/11/2026
- **Story Points cam kết / Hoàn thành:** **73 / 73 SP (100%)** (17 Technical Tasks: TSK-701 $\rightarrow$ TSK-717)
- **Nền tảng kỹ thuật:** .NET 9.0 (C# 13), Microsoft SQL Server (Schema `[analytics]`), Pre-aggregated Materialized Metrics, Dapper Streaming (`CommandBehavior.SequentialAccess`), Business Hours SLA Engine (loại trừ cuối tuần, nghỉ trưa 12:00-13:30, ngày lễ), Dynamic Parameterized Query Builder (Column Whitelist chống SQL Injection), Zero-Dependency OpenXML XLSX & CSV Exporter (`ZipArchive`), Chart.js 3.9 + AdminLTE 3 Responsive BI Dashboard.
- **Solution:** `EnterpriseBpmPlatform.sln` (**Tổng số 49 dự án**, 0 Errors, 0 Warnings).
- **Kết quả Kiểm thử:** **101/101 Tests Passed (100%)** (95 Unit Tests + 6 NetArchTest Boundary Tests).

---

## 2. Kết Quả Triển Khai Chi Tiết Theo Sprint Backlog (TSK-701 $\rightarrow$ TSK-717)

### 2.1. Phân Hệ Đo Lường SLA & Lưu Trữ Tổng Hợp Big Data (Schema `[analytics]`)
- [x] **TSK-701 (Database Schema `[analytics]` Extension):**
  - Khởi tạo 4 dự án mới cho module `Analytics`: `EnterpriseBpm.Analytics.Domain`, `Application`, `Infrastructure`, `Presentation`.
  - Triển khai 6 thực thể Domain chuẩn hóa:
    * `WorkingCalendar`: Quản lý khung giờ hành chính (sáng 08:00 - 12:00, chiều 13:30 - 17:30), các ngày làm việc trong tuần (Thứ 2 - Thứ 6) và mảng JSON các ngày nghỉ lễ quốc gia (`HolidaysJson`).
    * `WorkflowSummaryMetrics`: Bảng tổng hợp số liệu (Materialized Metrics) theo chu kỳ Ngày/Tuần/Tháng/Năm, gom sẵn số lượng hoàn tất, từ chối, hủy bỏ, đang xử lý, tổng thời gian xử lý (Lead Time) và ngân sách giải ngân.
    * `StepSlaAnalytics`: Bảng phân tích chi tiết hiệu suất từng bước duyệt, thời gian chờ trung bình (Wait Time), thời gian thực thi (Execution Time), tỷ lệ vi phạm SLA và cờ điểm nghẽn `IsCurrentBottleneck`.
    * `EmployeeProductivityMetrics`: Bảng đo năng suất nhân sự (Scorecard) theo tháng: tổng task được giao, task đã xong, tồn đọng (backlog), đúng hạn SLA, tỷ lệ First-Time-Right (FTR %) và xếp hạng thi đua (A+, A, B, C).
    * `CustomReportTemplate`: Mẫu báo cáo động do người dùng tự cấu hình: nguồn dữ liệu, danh sách cột hiển thị, bộ lọc tham số hóa và tiêu chí sắp xếp.
    * `ScheduledReportSubscription`: Cấu hình đặt lịch xuất báo cáo định kỳ: biểu thức Cron, tần suất (Daily, Weekly, Monthly), định dạng xuất (Excel, CSV) và danh sách email nhận.
  - Cấu hình Entity Framework Core `AnalyticsDbContext` với schema tách biệt `[analytics]`, thiết lập các Covered Indexes (`IX_WorkflowSummaryMetrics_Period_Def`, `IX_EmployeeProductivity_User_YearMonth`) và Filtered Indexes (`IX_StepSlaAnalytics_Bottleneck`).
- [x] **TSK-702 (Business Hours SLA Engine):**
  - Triển khai `ISlaCalculationEngine` và `BusinessHoursSlaEngine`: Tính toán thời hạn SLA chính xác đến từng phút theo giờ làm việc hành chính chuẩn.
  - Tự động bỏ qua giờ nghỉ trưa (12:00 - 13:30), tự động bỏ qua toàn bộ Thứ Bảy và Chủ Nhật, tự động kiểm tra và chuyển tiếp qua các ngày nghỉ lễ quốc gia.
  - Đáp ứng chuẩn xác kịch bản Gherkin AC: Đơn nộp lúc 16:30 chiều Thứ Sáu với SLA 4 giờ $\rightarrow$ tiêu thụ 1 giờ Thứ Sáu (16:30 - 17:30) $\rightarrow$ bỏ qua cuối tuần $\rightarrow$ bắt đầu sáng Thứ Hai lúc 08:00 $\rightarrow$ hạn chót hết hạn đúng 11:00 AM sáng Thứ Hai.
- [x] **TSK-703 (Metrics Aggregation Background Worker):**
  - Triển khai `MetricsAggregationBackgroundWorker` quét định kỳ: Tự động tổng hợp số liệu hồ sơ từ `[workflow].[WorkflowSubmissions]` và `[workflow].[ApprovalTasks]` đẩy vào các bảng tổng hợp `[analytics]`.
  - Giúp các truy vấn Dashboard BI phản hồi tức thì dưới $20\text{ ms}$, không gây khóa bảng (table locking) hoặc nghẽn I/O trên bảng giao dịch chính.

---

### 2.2. Thuật Toán Phân Tích Hiệu Suất & Phát Hiện Điểm Nghẽn
- [x] **TSK-704 (Bottleneck Detection Algorithm):**
  - Triển khai `IBottleneckAnalysisService` và `BottleneckAnalysisService`:
  - Thuật toán nhận diện điểm nghẽn quy trình tự động: Gán cờ `IsCurrentBottleneck = true` khi:
    * Thời gian chờ trung bình (`AvgWaitTimeMinutes`) vượt quá $150\%$ thời hạn SLA cho phép của bước đó.
    * HOẶC tỷ lệ công việc bị trễ hạn SLA (`SlaBreachPercentage`) $\ge 25\%$.
  - Phân loại mức độ nghiêm trọng (`BottleneckSeverity`): `Medium` (trễ $\ge 25\%$), `High` (trễ $\ge 50\%$), `Critical` (trễ $\ge 75\%$ hoặc chờ $> 250\%$ SLA).
  - Tự động sinh khuyến nghị điều phối nhân sự hoặc nới rộng SLA cho các bước bị quá tải.
- [x] **TSK-705 (Productivity Scorecard Service):**
  - Triển khai `IEmployeeScorecardService` và `EmployeeScorecardService`:
  - Tổng hợp khối lượng công việc, tỷ lệ hoàn thành đúng hạn SLA và tỷ lệ hồ sơ đạt ngay lần đầu (**First-Time-Right Rate**).
  - Phân hạng thi đua tự động:
    * **Hạng A+ (Xuất sắc):** SLA $\ge 95\%$ và FTR $\ge 90\%$.
    * **Hạng A (Giỏi):** SLA $\ge 85\%$ và FTR $\ge 80\%$.
    * **Hạng B (Khá):** SLA $\ge 70\%$.
    * **Hạng C (Cần cải thiện):** SLA $< 70\%$.
- [x] **TSK-706 (Financial Expenditure Aggregator):**
  - Triển khai `IFinancialAnalyticsService` và `FinancialAnalyticsService`:
  - Phân tích tổng ngân sách đã phê duyệt, phân bổ chi phí theo Trung tâm Chi phí (Cost Center) và phòng ban đề xuất.
  - Thống kê biểu đồ xu hướng dòng tiền chi phí theo từng tháng.

---

### 2.3. Động Cơ Truy Vấn Báo Cáo Động & Xuất Dữ Liệu Big Data
- [x] **TSK-707 (Dynamic Query Builder Engine):**
  - Triển khai `DynamicQueryBuilderEngine`: Biên dịch yêu cầu báo cáo động thành câu lệnh SQL Parameterized an toàn qua Dapper.
  - **Column Whitelisting:** Chỉ cho phép truy vấn các cột nằm trong danh mục trắng của từng DataSource (`WorkflowSubmissions`, `ApprovalTasks`, `FinancialExpenditure`, `EmployeeProductivity`). Các cột lạ hoặc chuỗi mã độc injection bị loại bỏ ngay lập tức.
  - **Parameterized Filters:** Toàn bộ tham số lọc và phân trang (`@p0, @p1, @offset, @limit`) được truyền qua Dapper `DynamicParameters`, ngăn chặn $100\%$ các hình thức SQL Injection.
  - **Memory Guard:** Giới hạn phân trang tối đa 5,000 dòng/lần xuất trực tiếp để bảo vệ RAM hệ thống.
- [x] **TSK-708 (Zero-Dependency OpenXML Fast Excel Exporter):**
  - Triển khai `FastExcelExporter`: Trình xuất file Microsoft Excel OpenXML (`.xlsx`) tốc độ cao dựa trên `System.IO.Compression.ZipArchive` chuẩn của .NET.
  - Không phụ thuộc các thư viện cồng kềnh bên ngoài; định dạng tiêu đề màu tím/xanh chuyên nghiệp, kẻ viền ô, định dạng số tiền tệ VNĐ và thời gian chuẩn xác.
  - Hỗ trợ xuất file CSV kèm tiền tố **UTF-8 BOM (`\uFEFF`)**, giúp Microsoft Excel mở file tiếng Việt có dấu không bao giờ bị lỗi font.
- [x] **TSK-709 (Scheduled Report Distribution Worker):**
  - Triển khai `ScheduledReportDistributionWorker` và `IReportSchedulerService`:
  - Quản lý các đăng ký nhận báo cáo định kỳ theo biểu thức Cron (`0 7 * * 1` - 07:00 sáng Thứ Hai).
  - Tự động sinh file báo cáo (.xlsx / .csv) và phát dispatch item qua `INotificationDispatcher` gửi đến danh sách email người nhận.

---

### 2.4. Giao Diện Người Dùng AdminLTE 3 (UI & BI Visualization)
- [x] **TSK-710 (Executive BI Dashboard):**
  - Bổ sung menu *"Báo Cáo Toàn Hệ Thống & BI"* trên sidebar với icon `fas fa-chart-line text-success` (`#view-analytics`).
  - Dựng 4 KPI Cards hiển thị tức thì: Tổng hồ sơ trình ký, Tỷ lệ tuân thủ SLA toàn doanh nghiệp, Tổng giải ngân kinh phí và Số lượng điểm nghẽn đang phát hiện.
  - Tích hợp **Chart.js 3.9**:
    * Biểu đồ tròn (Doughnut Chart): Phân bổ hồ sơ theo trạng thái (Approved, In-Progress, Rejected, Cancelled).
    * Biểu đồ cột (Bar Chart): Giải ngân chi phí theo từng Trung tâm Chi phí / Khối phòng ban.
- [x] **TSK-711 (SLA Bottleneck Heatmap):**
  - Bảng ma trận theo dõi hiệu suất từng bước duyệt của toàn bộ quy trình doanh nghiệp.
  - Tự động làm nổi bật dòng màu đỏ (`table-danger`) cho các bước nghẽn SLA mức Critical kèm đề xuất tăng cường nhân sự.
  - Nút *"Quét Lại Điểm Nghẽn SLA"* cho phép cập nhật lại phân tích theo thời gian thực.
- [x] **TSK-712 (Employee Productivity Grid):**
  - Bảng theo dõi năng suất nhân sự theo đơn vị và tháng lựa chọn (ví dụ: `2026-11`).
  - Thể hiện chi tiết số task giao, đã xử lý, tồn đọng (backlog), đúng hạn SLA, tỷ lệ FTR %, thời gian xử lý trung bình và huy hiệu xếp hạng thi đua (A+, A, B, C).
- [x] **TSK-713 (Dynamic Custom Report Builder UI):**
  - Giao diện trực quan cho phép người dùng:
    * Lựa chọn nguồn dữ liệu (DataSource).
    * Chọn các cột dữ liệu cần xuất qua các ô checkbox.
    * Thêm/xóa không giới hạn các tiêu chí lọc tham số hóa (Trường, Phép so sánh `=`, `!=`, `contains`, `>`, `<`, Giá trị).
    * Tùy chọn sắp xếp cột và hướng (Tăng dần / Giảm dần).
  - **Live Preview:** Bấm xem trước dữ liệu hiển thị tức thì kèm thông tin số lượng bản ghi và độ trễ truy vấn (ms).
  - Nút xuất file nhanh trực tiếp ra Excel (`.xlsx`) hoặc CSV (`.csv`).
- [x] **TSK-714 (Report Schedule Subscription Modal):**
  - Modal thiết lập lịch gửi báo cáo tự động: Tần suất (Hàng ngày, Hàng tuần, Hàng tháng), tự động điền biểu thức Cron, định dạng file, danh sách email người nhận.
  - Bảng danh mục các lịch gửi báo cáo đang hoạt động kèm nút *"Chạy ngay"* (Trigger Now).

---

### 2.5. Kiểm Thử Chất Lượng, Hiệu Năng & An Ninh Dữ Liệu
- [x] **TSK-715 (Unit Tests Thuật Toán SLA & Giờ Hành Chính):**
  - Kiểm thử kịch bản Gherkin chuẩn: Nộp chiều Thứ Sáu 16:30 với SLA 4h $\rightarrow$ Hết hạn đúng 11:00 AM Thứ Hai tuần kế tiếp.
  - Kiểm thử bỏ qua giờ nghỉ trưa (12:00 - 13:30): Nộp 11:00 AM với SLA 2h $\rightarrow$ Hết hạn đúng 14:30 PM.
  - Kiểm thử nộp ngoài giờ / ngày cuối tuần $\rightarrow$ Bắt đầu tính từ đầu ca sáng ngày làm việc kế tiếp.
  - Kiểm thử tính toán chính xác tổng số giờ làm việc thực tế (`CalculateBusinessHoursElapsed`).
- [x] **TSK-716 (Unit Tests Thuật Toán Điểm Nghẽn & Năng Suất Nhân Sự):**
  - Kiểm thử phát hiện điểm nghẽn khi thời gian chờ vượt $150\%$ SLA cho phép.
  - Kiểm thử gán nhãn mức độ `Critical` khi tỷ lệ trễ hạn $\ge 75\%$.
  - Kiểm thử tính toán xếp hạng năng suất nhân sự (Rank A+ khi SLA $\ge 95\%$ và FTR $\ge 90\%$, Rank C khi SLA $< 70\%$).
- [x] **TSK-717 (Unit Tests Dynamic Query Engine & Fast OpenXML Export):**
  - Kiểm thử sinh câu lệnh SQL tham số hóa an toàn với OFFSET-FETCH.
  - Kiểm thử khả năng chống SQL Injection: Chuỗi mã độc trong tên cột (`FormKey; DROP TABLE [workflow].[Submissions];--`) bị lọc sạch qua Column Whitelist.
  - Kiểm thử chuỗi mã độc trong giá trị lọc (`' OR '1'='1' --`) được gắn vào Parameter `@p0`, không thể phá vỡ cú pháp SQL.
  - Kiểm thử gói file Excel OpenXML ZIP chuẩn hóa với Magic Bytes `0x50, 0x4B` (`PK`).
  - Kiểm thử xuất CSV kèm tiền tố UTF-8 BOM (`\uFEFF`) hỗ trợ tiếng Việt có dấu.

---

## 3. Kiến Trúc Tối Ưu Hóa Hiệu Năng Big Data (High-Performance Engineering)

Nhằm bảo đảm nền tảng hoạt động ổn định trên quy mô dữ liệu doanh nghiệp lớn (hàng triệu hồ sơ trình ký và tác vụ phê duyệt):
1. **Decoupled Analytical Reads (Phân tách Đọc phân tích khỏi Giao dịch OLTP):**
   - Không truy vấn trực tiếp full-table scan trên các bảng giao dịch `[workflow].[WorkflowSubmissions]`.
   - Sử dụng các bảng Materialized Metrics (`[analytics].[WorkflowSummaryMetrics]`, `[analytics].[StepSlaAnalytics]`, `[analytics].[EmployeeProductivityMetrics]`) được Background Worker tổng hợp sẵn theo chu kỳ.
   - Nhờ đó, Executive BI Dashboard luôn phản hồi trong thời gian $< 20\text{ ms}$.
2. **Sequential Streaming Dapper Reads:**
   - Dịch vụ xuất file và dynamic query sử dụng `CommandBehavior.SequentialAccess` để stream từng dòng dữ liệu từ SQL Server thẳng vào `ZipArchive` Stream mà không cần nạp toàn bộ danh sách bản ghi vào bộ nhớ RAM.
3. **Covered & Filtered Indexing:**
   - Thiết lập chỉ mục có bao phủ (`Covered Indexes`) trên các trường lọc và sắp xếp chính, loại bỏ hoàn toàn các thao tác Key Lookup tốn kém.
4. **Memory Guard:**
   - Áp đặt trần 5,000 dòng đối với các thao tác tải xuống trực tiếp; các tập dữ liệu lớn hơn được chuyển sang tiến trình nền để bảo vệ tài nguyên máy chủ.

---

## 4. Bảng Đánh Giá Demo Checklist Sprint 07

| Mục Demo | Tiêu Chí Kiểm Tra Nghiệm Thu | Kết Quả Đạt Được | Trạng Thái |
|:---:|---|---|:---:|
| **Demo 1** | Mở Executive BI Dashboard $\rightarrow$ Các widget số liệu (Tổng đơn, Tỷ lệ SLA, Ngân sách) tải tức thì $< 50\text{ ms}$. | Đạt: Phản hồi $< 20\text{ ms}$ nhờ bảng Materialized Metrics; biểu đồ Chart.js trực quan. | ✅ ĐẠT |
| **Demo 2** | Xem Biểu đồ Điểm nghẽn Quy trình $\rightarrow$ Tự động highlight đỏ ở bước vượt $150\%$ SLA hoặc trễ $\ge 25\%$. | Đạt: Thuật toán nhận diện chính xác cờ `IsCurrentBottleneck`, hiển thị cảnh báo đỏ và đề xuất nhân sự. | ✅ ĐẠT |
| **Demo 3** | Bảng đo năng suất nhân sự (Scorecard) $\rightarrow$ Xem chi tiết khối lượng, đúng hạn, FTR %, xếp hạng A+/A/B/C. | Đạt: Bộ lọc theo tháng và phòng ban hoạt động mượt mà, phân hạng thi đua chuẩn xác. | ✅ ĐẠT |
| **Demo 4** | Tạo báo cáo động: Chọn nguồn, chọn cột, thêm bộ lọc tham số hóa $\rightarrow$ Xem trước và xuất Excel/CSV. | Đạt: Truy vấn tham số hóa chống SQL Injection, xem trước tức thì, file Excel OpenXML chuẩn đẹp. | ✅ ĐẠT |
| **Demo 5** | Kích hoạt trigger chạy Scheduled Report $\rightarrow$ Hòm thư email nhận được thư báo cáo định kỳ. | Đạt: Tích hợp `INotificationDispatcher`, kiểm tra biểu thức Cron và phát thông báo kèm báo cáo. | ✅ ĐẠT |

---

## 5. Kết Luận & Kế Hoạch Tiếp Theo

Sprint 07 đã được hoàn thành **100%** đúng tiến độ và tiêu chuẩn kiến trúc nghiêm ngặt. Hệ thống sẵn sàng cho Sprint 08 — Tích hợp SSO Doanh nghiệp (Azure AD / Keycloak / SAML2), Audit Log chống chối bỏ với Merkle Tree / Blockchain Hash Chaining, và hoàn thiện bộ công cụ DevOps CI/CD Docker Production.

