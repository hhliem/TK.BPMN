# 🚀 Sprint 02: Organization Structure, Positions & Form Master Data

---

## 1. Thông tin Chung về Sprint 02

- **Tên Sprint:** Sprint 02 — Cơ cấu Tổ chức, Nhân sự, Danh bạ & Quản lý Biểu mẫu Động
- **Thời gian Thực hiện:** Tuần 3 - Tuần 4 (10 ngày làm việc)
- **Mục tiêu Sprint (Sprint Goal):**
  > Xây dựng hoàn chỉnh cây cơ cấu tổ chức doanh nghiệp đa cấp (`OrgManagement`), quản lý chức danh (`Positions`), danh bạ nội bộ (`Directory API`), cơ chế ủy quyền (`Delegation`); kết hợp phân hệ Biểu mẫu động Form.io (`FormMasterData`) hỗ trợ phân phiên bản form (`IsLatest`), danh mục phụ thuộc Cascading Loading tốc độ cao và kiểm soát chống giả mạo dữ liệu (`Anti-Tampering`).
- **Tổng Story Points cam kết:** **71 SP**
- **Tổng Giờ Kỹ thuật khả dụng (Capacity):** **340 Giờ** (Team 5 Dev + 1 QA)

---

## 2. Sprint Backlog & Acceptance Criteria (Gherkin)

### US-15, US-16 & US-17: Cây Tổ chức Đa cấp & Chức danh (18 SP)
- **Acceptance Criteria:**
  ```gherkin
  Scenario: Chặn tạo vòng lặp cây tổ chức phân cấp
    Given phòng ban A có phòng ban con B
    When người dùng cập nhật ParentId của phòng ban A trỏ về phòng ban B
    Then hệ thống từ chối cập nhật và ném lỗi CircularOrgException.
  ```

### US-18 & US-19: Hồ sơ Nhân sự & Danh bạ Nội bộ (16 SP)
- **Acceptance Criteria:**
  ```gherkin
  Scenario: Tìm kiếm nhanh trong danh bạ nội bộ
    Given cơ sở dữ liệu có 10,000 nhân viên
    When người dùng gõ từ khóa tìm kiếm tên "Nguyễn" hoặc số máy lẻ "102"
    Then API DirectorySearch trả về kết quả trong thời gian < 30ms nhờ Covering Index.
  ```

### US-20 & US-21: Cơ chế Ủy quyền & Chống Ký vòng tròn (18 SP)
- **Acceptance Criteria:**
  ```gherkin
  Scenario: Chặn ủy quyền vòng tròn trong cùng khoảng thời gian
    Given người dùng A đã ủy quyền duyệt đơn cho B từ 01/12 đến 15/12
    When người dùng B cố tình tạo ủy quyền lại cho A trong khoảng thời gian trên
    Then hệ thống từ chối tạo mới và phát cảnh báo "Ủy quyền chéo không hợp lệ".
  ```

### US-22, US-23 & US-24: Form.io Schema, Versioning & Cascading (19 SP)
- **Acceptance Criteria:**
  ```gherkin
  Scenario: Bảo lưu phiên bản biểu mẫu cũ khi nâng cấp form
    Given đơn thanh toán đã submit với Form Version 1
    When Quản trị viên cập nhật Form lên Version 2 và đánh dấu IsLatest = 1
    Then người mở lại đơn cũ vẫn xem đúng cấu trúc giao diện Version 1
    And khi người nộp đơn tạo phiếu mới thì form hiển thị theo Version 2.
  ```

---

## 3. Danh mục Task Kỹ thuật Chi tiết (Tasks Breakdown)

| Mã Task | Module | Hạng mục Kỹ thuật | Mô tả Chi tiết Công việc Cần Code | Giờ (h) | Người Phụ trách |
|:---:|:---:|---|---|:---:|:---:|
| **TSK-201** | Org | Database Schema [org] | Tạo bảng `[org].[OrgUnits]`, `[org].[Positions]`, `[org].[Employees]`, `[org].[WorkflowDelegations]` kèm Foreign Keys và Soft Delete columns. | 8 | Backend Dev 1 |
| **TSK-202** | Org | Anti-Circular Traversal | Viết thuật toán duyệt ngược tổ tiên (Ancestry Traversal) kiểm tra đệ quy chống vòng lặp cây tổ chức. | 8 | Backend Dev 1 |
| **TSK-203** | Org | Positions & Level Management | Viết CRUD Chức danh cấp bậc (Positions) và API phân cấp chức danh. | 6 | Backend Dev 2 |
| **TSK-204** | Org | Employee Directory API | Viết API tìm kiếm danh bạ nhân sự, phân trang, tích hợp Covering Index `IX_Employees_DirectorySearch`. | 8 | Backend Dev 2 |
| **TSK-205** | Org | Delegation Resolver Service | Triển khai `IDelegationResolver` và `IOrgHierarchyService` phân giải danh tính người duyệt thực tế và người quản lý trực tiếp. | 12 | Backend Dev 1 |
| **TSK-206** | Org | Anti-Circular Delegation | Kiểm tra logic thời gian `[FromDate, ToDate]` ngăn chặn triệt để ủy quyền ngược hoặc vòng tròn. | 8 | Backend Dev 1 |
| **TSK-207** | Org | Employee Outgoing Events | Viết phát sinh MediatR Events: `EmployeeDeactivatedEvent`, `DelegationAssignedEvent`. | 6 | Backend Dev 2 |
| **TSK-208** | Form | Database Schema [form] | Tạo bảng `[form].[FormDefinitions]`, `[form].[MasterCategories]`, `[form].[MasterItems]` kèm Filtered Index `WHERE [IsLatest] = 1`. | 8 | Backend Dev 3 |
| **TSK-209** | Form | Form Versioning Service | Triển khai `IFormVersionService` hỗ trợ xuất bản version mới, lưu lịch sử `ChangeNotes`, và rollback version an toàn. | 10 | Backend Dev 3 |
| **TSK-210** | Form | Cascading Loading Engine | Xây dựng API MasterData trả về danh mục con theo `parentId` với Non-clustered Index tối ưu truy vấn $< 10\text{ ms}$. | 8 | Backend Dev 3 |
| **TSK-211** | Form | Multi-Level Redis Cache | Tích hợp Redis Caching cho Master Data với cơ chế tự động xóa cache theo tiền tố khóa khi danh mục thay đổi. | 10 | Backend Dev 3 |
| **TSK-212** | Form | Anti-Tampering FluentValidation | Viết bộ quy tắc `FluentValidation` kiểm tra tính hợp lệ quan hệ cha-con của dữ liệu nộp lên, chống sửa DevTools. | 10 | Backend Dev 2 |
| **TSK-213** | Form | Schema Sanitization | Viết bộ lọc làm sạch JSON Schema trước khi lưu, loại bỏ các đoạn mã script độc hại (chống Stored XSS). | 6 | Backend Dev 2 |
| **TSK-214** | UI | Org Tree View Management | Xây dựng giao diện cây tổ chức trực quan (jsTree / Vue Tree view) cho phép kéo thả và chỉnh sửa đơn vị. | 18 | Frontend Dev 1 |
| **TSK-215** | UI | Employee Directory Portal | Xây dựng trang danh bạ nội bộ có thanh tìm kiếm thông minh, hiển thị avatar, thẻ nhân viên và số máy lẻ. | 14 | Frontend Dev 1 |
| **TSK-216** | UI | Delegation Setup Modal | Xây dựng modal cho phép người dùng tự thiết lập ủy quyền duyệt đơn trong kỳ nghỉ phép/công tác. | 10 | Frontend Dev 2 |
| **TSK-217** | UI | Form.io Builder Embedding | Nhúng Form.io Drag & Drop Builder vào AdminLTE, cho phép kéo thả trường dữ liệu và cấu hình URL Cascading. | 22 | Frontend Dev 2 |
| **TSK-218** | UI | Cascading Form Renderer | Cấu hình Component Select Form.io hỗ trợ `refreshOn`, `clearOnRefresh` nạp dữ liệu mượt mà không bị giật trang. | 12 | Frontend Dev 2 |
| **TSK-219** | QA | Unit Test Org & Delegations | Viết 30 Unit Tests cho thuật toán chống lặp cây tổ chức, chống ủy quyền chéo và phân giải người duyệt. | 16 | QA Engineer |
| **TSK-220** | QA | Test Cascading & Tampering | Viết 25 Integration Tests giả lập sửa payload ID cha/con qua Postman và kiểm tra kết quả chặn của FluentValidation. | 16 | QA Engineer |
| **TỔNG** | | | **Tổng số 20 Tasks Kỹ thuật** | **226h** | |

---

## 4. Kế hoạch Đánh giá & Tiêu chí Release Sprint 02

### 4.1. Sprint Review / Demo Checklist
- [ ] **Demo 1:** Quản trị viên thao tác kéo thả chuyển phòng ban con thành cha của chính nó $\rightarrow$ Giao diện hiển thị thông báo lỗi chặn đệ quy.
- [ ] **Demo 2:** Mở trang Danh bạ Nhân sự $\rightarrow$ Gõ từ khóa tìm kiếm nhân viên, kết quả trả về tức thì với đầy đủ máy nhánh và email.
- [ ] **Demo 3:** Thiết lập ủy quyền duyệt đơn từ User A sang User B $\rightarrow$ Hệ thống tự động ghi nhận; thử ủy quyền ngược lại thì bị chặn.
- [ ] **Demo 4:** Thiết kế form mẫu "Đề nghị Thanh toán" trên Form.io Builder $\rightarrow$ Chọn Dự án A thì dropdown Hạng mục chỉ hiển thị các gói thầu thuộc Dự án A.
- [ ] **Demo 5:** Dùng Postman gửi request giả mạo trỏ Hạng mục của Dự án B vào Dự án A $\rightarrow$ Server ném lỗi validation 400 Bad Request.
- [ ] **Demo 6:** Nâng cấp form lên v2 $\rightarrow$ Mở lại phiếu cũ vẫn hiển thị chuẩn giao diện v1.

### 4.2. Đánh giá Rủi ro Sprint 02
| Rủi ro Kỹ thuật | Mức độ | Biện pháp Phòng ngừa |
|---|:---:|---|
| Dữ liệu Master Data bị stale khi có cập nhật | Thấp | Bắt buộc xóa cache Redis theo pattern `masterdata:{category}:*` mỗi khi có thao tác CUD. |
| Form.io render lỗi khi JSON Schema phức tạp | Trung bình | Tạo bộ schema validation chuẩn và preview form thử nghiệm trước khi lưu version chính thức. |

