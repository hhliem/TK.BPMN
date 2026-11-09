# 🏢 Frontend Sprint 02: Organization Tree, Employee Directory & Master Data

---

## 1. Thông tin Chung về FE-Sprint 02

- **Tên Sprint:** Frontend Sprint 02 — Interactive Org Tree, Employee Directory & Access Governance UI
- **Thời gian Thực hiện:** Tuần 3 - Tuần 4 (10 ngày làm việc)
- **Mục tiêu Sprint (Sprint Goal):**
  > Xây dựng giao diện Sơ đồ Cây Cơ cấu Tổ chức Tập đoàn tương tác trực quan (Zoom/Pan/Lọc), Danh bạ Nhân sự Doanh nghiệp với công nghệ Virtual Scrolling (hiển thị mượt mà trên 1,000 nhân viên, tìm kiếm P99 < 30ms), Phân hệ Ủy quyền Phê duyệt (Delegation) và Ma trận Phân quyền Vai trò (Role-Permission Matrix) tích hợp đồng bộ với Backend Modular Monolith schema `[org]` và `[auth]`.
- **Tổng Story Points cam kết:** **45 SP**
- **Tổng Giờ Kỹ thuật khả dụng (Capacity):** **225 Giờ** (Team 3 Frontend Devs + 1 QA)

---

## 2. Sprint Backlog & Acceptance Criteria (Gherkin)

### FE-US-08: Sơ đồ Cây Cơ cấu Tổ chức Trực quan (Interactive Org Tree) (13 SP)
- **User Story:** Là Giám đốc Nhân sự / Quản trị viên, tôi muốn xem toàn bộ cây cơ cấu tổ chức tập đoàn dạng đồ thị tương tác có thể phóng to, thu nhỏ và tìm kiếm phòng ban nhanh chóng.
- **Acceptance Criteria:**
  ```gherkin
  Scenario: Xem và điều hướng cây cơ cấu tổ chức
    Given người dùng truy cập trang /org/chart
    When hệ thống tải cấu trúc cây phân cấp từ API GET /api/org/units/tree
    Then sơ đồ cây hiển thị đầy đủ các cấp: Tập đoàn -> Chi nhánh -> Khối -> Ban -> Phòng -> Đội
    And người dùng có thể kéo thả để pan, cuộn chuột để zoom in/zoom out
    And nhấp đúp vào một nút đơn vị để thu gọn hoặc mở rộng các nút con trực thuộc.

  Scenario: Tìm kiếm và làm nổi bật đơn vị trên cây
    Given người dùng gõ từ khóa "Ban Công nghệ Thông tin" vào ô tìm kiếm
    When người dùng bấm Enter
    Then cây tổ chức tự động mở các nhánh dẫn đến Ban CNTT
    And đồ thị tự động chuyển tâm (center viewport) và làm nổi bật viền màu sáng vào nút đó.
  ```

### FE-US-09: Danh bạ Nhân sự Doanh nghiệp với Virtual Scrolling (13 SP)
- **User Story:** Là nhân viên công ty, tôi muốn tra cứu thông tin liên lạc của đồng nghiệp trong danh bạ với tốc độ tức thời và cuộn mượt mà ngay cả khi danh sách có hàng ngàn hồ sơ.
- **Acceptance Criteria:**
  ```gherkin
  Scenario: Cuộn mượt danh bạ 1,000+ nhân viên với Virtual Scrolling
    Given danh bạ doanh nghiệp có hơn 1,000 hồ sơ nhân sự
    When người dùng cuộn nhanh danh sách từ trên xuống dưới
    Then chỉ các DOM nodes hiển thị trên khung nhìn viewport (khoảng 15-20 nodes) được render
    And tốc độ khung hình duy trì ổn định $\ge 60\text{ FPS}$, không có hiện tượng giật lag (frame drop)
    And ô tìm kiếm theo tên/email áp dụng debounce 200ms cho thời gian phản hồi lọc dữ liệu P99 < 30ms.
  ```

### FE-US-10: Thiết lập Ký thay & Ủy quyền Phê duyệt (Delegation Modal) (8 SP)
- **User Story:** Là Trưởng phòng chuẩn bị đi công tác, tôi muốn thiết lập ủy quyền ký duyệt hồ sơ cho Phó phòng trong một khoảng thời gian xác định kèm phạm vi loại đơn cụ thể.
- **Acceptance Criteria:**
  ```gherkin
  Scenario: Tạo mới ủy quyền phê duyệt thành công
    Given người dùng mở Modal "Thiết lập Ký thay / Ủy quyền"
    When người dùng chọn Người được ủy quyền là "Phó phòng", chọn Khoảng ngày từ ngày A đến ngày B
    And chọn Phạm vi nghiệp vụ "Đề nghị Thanh toán" kèm hạn mức tối đa "50,000,000 VNĐ"
    And bấm nút "Lưu ủy quyền"
    Then API POST /api/org/delegations được gọi và trả về HTTP 201 Created
    And danh sách ủy quyền đang hoạt động được cập nhật kèm Badge trạng thái "Đang hiệu lực".
  ```

### FE-US-11: Ma trận Phân quyền Vai trò (Role-Permission Matrix) (11 SP)
- **User Story:** Là Quản trị viên Bảo mật, tôi muốn quản lý phân quyền các vai trò theo dạng ma trận trực quan để dễ dàng cấp phát hoặc thu hồi quyền hạt nhân chỉ bằng cách tích chọn.
- **Acceptance Criteria:**
  ```gherkin
  Scenario: Cập nhật quyền hạn theo ma trận
    Given Quản trị viên mở trang /auth/role-matrix
    When tích chọn quyền "workflow:task:approve" cho vai trò "TRUONG_PHONG"
    And bấm nút "Lưu thay đổi"
    Then hệ thống hiển thị Dialog xác nhận thay đổi chính sách bảo mật
    And sau khi xác nhận, API PUT /api/auth/roles/{id}/permissions cập nhật thành công
    And Toast hiển thị thông báo "Đã cập nhật quyền hạn và xóa cache tức thì thành công".
  ```

---

## 3. Danh mục Task Kỹ thuật Chi tiết (Tasks Breakdown)

| Mã Task | Phân hệ / Tầng | Hạng mục Kỹ thuật | Mô tả Chi tiết Công việc Cần Code | Giờ (h) | Người Phụ trách |
|:---:|:---:|---|---|:---:|:---:|
| **TSK-FE-201** | Org Chart | Interactive Tree Canvas | Dựng component `OrgTreeCanvas.vue` sử dụng Vue Flow / SVG Renderer, hỗ trợ Pan, Zoom, MiniMap và auto-layout phân cấp. | 24 | Frontend Dev 1 |
| **TSK-FE-202** | Org Chart | Node Card & Expand/Collapse | Dựng component `OrgNodeCard.vue` hiển thị logo đơn vị, tên phòng, trưởng phòng, số lượng nhân sự, trạng thái thu gọn/mở rộng. | 16 | Frontend Dev 1 |
| **TSK-FE-203** | Org Chart | Unit Drawer & CRUD Forms | Dựng `OrgUnitDrawer.vue` trượt từ phải sang để xem thông tin chi tiết, sửa tên đơn vị, đổi mã phòng ban, chỉ định trưởng phòng. | 16 | Frontend Dev 2 |
| **TSK-FE-204** | Directory | Virtual Scroller Setup | Tích hợp `@tanstack/vue-virtual` hoặc `vue-virtual-scroller` dựng `EmployeeDirectoryView.vue` với khả năng render mượt 1,000+ nhân viên. | 18 | Frontend Dev 2 |
| **TSK-FE-205** | Directory | Fast Search & Filter Panel | Dựng thanh bộ lọc đa tiêu chí: Phòng ban (Tree Select), Chức danh, Trạng thái (Chính thức, Thử việc), Debounce search P99 < 30ms. | 14 | Frontend Dev 3 |
| **TSK-FE-206** | Directory | Employee Profile Card | Dựng `EmployeeProfileModal.vue` hiển thị avatar, email, điện thoại, chi nhánh, cây cấp trên trực tiếp (Reporting Manager) và hạn mức duyệt. | 12 | Frontend Dev 3 |
| **TSK-FE-207** | Delegation | Delegation Table View | Dựng `DelegationListView.vue` hiển thị danh sách các hồ sơ ủy quyền: Người ủy quyền, Người nhận, Thời gian, Phạm vi, Trạng thái hoạt động. | 14 | Frontend Dev 2 |
| **TSK-FE-208** | Delegation | Delegation Form Modal | Dựng `CreateDelegationModal.vue` với DateRangePicker, Dropdown chọn nhân viên, Multi-select chọn luồng quy trình và ô nhập hạn mức tài chính. | 16 | Frontend Dev 3 |
| **TSK-FE-209** | Access Matrix | Role-Permission Grid | Dựng `RolePermissionMatrix.vue` ma trận 2 chiều (Cột: Roles, Hàng: Permissions gom nhóm theo Module), Sticky header & Sticky first column. | 22 | Frontend Dev 1 |
| **TSK-FE-210** | Access Matrix | Batch Action & Confirm Dialog | Cơ chế gom các thay đổi quyền chưa lưu (Dirty State Indicator) và Dialog xác nhận bảo mật kèm cảnh báo thu hồi quyền người dùng. | 14 | Frontend Dev 2 |
| **TSK-FE-211** | QA Testing | Unit Tests Org & Directory | Viết bộ kiểm thử Unit Tests cho OrgTree helper (flatten, search nodes) và Virtual Scroller calculation logic (>30 test cases). | 18 | QA Engineer |
| **TSK-FE-212** | QA Testing | E2E Tests Org & Delegation | Kịch bản E2E: Tìm kiếm phòng ban trên cây -> Mở hồ sơ nhân viên -> Thiết lập ủy quyền mới -> Kiểm tra hiển thị danh sách. | 20 | QA Engineer |
| **TỔNG** | | | **Tổng số 12 Tasks Kỹ thuật** | **204h** | |

---

## 4. Kế hoạch Đánh giá & Tiêu chí Release FE-Sprint 02

### 4.1. Sprint Review / Demo Checklist
- [ ] **Demo 1:** Mở màn hình Sơ đồ Cây Tổ chức, thực hiện Pan/Zoom mượt mà trên canvas; bấm nút "Thu gọn tất cả" và "Mở rộng cấp 2" đồ thị thích ứng tức thì.
- [ ] **Demo 2:** Gõ từ khóa tìm kiếm phòng ban, đồ thị tự động chuyển tiêu điểm (auto-pan) đến đúng vị trí phòng ban đó và phát sáng đường biên.
- [ ] **Demo 3:** Mở Danh bạ Nhân sự với 1,000 bản ghi mẫu, cuộn liên tục chuột từ đầu đến cuối không xuất hiện hiện tượng giật màn hình hoặc đơ trình duyệt (duy trì 60 FPS).
- [ ] **Demo 4:** Thiết lập một hồ sơ ủy quyền phê duyệt với ngày hiệu lực trong tương lai $\rightarrow$ Bảng danh sách hiển thị Badge màu cam "Sắp hiệu lực"; hồ sơ trong khoảng ngày hiện tại hiển thị Badge màu xanh lá "Đang hiệu lực".
- [ ] **Demo 5:** Trên Ma trận Phân quyền, tích bỏ quyền duyệt của một vai trò và bấm "Lưu" $\rightarrow$ Hệ thống gửi request thành công và hiển thị phản hồi tức thì.

### 4.2. Đánh giá Rủi ro Kỹ thuật FE-Sprint 02
| Rủi ro Kỹ thuật | Mức độ | Biện pháp Phòng ngừa |
|---|:---:|---|
| **Cây tổ chức quá lớn làm chậm DOM:** Khi tập đoàn có hàng trăm chi nhánh | Cao | Triển khai cơ chế Virtual Tree hoặc chỉ nạp cây ở dạng lazy-load khi người dùng nhấp mở nút con. |
| **Bảng Ma trận phân quyền tràn màn hình:** Hàng trăm quyền x hàng chục vai trò gây lag scroll | Trung bình | Cố định (sticky) hàng tiêu đề và cột tên quyền bằng CSS `position: sticky`, phân nhóm quyền theo từng Accordion Module thu gọn. |

