# 📝 Frontend Sprint 03: Form.io Dynamic Forms & Cascading Loading

---

## 1. Thông tin Chung về FE-Sprint 03

- **Tên Sprint:** Frontend Sprint 03 — Form.io Vue 3 Integration, Cascading Dropdowns & File Upload
- **Thời gian Thực hiện:** Tuần 5 - Tuần 6 (10 ngày làm việc)
- **Mục tiêu Sprint (Sprint Goal):**
  > Xây dựng Component bọc Form.io Vue 3 Renderer tùy biến 100% theo Tailwind CSS Design Tokens của hệ thống, hoàn thiện cơ chế Cascading Dropdowns tự động lọc theo trường cha (`clearOnRefresh: true`), xử lý hiển thị lỗi nhập liệu trực tiếp (Inline Validation) bằng tiếng Việt chuẩn hóa, tích hợp bộ tải tệp kéo thả (Drag & Drop) tiến trình OneDrive và cơ chế Tự động lưu bản nháp (Auto-save draft) chống mất dữ liệu khi mất kết nối mạng.
- **Tổng Story Points cam kết:** **55 SP**
- **Tổng Giờ Kỹ thuật khả dụng (Capacity):** **275 Giờ** (Team 3 Frontend Devs + 1 QA)

---

## 2. Sprint Backlog & Acceptance Criteria (Gherkin)

### FE-US-12: Component bọc Form.io Vue 3 Renderer Chuẩn Hóa CSS (13 SP)
- **User Story:** Là người dùng doanh nghiệp, tôi muốn giao diện biểu mẫu động hiển thị đồng bộ với thiết kế của toàn hệ thống (phông chữ, ô nhập liệu bo góc, màu sắc focus) chứ không phải kiểu dáng Bootstrap thô sơ mặc định.
- **Acceptance Criteria:**
  ```gherkin
  Scenario: Render biểu mẫu Form.io với Tailwind CSS Design Tokens
    Given một JSON Schema biểu mẫu hợp lệ được tải từ API GET /api/form/definitions/{code}
    When Component <FormioRenderer :schema="schema" /> được mount vào DOM
    Then toàn bộ các input, select, textarea, checkbox kế thừa đúng lớp CSS Tailwind (--color-primary, rounded-lg, border-slate-200)
    And hiển thị hoàn hảo ở cả 2 chế độ Sáng (Light) và Tối (Dark)
    And phát ra (emit) các sự kiện `change`, `submit`, `error` chuẩn Vue 3.
  ```

### FE-US-13: Động cơ Cascading Dropdowns Tự Động Lọc & Tự Xóa Dữ liệu Cũ (13 SP)
- **User Story:** Là người nộp đơn, khi tôi chọn "Nhóm chi phí", trường "Mục chi phí con" phải tự động tải lại danh mục tương ứng và tự động xóa giá trị đã chọn trước đó nếu tôi thay đổi nhóm chi phí cha.
- **Acceptance Criteria:**
  ```gherkin
  Scenario: Tự động tải và đồng bộ Cascading Dropdowns
    Given trường "Mục chi phí" được cấu hình phụ thuộc vào trường "Nhóm chi phí" với thuộc tính "clearOnRefresh: true"
    When người dùng chọn "Chi phí Tiếp khách" ở trường Nhóm chi phí
    Then trường Mục chi phí kích hoạt hiệu ứng tải spinner nội tuyến (inline loading)
    And gọi API GET /api/form/master-data/expense-items?groupId=... để nạp danh sách con
    And khi người dùng đổi lại Nhóm chi phí sang "Chi phí Công tác", giá trị đã chọn ở Mục chi phí bị xóa sạch (reset về null) ngay lập tức.
  ```

### FE-US-14: Inline Field Validation & Bản địa hóa Tiếng Việt (8 SP)
- **User Story:** Là người nhập liệu, khi nhập sai định dạng hoặc thiếu trường bắt buộc, tôi muốn thông báo lỗi hiển thị rõ ràng ngay dưới chân ô nhập liệu bằng tiếng Việt dễ hiểu.
- **Acceptance Criteria:**
  ```gherkin
  Scenario: Hiển thị lỗi nhập liệu trực tiếp bằng tiếng Việt
    Given một trường bắt buộc "Số tiền đề nghị" bị bỏ trống khi bấm "Nộp hồ sơ"
    When hệ thống kích hoạt Client-side Validation
    Then viền ô nhập liệu chuyển sang màu đỏ cảnh báo (border-rose-500)
    And dòng chữ bên dưới hiển thị: "Vui lòng nhập Số tiền đề nghị" thay vì câu tiếng Anh mặc định
    And màn hình tự động cuộn (smooth scroll) đến vị trí trường bị lỗi đầu tiên.
  ```

### FE-US-15: Kéo thả Tải lên Tệp đính kèm với Tiến trình OneDrive (13 SP)
- **User Story:** Là nhân viên tạo đề xuất, tôi muốn kéo thả các hóa đơn chứng từ vào khung tải lên, hệ thống hiển thị tiến trình tải trực quan và cho phép xem trước tệp trước khi nộp.
- **Acceptance Criteria:**
  ```gherkin
  Scenario: Kéo thả tải lên nhiều hóa đơn chứng từ
    Given người dùng kéo thả 3 tệp PDF/ảnh vào vùng upload
    When các tệp đáp ứng tiêu chuẩn định dạng (PDF, PNG, JPG) và dung lượng $\le 25\text{MB}$
    Then thanh tiến trình (Progress Bar) hiển thị tỷ lệ % tải lên thực tế tới OneDrive
    And sau khi tải thành công, danh sách tệp hiển thị kèm nút Xem trước (Preview) và nút Xóa.
  ```

### FE-US-16: Tự động Lưu Bản Nháp Hồ sơ (Auto-Save Drafts) (8 SP)
- **User Story:** Là người làm hồ sơ dài, tôi muốn hệ thống tự động lưu bản nháp định kỳ để nếu bị mất điện hoặc mất mạng đột ngột thì dữ liệu vừa gõ không bị mất.
- **Acceptance Criteria:**
  ```gherkin
  Scenario: Tự động lưu nháp vào LocalStorage / IndexedDB
    Given người dùng đang điền hồ sơ trình ký dở dang
    When sau mỗi 15 giây hoặc khi người dùng dừng gõ phím quá 2 giây
    Then toàn bộ trạng thái dữ liệu form được tự động lưu vào LocalStorage
    And biểu tượng nhấp nháy góc biểu mẫu hiển thị: "Đã lưu nháp lúc 14:32:05"
    And khi người dùng mở lại trang biểu mẫu đó, xuất hiện thông báo: "Tìm thấy bản nháp chưa nộp. Bạn có muốn khôi phục không?".
  ```

---

## 3. Danh mục Task Kỹ thuật Chi tiết (Tasks Breakdown)

| Mã Task | Phân hệ / Tầng | Hạng mục Kỹ thuật | Mô tả Chi tiết Công việc Cần Code | Giờ (h) | Người Phụ trách |
|:---:|:---:|---|---|:---:|:---:|
| **TSK-FE-301** | Form.io Engine | Custom Vue 3 Wrapper | Viết component `FormioRenderer.vue` tích hợp `@formio/vue`, hook vòng đời `beforeSubmit`, `renderComplete`, tùy biến template CSS. | 24 | Frontend Dev 1 |
| **TSK-FE-302** | Form.io Engine | Theme CSS Overrides | Viết bộ CSS module ghi đè các class mặc định của Form.io sang Tailwind CSS tokens (`.formio-component`, `.form-control`, `.is-invalid`). | 16 | Frontend Dev 1 |
| **TSK-FE-303** | Form.io Engine | Cascading Select Provider | Triển khai plugin mở rộng cho Form.io Select component, xử lý URL dynamic interpolation `{{ data.parentId }}` và cờ `clearOnRefresh`. | 22 | Tech Lead |
| **TSK-FE-304** | Form.io Engine | Custom Conditional Logic | Mở rộng dynamic show/hide/disable dựa trên JSONLogic và biểu thức điều kiện nghiệp vụ đa trường. | 16 | Frontend Dev 2 |
| **TSK-FE-305** | Form.io Engine | Vietnamese i18n Dictionary | Xây dựng từ điển chuyển ngữ tiếng Việt hoàn chỉnh cho tất cả thông báo validation của Form.io (required, min, max, pattern, email, date). | 10 | Frontend Dev 3 |
| **TSK-FE-306** | Form Inputs | Currency & Number Mask | Viết Custom Form.io Component cho trường Tiền tệ VNĐ có định dạng dấu chấm phân cách hàng nghìn (`10.000.000 đ`) và tự động parse số thực. | 16 | Frontend Dev 2 |
| **TSK-FE-307** | File Upload | Drag & Drop Zone Component | Dựng component `FormioFileUpload.vue` hỗ trợ kéo thả tệp, kiểm tra MIME type, kích thước tối đa, icon theo đuôi tệp. | 18 | Frontend Dev 3 |
| **TSK-FE-308** | File Upload | OneDrive Upload Integration | Tích hợp upload progress bar với Axios `onUploadProgress`, cơ chế chunked upload cho tệp lớn và liên kết API OneDrive. | 18 | Frontend Dev 2 |
| **TSK-FE-309** | File Upload | In-App File Preview Modal | Dựng modal xem trước tệp: nhúng PDF viewer (`pdfjs`), xem trước ảnh phóng to thu nhỏ, xem tệp Word qua Office Web Viewer. | 16 | Frontend Dev 3 |
| **TSK-FE-310** | Draft Storage | Auto-save & Recovery Service | Viết composable `useFormDraftAutoSave.ts` quản lý lưu IndexedDB, hiển thị trạng thái lưu nháp và banner khôi phục dữ liệu. | 16 | Frontend Dev 1 |
| **TSK-FE-311** | QA Testing | Unit Tests Form Components | Viết bộ Unit Tests cho Cascading select evaluation, currency mask và auto-save composable (>35 test cases). | 18 | QA Engineer |
| **TSK-FE-312** | QA Testing | E2E Tests Form Filling & Upload | Viết kịch bản E2E kiểm tra điền form, đổi dropdown cha làm sạch dropdown con, upload hóa đơn và khôi phục nháp sau khi reload. | 20 | QA Engineer |
| **TỔNG** | | | **Tổng số 12 Tasks Kỹ thuật** | **210h** | |

---

## 4. Kế hoạch Đánh giá & Tiêu chí Release FE-Sprint 03

### 4.1. Sprint Review / Demo Checklist
- [ ] **Demo 1:** Nạp một JSON Schema mẫu gồm 15 trường dữ liệu; giao diện hiển thị đồng nhất bảng màu Swiss Minimalist, không có bất kỳ class CSS Bootstrap nào bị vỡ giao diện.
- [ ] **Demo 2:** Chọn mục "Chi nhánh Hồ Chí Minh" ở trường Tỉnh/Thành phố $\rightarrow$ Trường "Phòng giao dịch" hiện spinner và tải đúng các phòng giao dịch tại TP.HCM; khi đổi về "Hà Nội", trường con tự xóa dữ liệu cũ ngay lập tức.
- [ ] **Demo 3:** Nhập thử số tiền `50000000` $\rightarrow$ Tự động định dạng thành `50.000.000 VNĐ`; nhập số âm $\rightarrow$ Báo lỗi tiếng Việt: "Số tiền không được nhỏ hơn 0".
- [ ] **Demo 4:** Kéo thả một tệp hóa đơn PDF 15MB vào ô tải tệp $\rightarrow$ Thanh tiến trình chạy từ 0% đến 100%, tệp tải lên thành công và có thể nhấp mở xem trực tiếp trong modal.
- [ ] **Demo 5:** Đang điền đơn dở dang, nhấn F5 hoặc tắt tab trình duyệt rồi mở lại $\rightarrow$ Xuất hiện banner thông báo khôi phục bản nháp, bấm "Đồng ý" thì toàn bộ dữ liệu đã nhập được điền lại nguyên vẹn.

### 4.2. Đánh giá Rủi ro Kỹ thuật FE-Sprint 03
| Rủi ro Kỹ thuật | Mức độ | Biện pháp Phòng ngừa |
|---|:---:|---|
| **Xung đột CSS giữa Form.io và Tailwind:** Class nội bộ của Form.io bị trùng tên với Tailwind | Cao | Sử dụng CSS Scoping hoặc bọc Form.io trong container riêng kèm tiền tố cấu hình rõ ràng. |
| **Dữ liệu nháp quá tải LocalStorage (giới hạn 5MB):** Khi người dùng upload ảnh base64 | Trung bình | Không lưu file đính kèm vào LocalStorage; chỉ lưu File Metadata (ID, tên tệp) và dùng IndexedDB để lưu trữ nháp nếu cần. |

