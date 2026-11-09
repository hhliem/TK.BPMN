# 🗃️ Master Product Backlog — Frontend SPA (Vue.js 3 & UI/UX Pro Max)

Nền tảng Giao diện Người dùng Hiện đại (Single Page Application - SPA) cho **Enterprise BPM Platform** được xây dựng trên nền tảng **Vue.js 3 (Composition API + TypeScript)** kết hợp công nghệ **Vite, Pinia, Tailwind CSS, shadcn-vue** và bộ tri thức thiết kế chuyên sâu **Skill 02 (`02-frontend-ui-ux-pro-max`)**.

---

## 1. Công nghệ & Kiến trúc Frontend Chuẩn hóa

| Thành phần | Công nghệ Lựa chọn | Mục đích & Lợi ích Kỹ thuật |
|---|---|---|
| **Core Framework** | **Vue.js 3.5+ (Composition API & `<script setup>`)** | Hiệu năng cao, Virtual DOM tối ưu, phản ứng 2 chiều tự động (Reactivity), TypeScript an toàn kiểu. |
| **Build Tool & Bundler** | **Vite 6.x** | Khởi động tức thì (Instant Server Start), Hot Module Replacement (HMR) tính bằng mili-giây. |
| **State Management** | **Pinia 2.x** | Quản lý trạng thái tập trung (Auth, Permissions, Theme, Notification Hub, Workflow Drafts), hỗ trợ DevTools mượt mà. |
| **Styling & Design System** | **Tailwind CSS v3.4+ & CSS Semantic Tokens** | Triển khai triệt để 80 UI Styles (Bento Grid, Swiss Minimalist, Dark Mode) từ Skill 02, cấu hình biến CSS `--color-primary`, `--space-*`. |
| **Component Library** | **shadcn-vue / Radix Vue & Headless UI** | Thành phần giao diện không style sẵn, đạt chuẩn tiếp cận **WCAG 2.2 AA** (Keyboard navigation, Focus rings, ARIA labels). |
| **Dynamic Form Engine** | **Form.io Vue 3 Integration** | Hiển thị biểu mẫu động từ JSON Schema, Cascading Dropdowns (`clearOnRefresh: true`), Client-side Validation tức thì. |
| **Icons Library** | **Lucide Icons Vue / Heroicons SVG** | Biểu tượng vector SVG sắc nét, tuyệt đối không dùng Emoji làm icon hệ thống. |
| **Realtime Push** | **Microsoft @microsoft/signalr** | Kết nối WebSocket tới Backend .NET, nhận thông báo tức thời (TaskAssignedEvent, SlaWarningEvent). |
| **Data Visualization** | **Apache ECharts / Vue-ECharts & ApexCharts** | Trực quan hóa Heatmap nghẽn luồng quy trình, biểu đồ thời gian xử lý và đồng hồ đo SLA. |
| **HTTP Client** | **Axios (có Interceptor)** | Tự động đính kèm `Bearer Token`, sinh mã `X-Correlation-ID`, xử lý Refresh Token và Circuit Breaker error toasts. |

---

## 2. Danh mục Epics Frontend (FE-EPIC)

| Mã Epic | Tên Epic Giao diện | Module Phụ trách | Tổng SP Dự kiến | Sprint Thực hiện |
|:---:|---|---|:---:|:---:|
| **FE-EPIC-01** | Khung Ứng dụng, Xác thực RBAC & Shell Giao diện | AppShell, Auth, Layout | **50 SP** | FE-Sprint 01 |
| **FE-EPIC-02** | Phân hệ Cây Cơ cấu Tổ chức, Danh bạ & Phân quyền | OrgManagement, Directory | **45 SP** | FE-Sprint 02 |
| **FE-EPIC-03** | Động cơ Biểu mẫu Động Form.io & Cascading Loading | FormMasterData, Documents | **55 SP** | FE-Sprint 03 |
| **FE-EPIC-04** | Hộp thư Công việc, Luồng Duyệt & Nhúng BPMN Canvas | WorkflowInbox, ElsaStudio | **60 SP** | FE-Sprint 04 |
| **FE-EPIC-05** | Thông báo Thời gian thực SignalR & Cổng Bản tin | NotificationHub, NewsFeed | **48 SP** | FE-Sprint 05 |
| **FE-EPIC-06** | Trợ lý Ảo AI Copilot & Tra cứu RAG Quy chế Nội bộ | FloatingCopilot, AiChat | **45 SP** | FE-Sprint 06 |
| **FE-EPIC-07** | Dashboard Điều hành BI, Heatmap & Báo cáo Động | ExecutiveBI, ReportBuilder | **55 SP** | FE-Sprint 07 |
| **FE-EPIC-08** | Tối ưu Hiệu năng, Đạt chuẩn WCAG AA & Đóng gói PWA | Performance, Accessibility | **45 SP** | FE-Sprint 08 |
| **TỔNG CỘNG** | **Toàn bộ 8 Epics Frontend Vue.js 3** | | **403 SP** | **8 Sprints** |

---

## 3. Danh mục User Stories Chi tiết

### FE-EPIC-01: Khung Ứng dụng, Xác thực RBAC & Shell Giao diện
- **FE-US-01:** Khởi tạo kiến trúc Vue 3 Vite TypeScript, cấu hình Tailwind CSS Design Tokens (`--color-primary`, `--color-background`) theo phong cách Swiss Minimalist & Bento Grid (8 SP).
- **FE-US-02:** Trang Đăng nhập & Xác thực JWT Bearer, tự động chuyển hướng theo phân quyền vai trò (Role-based Routing) (8 SP).
- **FE-US-03:** Axios Interceptor tự động gắn Header `Authorization: Bearer <token>` và `X-Correlation-ID` cho mọi request (5 SP).
- **FE-US-04:** Layout App Shell chuẩn: Sidebar thu gọn mở rộng có nhớ trạng thái `localStorage`, Header cố định, Breadcrumb điều hướng (8 SP).
- **FE-US-05:** Cây Menu phân quyền động (Dynamic RBAC Navigation) tự ẩn các mục người dùng không có quyền truy cập (8 SP).
- **FE-US-06:** Chuyển đổi Dark/Light Mode tức thời với lưu cấu hình cá nhân (5 SP).
- **FE-US-07:** Modal Thông tin Tài khoản Cá nhân, Đổi mật khẩu & Đăng xuất an toàn (8 SP).

### FE-EPIC-02: Phân hệ Cây Cơ cấu Tổ chức, Danh bạ & Phân quyền
- **FE-US-08:** Sơ đồ Cây Tổ chức Tập đoàn trực quan (Interactive Org Tree), phóng to thu nhỏ và lọc theo chi nhánh/phòng ban (13 SP).
- **FE-US-09:** Danh bạ Nhân sự Doanh nghiệp với Virtual Scrolling (cuộn mượt mà cho 1,000 nhân viên, tìm kiếm P99 < 30ms) (13 SP).
- **FE-US-10:** Modal Thiết lập Ký thay / Ủy quyền Phê duyệt Công tác kèm chọn phạm vi và thời hạn hiệu lực (8 SP).
- **FE-US-11:** Ma trận Phân quyền Vai trò (Role-Permission Matrix) dạng bảng lưới tương tác nhanh (11 SP).

### FE-EPIC-03: Động cơ Biểu mẫu Động Form.io & Cascading Loading
- **FE-US-12:** Component bọc Form.io Vue 3 Renderer tùy biến CSS theo Design Tokens của hệ thống (13 SP).
- **FE-US-13:** Cơ chế Cascading Dropdowns tự động tải theo trường cha `{{ data.parentId }}` kèm `clearOnRefresh: true` và `customConditional` (13 SP).
- **FE-US-14:** Bảng phản hồi lỗi nhập liệu trực tiếp (Inline Field Validation) với thông báo tiếng Việt thân thiện (8 SP).
- **FE-US-15:** Tải lên tệp đính kèm hóa đơn/hợp đồng kéo thả (Drag & Drop) với thanh tiến trình tải lên OneDrive (13 SP).
- **FE-US-16:** Tự động lưu bản nháp hồ sơ (Auto-save draft) vào LocalStorage / Cache chống mất dữ liệu khi mất kết nối mạng (8 SP).

### FE-EPIC-04: Hộp thư Công việc, Luồng Duyệt & Nhúng BPMN Canvas
- **FE-US-17:** Hộp thư Công việc Cần xử lý (Task Inbox) hỗ trợ phân loại tab: Cần duyệt, Đã xử lý, Được ủy quyền, Theo dõi (13 SP).
- **FE-US-18:** Bảng dữ liệu tác vụ phân trang, sắp xếp theo hạn SLA, hiển thị Badge cảnh báo SLA nguy cấp (13 SP).
- **FE-US-19:** Màn hình Chi tiết Hồ sơ Trình ký: Hiển thị form đã điền (Readonly/Editable theo bước), tệp đính kèm và thông tin người nộp (13 SP).
- **FE-US-20:** Thanh tác vụ Ký duyệt đa năng: Nút Duyệt, Từ chối kèm nhập lý do, Chuyển tiếp (Delegate/Transfer) và Ký số (8 SP).
- **FE-US-21:** Dòng thời gian Lịch sử Phê duyệt (Approval History Timeline) ghi nhận chi tiết thời gian, người ký và ý kiến chỉ đạo (8 SP).
- **FE-US-22:** Nhúng Trình xem Quy trình Đồ họa (Elsa Studio / BPMN Visual Canvas) thể hiện bước hiện tại đang chạy (5 SP).

### FE-EPIC-05: Thông báo Thời gian thực SignalR & Cổng Bản tin
- **FE-US-23:** Dịch vụ Pinia Store kết nối WebSocket SignalR Hub, tự động kết nối lại khi rớt mạng (Reconnection resilience) (8 SP).
- **FE-US-24:** Chuông thông báo (Notification Bell) hiển thị Badge đếm số lượng tác vụ chưa đọc trên thanh tiêu đề (8 SP).
- **FE-US-25:** Drawer trượt danh sách thông báo nổi bật với phân loại: Giao việc mới, Cảnh báo SLA trễ hạn, Tin tức công ty (8 SP).
- **FE-US-26:** Toast Notifications góc phải màn hình bật lên khi có sự kiện `TaskAssigned` hoặc `WorkflowApproved` (5 SP).
- **FE-US-27:** Cổng Bản tin Truyền thông Nội bộ (News Portal) hiển thị bài viết nổi bật, danh mục và lượt đọc (11 SP).
- **FE-US-28:** Màn hình Chi tiết Bài viết Tin tức với Typography chuẩn hóa, thư viện ảnh và tài liệu đính kèm (8 SP).

### FE-EPIC-06: Trợ lý Ảo AI Copilot & Tra cứu RAG Quy chế Nội bộ
- **FE-US-29:** Widget Nổi Trợ lý AI (Floating Copilot Widget) thu gọn ở góc dưới màn hình với phím tắt nhanh (`Ctrl + Space`) (8 SP).
- **FE-US-30:** Giao diện Trò chuyện Hội thoại (Chat Interface) với hiệu ứng gõ chữ thời gian thực (Markdown Streaming) (13 SP).
- **FE-US-31:** Thẻ Trích dẫn Quy chế Doanh nghiệp (Citation Card) bấm vào mở Drawer xem nguyên văn Điều/Khoản quy định nội bộ (11 SP).
- **FE-US-32:** Bộ chuyển đổi Nhà cung cấp AI (Provider Switcher: Gemini, ChatGPT, Claude) và danh sách gợi ý câu hỏi nghiệp vụ mẫu (8 SP).
- **FE-US-33:** Lịch sử trò chuyện và nút Đánh giá Hài lòng (Thums up/down) phản hồi chất lượng câu trả lời của AI (5 SP).

### FE-EPIC-07: Dashboard Điều hành BI, Heatmap & Báo cáo Động
- **FE-US-34:** Bảng điều khiển Giám đốc (Executive BI Dashboard) cấu trúc Bento Grid với 4 chỉ số KPI: Tỷ lệ duyệt đúng hạn, Thời gian xử lý trung bình, Tổng hồ sơ, Tác vụ trễ hạn (13 SP).
- **FE-US-35:** Bản đồ Nhiệt Phát hiện Nút thắt Cổ chai (Bottleneck Heatmap Chart) trực quan hóa các bước duyệt chậm nhất (13 SP).
- **FE-US-36:** Bảng Xếp hạng Năng suất Nhân sự (Productivity Scorecards) với bộ lọc theo phòng ban và thời gian (8 SP).
- **FE-US-37:** Trình dựng Báo cáo Động (Dynamic Report Builder) cho phép người dùng tự kéo thả chọn cột, điều kiện lọc và xem trước dữ liệu sống (13 SP).
- **FE-US-38:** Xuất Báo cáo Excel định dạng OpenXML (.xlsx) chuẩn hóa màu sắc và tiêu đề chỉ với 1 click (8 SP).

### FE-EPIC-08: Tối ưu Hiệu năng, Đạt chuẩn WCAG AA & Đóng gói PWA
- **FE-US-39:** Tối ưu hóa Bundle Size: Áp dụng Route Lazy-loading, dynamic import, nén Gzip/Brotli, điểm Lighthouse $\ge 90$ (11 SP).
- **FE-US-40:** Kiểm định Khả năng Tiếp cận WCAG 2.2 AA: Kiểm tra độ tương phản $\ge 4.5:1$, bẫy phím (Focus Trap) trong Modals, hỗ trợ Screen Reader (11 SP).
- **FE-US-41:** Kiểm thử Đa Thiết bị (Cross-Device Testing): Đảm bảo hiển thị hoàn hảo trên màn hình 375px, 768px, 1024px, 1440px không bị lỗi tràn ngang (11 SP).
- **FE-US-42:** Cấu hình Progressive Web App (PWA) với Service Worker lưu offline shell và thông báo đẩy (Push Notifications) (8 SP).
- **FE-US-43:** Đóng gói Docker Container với Nginx tối ưu hóa bảo mật OWASP Headers (Cache-Control, CSP, HSTS) sẵn sàng triển khai Production (5 SP).

---

## 4. Tiêu chí Hoàn thành (Definition of Done - DoD)
Một User Story Frontend được xem là Hoàn thành (Done) khi đáp ứng đủ 6 tiêu chí:
1. **TypeScript 100%:** Không dùng kiểu `any`, có interface rõ ràng cho tất cả Request/Response DTOs.
2. **Tuân thủ Design System:** Sử dụng đúng bảng màu, kiểu chữ, viền bo và khoảng cách từ Skill 02 (`02-frontend-ui-ux-pro-max`).
3. **Responsive & Không lỗi hiển thị:** Hoạt động tốt trên 4 mốc màn hình chuẩn (375px, 768px, 1024px, 1440px).
4. **Tiêu chuẩn Tiếp cận (Accessibility):** Có `cursor: pointer` cho phần tử bấm được, có focus ring khi điều hướng bằng bàn phím, độ tương phản đạt tối thiểu 4.5:1.
5. **Icon System Chuẩn:** Tuyệt đối không dùng Emoji làm biểu tượng hệ thống; sử dụng SVG vector (Lucide Icons).
6. **Không lỗi Console:** Không có warning hoặc error nào trên Developer Tools Console.

