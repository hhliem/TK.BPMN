# 🎨 Frontend Sprint 01: Core Architecture, Shell Layout & Authentication

---

## 1. Thông tin Chung về FE-Sprint 01

- **Tên Sprint:** Frontend Sprint 01 — Core Architecture, App Shell Layout, RBAC Navigation & Authentication
- **Thời gian Thực hiện:** Tuần 1 - Tuần 2 (10 ngày làm việc)
- **Mục tiêu Sprint (Sprint Goal):**
  > Thiết lập nền tảng dự án Vue 3.5+ (Vite 6, TypeScript, Pinia, Tailwind CSS), xây dựng hệ thống Design Tokens và chuẩn giao diện Swiss Minimalist / Bento Grid từ Skill 02 (`02-frontend-ui-ux-pro-max`), hoàn thiện luồng Xác thực JWT Bearer với Axios Interceptor, App Shell Layout chuẩn doanh nghiệp (Sidebar thu gọn, Header cố định, Breadcrumb) và Cây Menu điều hướng phân quyền động (Dynamic RBAC Navigation).
- **Tổng Story Points cam kết:** **50 SP**
- **Tổng Giờ Kỹ thuật khả dụng (Capacity):** **250 Giờ** (Team 3 Frontend Devs + 1 QA)

---

## 2. Sprint Backlog & Acceptance Criteria (Gherkin)

### FE-US-01: Khởi tạo Kiến trúc Vue 3 Vite TypeScript & Design Tokens (8 SP)
- **User Story:** Là Frontend Developer, tôi muốn khởi tạo cấu trúc dự án chuẩn modular với Tailwind CSS Design Tokens để toàn bộ ứng dụng đồng nhất về bảng màu, kiểu chữ và khoảng cách.
- **Acceptance Criteria:**
  ```gherkin
  Scenario: Thiết lập Design Tokens và biến CSS ngữ nghĩa
    Given dự án Vue 3 được khởi tạo với Vite 6 và TypeScript ở chế độ Strict
    When cấu hình tailwind.config.ts với các biến CSS ngữ nghĩa (--color-primary, --color-surface, --space-*)
    Then giao diện hiển thị đúng bảng màu thương hiệu doanh nghiệp
    And không có lỗi biên dịch TypeScript (tsc --noEmit đạt 0 errors).
  ```

### FE-US-02 & FE-US-03: Đăng nhập JWT Bearer & Axios Interceptor An toàn (13 SP)
- **User Story:** Là người dùng doanh nghiệp, tôi muốn đăng nhập bằng tài khoản và mật khẩu, hệ thống lưu phiên làm việc an toàn và tự động đính kèm token vào mọi truy vấn API.
- **Acceptance Criteria:**
  ```gherkin
  Scenario: Đăng nhập thành công và tự động gắn Token
    Given người dùng nhập đúng Username và Password tại trang /login
    When người dùng bấm nút "Đăng nhập"
    Then API POST /api/auth/login trả về Access Token và Refresh Token
    And Pinia AuthStore lưu trữ thông tin người dùng và quyền hạn
    And Axios Interceptor tự động thêm header "Authorization: Bearer <token>" và "X-Correlation-ID" vào các request tiếp theo
    And người dùng được chuyển hướng đến trang Dashboard hoặc trang trước khi bị chặn.

  Scenario: Xử lý phiên đăng nhập hết hạn (HTTP 401 Unauthorized)
    Given Access Token của người dùng đã hết hạn trong khi thao tác
    When một request API bất kỳ trả về mã lỗi HTTP 401
    Then Axios Response Interceptor tự động gọi API POST /api/auth/refresh-token
    And nếu làm mới thành công, request ban đầu được tự động thực thi lại mà không ngắt quãng trải nghiệm
    And nếu làm mới thất bại, người dùng được chuyển về /login kèm thông báo phiên làm việc kết thúc.
  ```

### FE-US-04 & FE-US-05: App Shell Layout & Dynamic RBAC Navigation (16 SP)
- **User Story:** Là nhân viên công ty, tôi muốn thanh điều hướng (Sidebar) chỉ hiển thị các phân hệ và tính năng mà tôi được phân quyền, đồng thời có thể thu gọn thanh menu để mở rộng không gian làm việc.
- **Acceptance Criteria:**
  ```gherkin
  Scenario: Phân quyền động cây menu điều hướng
    Given người dùng có danh sách quyền ['org:view', 'workflow:inbox:view']
    When App Shell nạp danh sách cấu hình tuyến đường (Navigation Routes)
    Then Sidebar chỉ render mục "Cơ cấu Tổ chức" và "Hộp thư Công việc"
    And các mục Quản trị hệ thống, Cấu hình biểu mẫu bị ẩn hoàn toàn khỏi DOM
    And nếu người dùng cố tình gõ URL /admin/roles trên trình duyệt, Router Guard chuyển hướng về trang 403 Forbidden.

  Scenario: Thu gọn/Mở rộng Sidebar và ghi nhớ trạng thái
    Given người dùng đang ở giao diện chính
    When người dùng bấm nút Toggle Sidebar
    Then Sidebar chuyển đổi mượt mà giữa dạng đầy đủ (260px) và dạng biểu tượng (68px) kèm tooltip
    And trạng thái này được lưu vào localStorage để giữ nguyên khi người dùng F5 hoặc quay lại.
  ```

### FE-US-06 & FE-US-07: Chế độ Sáng/Tối & Quản lý Tài khoản Cá nhân (13 SP)
- **User Story:** Là người dùng, tôi muốn dễ dàng chuyển đổi Dark/Light mode và mở modal đổi mật khẩu cá nhân để bảo mật tài khoản.
- **Acceptance Criteria:**
  ```gherkin
  Scenario: Chuyển đổi Dark/Light Mode không gây nhấp nháy (FOUC)
    Given người dùng bấm nút chuyển đổi giao diện Sáng/Tối trên Header
    When chế độ Dark Mode được kích hoạt
    Then thẻ <html> được gắn class "dark", các biến màu nền và chữ đổi sang tone tối tương phản cao
    And không có hiện tượng giật sáng (Flash of Unstyled Content) khi tải lại trang.
  ```

---

## 3. Danh mục Task Kỹ thuật Chi tiết (Tasks Breakdown)

| Mã Task | Phân hệ / Tầng | Hạng mục Kỹ thuật | Mô tả Chi tiết Công việc Cần Code | Giờ (h) | Người Phụ trách |
|:---:|:---:|---|---|:---:|:---:|
| **TSK-FE-101** | Core Architecture | Scaffolding & Tooling | Khởi tạo dự án Vite 6 + Vue 3.5 TypeScript, cấu hình ESLint, Prettier, Husky git pre-commit hook và path aliases (`@/*`). | 10 | Tech Lead |
| **TSK-FE-102** | Design Tokens | Tailwind CSS Config | Cấu hình `tailwind.config.ts` tích hợp bộ tokens Swiss Minimalist / Bento Grid, biến màu CSS (`--bg-primary`, `--text-main`, `--border-subtle`), typography Inter / Plus Jakarta Sans. | 14 | Frontend Dev 1 |
| **TSK-FE-103** | Core Components | shadcn-vue / Radix Setup | Tích hợp thư viện headless components: Button, Input, DropdownMenu, Dialog, Tooltip, Avatar, Toast với chuẩn WCAG 2.2 AA. | 16 | Frontend Dev 1 |
| **TSK-FE-104** | State Management | Pinia Core Stores | Khởi tạo Pinia với các store: `useAuthStore`, `useAppThemeStore`, `useNavigationStore`, `useGlobalLoadingStore`. | 12 | Frontend Dev 2 |
| **TSK-FE-105** | Network / API | Axios Client & Interceptors | Viết `apiClient.ts` xử lý Request Interceptor (Bearer Token, Correlation ID), Response Interceptor (Refresh Token queue lock, Global Error Toasts). | 16 | Frontend Dev 2 |
| **TSK-FE-106** | Authentication | Login View & Form Validation | Dựng trang `LoginView.vue` với thiết kế thương hiệu doanh nghiệp, validation tài khoản/mật khẩu, ghi nhớ "Remember Me" và trạng thái loading spinner. | 16 | Frontend Dev 3 |
| **TSK-FE-107** | Routing | Vue Router & Auth Guards | Cấu hình `vue-router` 4 với cơ chế route meta (`requiresAuth`, `requiredPermission`), xử lý chuyển hướng redirect an toàn. | 14 | Frontend Dev 2 |
| **TSK-FE-108** | App Shell | Master Layout & Sidebar | Dựng `AppLayout.vue`, `AppSidebar.vue` hỗ trợ thu gọn 68px/260px, phân nhóm menu đa cấp, biểu tượng Lucide Icons và animation CSS mượt mà. | 22 | Frontend Dev 1 |
| **TSK-FE-109** | App Shell | Header & Breadcrumbs | Dựng `AppHeader.vue` với thanh tìm kiếm nhanh, nút chuyển Theme, thông tin người dùng đăng nhập và `AppBreadcrumbs.vue` tự động parse theo route. | 16 | Frontend Dev 1 |
| **TSK-FE-110** | RBAC Engine | Permission Directive & Guard | Viết Custom Directive `v-permission="['org:edit']"` tự động ẩn element nếu thiếu quyền và hàm helper `hasPermission()` cho component templates. | 12 | Frontend Dev 2 |
| **TSK-FE-111** | User Profile | Account Modal & Password Change | Dựng `UserProfileModal.vue` hiển thị thông tin tài khoản, chức vụ, phòng ban và form đổi mật khẩu kèm xác thực độ mạnh mật khẩu. | 16 | Frontend Dev 3 |
| **TSK-FE-112** | Error Handling | Error Views (403, 404, 500) | Dựng các trang thông báo lỗi đẹp mắt theo chuẩn Bento Card kèm nút "Quay lại trang chủ" và thông tin Correlation ID hỗ trợ hỗ trợ kỹ thuật. | 10 | Frontend Dev 3 |
| **TSK-FE-113** | QA Testing | Unit Tests (Vitest) | Viết bộ kiểm thử Unit Tests cho AuthStore, Axios Interceptor Refresh Token logic, và Permission Evaluator helper (>35 test cases). | 18 | QA Engineer |
| **TSK-FE-114** | QA Testing | E2E Tests (Playwright) | Viết kịch bản E2E kiểm tra luồng: Đăng nhập thành công -> Điều hướng App Shell -> Đổi mật khẩu -> Đăng xuất an toàn. | 18 | QA Engineer |
| **TỔNG** | | | **Tổng số 14 Tasks Kỹ thuật** | **204h** | |

---

## 4. Kế hoạch Đánh giá & Tiêu chí Release FE-Sprint 01

### 4.1. Sprint Review / Demo Checklist
- [ ] **Demo 1:** Chạy lệnh `npm run dev`, giao diện khởi động dưới 300ms, toàn bộ biến màu CSS và font chữ hiển thị chuẩn xác theo bảng mẫu Swiss Minimalist.
- [ ] **Demo 2:** Đăng nhập tài khoản Quản trị viên $\rightarrow$ Menu hiển thị đầy đủ các mục; Đăng nhập tài khoản Nhân viên $\rightarrow$ Các mục Quản trị bị ẩn hoàn toàn.
- [ ] **Demo 3:** Cố tình xóa Access Token trong bộ nhớ và thực hiện một thao tác click $\rightarrow$ Axios Interceptor tự động refresh token thành công mà người dùng không bị văng ra.
- [ ] **Demo 4:** Nhấn nút thu gọn Sidebar $\rightarrow$ Menu thu về 68px, hover vào từng icon hiển thị tooltip nổi chính xác tên chức năng; tải lại trang F5 trạng thái vẫn được duy trì.
- [ ] **Demo 5:** Bấm nút chuyển đổi Dark Mode $\rightarrow$ Toàn bộ giao diện đổi màu nền và màu chữ tối tương phản cao ngay lập tức, không có vết nháy trắng màn hình.

### 4.2. Đánh giá Rủi ro Kỹ thuật FE-Sprint 01
| Rủi ro Kỹ thuật | Mức độ | Biện pháp Phòng ngừa |
|---|:---:|---|
| **Race condition khi Refresh Token:** Nhiều request đồng thời trả về 401 gây gọi API refresh lặp lại liên tục | Cao | Sử dụng cờ hiệu `isRefreshing` và hàng đợi `failedQueue` trong Axios Interceptor để gom các request chờ token mới. |
| **Lộ đường dẫn nhạy cảm trên Router:** Người dùng gõ tay URL trang quản trị | Trung bình | Kiểm tra chặt chẽ `beforeEach` Router Guard kết hợp kiểm tra quyền từ AuthStore trước khi cho phép render component. |
| **FOUC (Flash of Unstyled Content) Dark Mode:** Màn hình nháy sáng trước khi nạp xong theme | Thấp | Chèn script nhỏ đồng bộ ở thẻ `<head>` trong `index.html` đọc trạng thái `theme` từ `localStorage` trước khi Vue mount. |

