# 📊 Frontend Sprint 07: Executive BI Dashboard & Dynamic Reporting

---

## 1. Thông tin Chung về FE-Sprint 07

- **Tên Sprint:** Frontend Sprint 07 — Executive BI Dashboard, Bottleneck Heatmap & Dynamic Report Builder
- **Thời gian Thực hiện:** Tuần 13 - Tuần 14 (10 ngày làm việc)
- **Mục tiêu Sprint (Sprint Goal):**
  > Xây dựng Bảng điều khiển Giám đốc (Executive BI Dashboard) theo bố cục Bento Grid hiện đại từ Skill 02 với 4 thẻ KPI cốt lõi, tích hợp biểu đồ Apache ECharts trực quan hóa Bản đồ nhiệt Nút thắt Cổ chai (Bottleneck Heatmap) phát hiện các bước duyệt chậm trễ, Bảng xếp hạng Năng suất Nhân sự (Productivity Scorecards), và Trình dựng Báo cáo Động (Dynamic Report Builder) cho phép tự chọn cột lọc dữ liệu kèm tính năng xuất Excel chuẩn hóa 1-click.
- **Tổng Story Points cam kết:** **55 SP**
- **Tổng Giờ Kỹ thuật khả dụng (Capacity):** **275 Giờ** (Team 3 Frontend Devs + 1 QA)

---

## 2. Sprint Backlog & Acceptance Criteria (Gherkin)

### FE-US-34 & FE-US-35: Dashboard Điều hành Bento Grid & Bản đồ Nhiệt Cổ chai (26 SP)
- **User Story:** Là Ban Lãnh đạo, tôi muốn theo dõi tổng thể sức khỏe vận hành quy trình doanh nghiệp trên một màn hình trực quan, nhanh chóng phát hiện khâu nào hoặc phòng ban nào đang gây ách tắc hồ sơ.
- **Acceptance Criteria:**
  ```gherkin
  Scenario: Xem các chỉ số KPI điều hành trên Bento Grid
    Given Lãnh đạo truy cập trang Dashboard Điều hành /analytics/dashboard
    When hệ thống nạp dữ liệu từ API GET /api/analytics/kpis?period=month
    Then 4 thẻ KPI Bento Cards hiển thị sắc nét:
      | Chỉ số | Giá trị Mục tiêu | Màu sắc Trạng thái |
      | Tỷ lệ duyệt đúng hạn | $\ge 92\%$ | Xanh lá (Emerald) |
      | Thời gian xử lý trung bình | $\le 1.8\text{ ngày}$ | Xanh dương (Sky) |
      | Tổng số hồ sơ tiếp nhận | Số lượng thực tế | Chàm (Indigo) |
      | Hồ sơ đang trễ hạn SLA | Số lượng thực tế | Đỏ cảnh báo (Rose) |
    And mỗi thẻ có mũi tên chỉ báo tăng/giảm % so với tháng trước.

  Scenario: Trực quan hóa điểm nghẽn quy trình bằng Bottleneck Heatmap
    Given người dùng xem biểu đồ Bản đồ nhiệt quy trình
    When biểu đồ Apache ECharts Heatmap được render
    Then trục tung thể hiện các Loại quy trình, trục hoành thể hiện các Bước phê duyệt
    And các ô có thời gian chờ duyệt lâu nhất (trên 24 giờ) tự động tô màu đỏ đậm rực rỡ
    And rê chuột (hover) vào từng ô hiển thị Tooltip chi tiết: Thời gian trung bình, Số hồ sơ tồn đọng và Người phụ trách chính.
  ```

### FE-US-36: Bảng Xếp hạng Năng suất Xử lý (Productivity Scorecards) (8 SP)
- **User Story:** Là Trưởng phòng Vận hành, tôi muốn xem bảng xếp hạng năng suất của các chuyên viên để khen thưởng kịp thời và điều phối khối lượng công việc hợp lý.
- **Acceptance Criteria:**
  ```gherkin
  Scenario: Xem và sắp xếp bảng xếp hạng năng suất nhân sự
    Given người dùng mở tab "Năng suất Nhân sự"
    When bảng dữ liệu Scorecard hiển thị
    Then danh sách nhân sự được sắp xếp theo: Tổng hồ sơ đã xử lý, Thời gian xử lý trung bình (giờ), Tỷ lệ đúng hạn (%)
    And có huy hiệu Top 1, Top 2, Top 3 cho những cá nhân xuất sắc nhất
    And người dùng có thể lọc bảng theo Phòng ban cụ thể.
  ```

### FE-US-37 & FE-US-38: Trình dựng Báo cáo Động & Xuất Excel Chuẩn Hóa (21 SP)
- **User Story:** Là Chuyên viên Phân tích Dữ liệu, tôi muốn tự do chọn các cột thông tin cần xuất, đặt các điều kiện lọc và tải về tệp Excel định dạng đẹp mắt chỉ với một cú nhấp chuột.
- **Acceptance Criteria:**
  ```gherkin
  Scenario: Tự dựng báo cáo bằng cách kéo thả chọn cột
    Given người dùng truy cập /analytics/report-builder
    When người dùng chọn nguồn dữ liệu "Hồ sơ Đề nghị Thanh toán"
    And tích chọn các cột: [Mã hồ sơ, Người nộp, Số tiền, Ngày nộp, Người duyệt cuối, Trạng thái]
    And thêm điều kiện lọc: "Số tiền > 10.000.000 VNĐ" và "Trạng thái = Đã duyệt"
    Then bảng xem trước (Live Preview) lập tức cập nhật dữ liệu 20 bản ghi khớp với điều kiện.

  Scenario: Xuất dữ liệu ra tệp Excel (.xlsx) chuẩn doanh nghiệp
    Given người dùng bấm nút "Xuất Excel" trên thanh công cụ
    When hệ thống xử lý xuất dữ liệu
    Then trình duyệt tải về tệp Excel `.xlsx` với tên tệp chuẩn hóa (VD: `BaoCao_HoSoThanhToan_20261109.xlsx`)
    And hàng tiêu đề có nền màu xanh thương hiệu chữ trắng in đậm, các cột số tiền được định dạng phân cách hàng nghìn, ngày tháng định dạng `DD/MM/YYYY`.
  ```

---

## 3. Danh mục Task Kỹ thuật Chi tiết (Tasks Breakdown)

| Mã Task | Phân hệ / Tầng | Hạng mục Kỹ thuật | Mô tả Chi tiết Công việc Cần Code | Giờ (h) | Người Phụ trách |
|:---:|:---:|---|---|:---:|:---:|
| **TSK-FE-701** | BI Dashboard | Bento Grid Layout Shell | Dựng `ExecutiveDashboardView.vue` với lưới Bento Grid tương thích màn hình 1024px, 1440px và 4K. | 18 | Frontend Dev 1 |
| **TSK-FE-702** | BI Dashboard | KPI Metrics Cards | Dựng component `KpiMetricCard.vue` hiển thị số liệu lớn, sparkline thu nhỏ, tỷ lệ tăng trưởng và icon tương phản. | 14 | Frontend Dev 1 |
| **TSK-FE-703** | Data Viz | Apache ECharts Integration | Cấu hình thư viện `vue-echarts`, thiết lập theme màu sắc đồng bộ với Tailwind CSS tokens (Dark/Light mode reactive). | 16 | Frontend Dev 2 |
| **TSK-FE-704** | Data Viz | Bottleneck Heatmap Chart | Dựng `BottleneckHeatmapChart.vue` ma trận Loại đơn x Bước duyệt, tooltip đa thông số, nhấp vào ô để xem danh sách hồ sơ tắc. | 22 | Tech Lead |
| **TSK-FE-705** | Data Viz | Processing Time Trends Chart | Dựng biểu đồ xu hướng đường/cột thể hiện thời gian xử lý hồ sơ qua các tuần trong quý. | 14 | Frontend Dev 2 |
| **TSK-FE-706** | BI Dashboard | Productivity Scorecard Table | Dựng `ProductivityScorecardTable.vue` với thanh progress bar % đúng hạn, avatar nhân viên, bộ lọc phòng ban. | 16 | Frontend Dev 3 |
| **TSK-FE-707** | BI Dashboard | Global Time & Scope Filter | Dựng thanh bộ lọc thời gian: Hôm nay, 7 ngày qua, Tháng này, Quý này, Tùy chọn DateRangePicker và chọn Chi nhánh. | 14 | Frontend Dev 2 |
| **TSK-FE-708** | Report Builder | Column Selector & Reorder | Dựng `DynamicReportBuilderView.vue` với danh sách checkbox chọn cột và kéo thả sắp xếp thứ tự cột (Drag & Drop sorting). | 18 | Frontend Dev 3 |
| **TSK-FE-709** | Report Builder | Dynamic Conditions Engine | Dựng component thiết lập điều kiện lọc nhiều tầng (AND/OR, trường dữ liệu, toán tử so sánh, giá trị nhập). | 20 | Frontend Dev 1 |
| **TSK-FE-710** | Report Builder | Live Preview Table | Dựng bảng xem trước dữ liệu sống 20 dòng kết quả đầu tiên với Skeleton loader khi đang truy vấn. | 14 | Frontend Dev 3 |
| **TSK-FE-711** | Report Builder | Excel Export Engine | Tích hợp xuất Excel chuẩn hóa (sử dụng API streaming Backend hoặc thư viện `exceljs`), tự động căn chỉnh độ rộng cột và style tiêu đề. | 16 | Frontend Dev 2 |
| **TSK-FE-712** | QA Testing | Unit Tests BI Components | Viết Unit Tests cho các hàm tính toán KPI delta %, chuyển đổi dữ liệu ECharts matrix và bộ dựng điều kiện lọc (>35 test cases). | 18 | QA Engineer |
| **TSK-FE-713** | QA Testing | E2E Tests BI & Report Builder | Kịch bản E2E: Mở Dashboard -> Lọc dữ liệu theo tháng -> Mở Trình dựng báo cáo -> Chọn cột -> Xuất file Excel thành công. | 22 | QA Engineer |
| **TỔNG** | | | **Tổng số 13 Tasks Kỹ thuật** | **222h** | |

---

## 4. Kế hoạch Đánh giá & Tiêu chí Release FE-Sprint 07

### 4.1. Sprint Review / Demo Checklist
- [ ] **Demo 1:** Mở trang Dashboard Điều hành, 4 thẻ KPI Bento hiển thị trực quan các con số tổng hợp, biểu đồ sparkline nhỏ chạy mượt mà dưới 1 giây tải trang.
- [ ] **Demo 2:** Trên Bản đồ nhiệt (Heatmap), bước "Phê duyệt Giám đốc Tài chính" của quy trình "Mua sắm Tài sản" hiện màu đỏ đậm; rê chuột vào hiển thị thời gian chờ trung bình 32.5 giờ; nhấp vào ô mở danh sách 5 hồ sơ đang chờ.
- [ ] **Demo 3:** Đổi bộ lọc từ "Tháng này" sang "Quý này" $\rightarrow$ Toàn bộ biểu đồ và thẻ số liệu tự động chuyển động cập nhật dữ liệu mới mượt mà (smooth chart animation).
- [ ] **Demo 4:** Vào Trình dựng Báo cáo Động, bỏ chọn 2 cột và kéo cột "Số tiền" lên vị trí thứ hai $\rightarrow$ Bảng xem trước dữ liệu sống thay đổi thứ tự cột ngay lập tức.
- [ ] **Demo 5:** Bấm nút "Xuất Excel" $\rightarrow$ Tải về tệp `.xlsx`, mở trong Microsoft Excel hiển thị đúng định dạng tiêu đề in đậm nền xanh, số tiền có dấu chấm phân cách hàng nghìn rõ ràng.

### 4.2. Đánh giá Rủi ro Kỹ thuật FE-Sprint 07
| Rủi ro Kỹ thuật | Mức độ | Biện pháp Phòng ngừa |
|---|:---:|---|
| **ECharts render lại làm chậm trang khi đổi kích thước màn hình:** Resize window liên tục | Trung bình | Dùng `ResizeObserver` kết hợp debounce 150ms khi gọi hàm `chartInstance.resize()`. |
| **Báo cáo khối lượng dữ liệu lớn gây tràn RAM trình duyệt:** Khi xuất hàng chục ngàn dòng | Cao | Phân trang chặt chẽ ở bảng preview; khi xuất dữ liệu lớn $> 1,000$ dòng, chuyển hướng gọi API Backend sinh file nền và trả về URL tải tệp. |
