# 🛡️ Frontend Sprint 08: Performance Tuning, WCAG Accessibility, PWA & Production Hardening

---

## 1. Thông tin Chung về FE-Sprint 08

- **Tên Sprint:** Frontend Sprint 08 — Performance Optimization, WCAG 2.2 AA Accessibility, Cross-device PWA & Docker Production Hardening
- **Thời gian Thực hiện:** Tuần 15 - Tuần 16 (10 ngày làm việc)
- **Mục tiêu Sprint (Sprint Goal):**
  > Hoàn thiện tối ưu hóa hiệu năng ứng dụng Frontend Vue 3 (Route Lazy-loading, Code-splitting, nén Brotli, đạt điểm Google Lighthouse $\ge 90$), kiểm định toàn diện khả năng tiếp cận WCAG 2.2 AA (Color contrast $\ge 4.5:1$, Keyboard Focus Trap, Screen Reader ARIA), tinh chỉnh Responsive đa thiết bị (375px - 1440px), cấu hình Progressive Web App (PWA) và đóng gói Production Dockerfile với Nginx bảo mật chuẩn OWASP Security Headers.
- **Tổng Story Points cam kết:** **45 SP**
- **Tổng Giờ Kỹ thuật khả dụng (Capacity):** **225 Giờ** (Team 3 Frontend Devs + 1 QA)

---

## 2. Sprint Backlog & Acceptance Criteria (Gherkin)

### FE-US-39: Tối ưu Hóa Hiệu Năng & Điểm Google Lighthouse $\ge 90$ (11 SP)
- **User Story:** Là người dùng, tôi muốn ứng dụng tải nhanh chóng dưới 1.5 giây trên mạng thông thường, chuyển trang tức thì không có độ trễ giật lag.
- **Acceptance Criteria:**
  ```gherkin
  Scenario: Đạt chỉ số hiệu năng Google Lighthouse $\ge 90$
    Given ứng dụng Vue 3 được build ở chế độ Production (npm run build)
    When chạy công cụ đo kiểm Google Lighthouse trên trang Dashboard và Task Inbox
    Then điểm số Performance đạt $\ge 90$ điểm
    And chỉ số Largest Contentful Paint (LCP) $\le 2.0\text{ giây}$
    And chỉ số First Input Delay (FID) $\le 100\text{ms}$
    And chỉ số Cumulative Layout Shift (CLS) $\le 0.05$ (không xê dịch layout).
  ```

### FE-US-40: Đạt Chuẩn Tiếp Cận Toàn Diện WCAG 2.2 AA (11 SP)
- **User Story:** Là nhân viên sử dụng bàn phím hoặc công cụ hỗ trợ người khiếm thị (Screen Reader), tôi có thể điều hướng, duyệt hồ sơ và thao tác bình thường mà không cần dùng chuột.
- **Acceptance Criteria:**
  ```gherkin
  Scenario: Điều hướng bàn phím và bẫy tiêu điểm (Focus Trap) trong Modal
    Given người dùng mở một Modal xác nhận hoặc Drawer thông báo
    When người dùng nhấn phím "Tab" liên tục
    Then tiêu điểm bàn phím chỉ di chuyển tuần hoàn bên trong các phần tử của Modal, không lọt ra ngoài nền
    And nhấn phím "Escape" lập tức đóng Modal và trả tiêu điểm về nút đã kích hoạt trước đó
    And tất cả các nút bấm icon đều có thuộc tính "aria-label" rõ ràng.

  Scenario: Độ tương phản màu sắc đạt chuẩn $\ge 4.5:1$
    Given kiểm tra bằng công cụ tự động axe-core
    When rà soát trên toàn bộ các trang giao diện (cả Light Mode và Dark Mode)
    Then tỷ lệ tương phản giữa chữ và nền (Contrast Ratio) đạt tối thiểu 4.5:1 cho chữ thường và 3:1 cho chữ lớn
    And không có bất kỳ cảnh báo vi phạm mức độ Serious hoặc Critical nào.
  ```

### FE-US-41 & FE-US-42: Trải nghiệm Đa Thiết Bị & Cấu hình PWA (19 SP)
- **User Story:** Là Lãnh đạo thường xuyên đi công tác, tôi muốn mở hệ thống trên điện thoại hoặc máy tính bảng để duyệt hồ sơ với giao diện gọn gàng, có thể cài đặt như ứng dụng native app (PWA).
- **Acceptance Criteria:**
  ```gherkin
  Scenario: Hiển thị mượt mà trên màn hình di động (375px)
    Given người dùng mở ứng dụng trên màn hình điện thoại (chiều rộng 375px)
    When truy cập trang Hộp thư Công việc và Chi tiết Hồ sơ
    Then Sidebar tự động chuyển thành Mobile Drawer trượt ra khi bấm Menu Hamburger
    And bảng dữ liệu tự động chuyển thành dạng danh sách thẻ (Card view) dễ đọc
    And không xuất hiện thanh cuộn ngang khó chịu (No horizontal overflow).

  Scenario: Cài đặt và sử dụng tính năng PWA Offline Shell
    Given người dùng truy cập trang web qua trình duyệt Chrome hoặc Safari trên di động
    When banner "Cài đặt ứng dụng Enterprise BPMN" xuất hiện
    Then người dùng bấm "Cài đặt" để đưa biểu tượng app ra màn hình chính (Home screen)
    And khi mất mạng Internet đột ngột, Service Worker phục vụ Offline Shell kèm thông báo thân thiện.
  ```

### FE-US-43: Đóng gói Docker Container & Bảo mật OWASP Headers (5 SP)
- **User Story:** Là DevOps Engineer, tôi muốn đóng gói ứng dụng Frontend vào một Docker Image siêu nhẹ chạy trên nền Nginx với cấu hình bảo mật nghiêm ngặt để triển khai an toàn lên Kubernetes / Server.
- **Acceptance Criteria:**
  ```gherkin
  Scenario: Build Docker Image và kiểm tra Security Headers
    Given Dockerfile multi-stage build cho dự án Vue 3
    When chạy lệnh docker build và khởi chạy container với Nginx Alpine
    Then kích thước Docker image $\le 30\text{MB}$
    And phản hồi HTTP trả về đầy đủ các headers bảo mật OWASP:
      | Header Name | Giá trị Yêu cầu |
      | Content-Security-Policy | default-src 'self' ... |
      | X-Frame-Options | SAMEORIGIN |
      | X-Content-Type-Options | nosniff |
      | Strict-Transport-Security | max-age=31536000; includeSubDomains |
      | Referrer-Policy | strict-origin-when-cross-origin |
  ```

---

## 3. Danh mục Task Kỹ thuật Chi tiết (Tasks Breakdown)

| Mã Task | Phân hệ / Tầng | Hạng mục Kỹ thuật | Mô tả Chi tiết Công việc Cần Code | Giờ (h) | Người Phụ trách |
|:---:|:---:|---|---|:---:|:---:|
| **TSK-FE-801** | Performance | Code Splitting & Dynamic Imports | Cấu hình `vite.config.ts` chia nhỏ chunks: `vendor-core` (vue, pinia, router), `vendor-ui` (radix), `vendor-viz` (echarts), `vendor-formio`. | 16 | Tech Lead |
| **TSK-FE-802** | Performance | Brotli & Gzip Compression | Tích hợp plugin `vite-plugin-compression` tự động sinh các tệp nén trước `.br` và `.gz` cho toàn bộ tài nguyên tĩnh. | 10 | Frontend Dev 1 |
| **TSK-FE-803** | Performance | Lighthouse Performance Tuning | Rà soát chỉ số Web Vitals, tối ưu tải trước font chữ (Font preloading), lazy-load hình ảnh, loại bỏ CSS thừa (PurgeCSS). | 18 | Frontend Dev 1 |
| **TSK-FE-804** | Accessibility | axe-core Automated Audit | Tích hợp `@axe-core/playwright` vào bộ kiểm thử tự động, quét phát hiện và sửa toàn bộ lỗi độ tương phản màu sắc. | 16 | QA Engineer |
| **TSK-FE-805** | Accessibility | Keyboard Focus Trap & Rings | Bổ sung Focus Trap (`focus-trap` / Radix Primitive) cho tất cả Dialogs, Drawers và thiết lập viền focus nổi bật (`focus-visible:ring-2`). | 14 | Frontend Dev 2 |
| **TSK-FE-806** | Accessibility | Screen Reader ARIA Polish | Bổ sung đầy đủ nhãn ngữ nghĩa `aria-label`, `aria-describedby`, `role="status"` cho badge cảnh báo và nút đóng modal. | 12 | Frontend Dev 2 |
| **TSK-FE-807** | Responsive | Mobile Layout Polish (375px) | Hoàn thiện chuyển đổi Sidebar thành Mobile Slide Menu, tối ưu hóa bảng biểu sang dạng thẻ Cards trên màn hình $< 768px$. | 18 | Frontend Dev 3 |
| **TSK-FE-808** | PWA | Service Worker & Manifest | Cấu hình `vite-plugin-pwa` với chiến lược caching: StaleWhileRevalidate cho tài nguyên tĩnh, NetworkFirst cho API dữ liệu. | 16 | Frontend Dev 2 |
| **TSK-FE-809** | PWA | App Icons & Install Banner | Tạo bộ icon PWA đầy đủ kích thước (192x192, 512x512, maskable) và banner hướng dẫn cài đặt lên màn hình chính. | 10 | Frontend Dev 3 |
| **TSK-FE-810** | DevOps / Docker | Multi-stage Dockerfile | Viết `Dockerfile` 2 tầng (Node.js 22 Alpine để build Vite và Nginx 1.27 Alpine để serve tĩnh), tối ưu dung lượng dưới 30MB. | 12 | Tech Lead |
| **TSK-FE-811** | DevOps / Docker | Hardened Nginx Configuration | Viết `nginx.conf` hỗ trợ SPA fallback `try_files $uri /index.html`, cấu hình nén Brotli/Gzip và các OWASP Security Headers. | 12 | Tech Lead |
| **TSK-FE-812** | QA Testing | Cross-Browser & Audit Pipeline | Chạy tự động kiểm thử trên Chrome, Firefox, Safari (WebKit) và kiểm tra điểm Lighthouse CI trong quy trình CI/CD. | 18 | QA Engineer |
| **TỔNG** | | | **Tổng số 12 Tasks Kỹ thuật** | **172h** | |

---

## 4. Kế hoạch Đánh giá & Tiêu chí Release FE-Sprint 08

### 4.1. Sprint Review / Demo Checklist
- [ ] **Demo 1:** Chạy báo cáo Google Lighthouse trên bản Production build $\rightarrow$ Cả 4 chỉ số (Performance, Accessibility, Best Practices, SEO) đều đạt trên 90 điểm.
- [ ] **Demo 2:** Rút chuột, chỉ dùng phím Tab và Enter để đăng nhập, điều hướng menu, mở một hồ sơ và bấm duyệt $\rightarrow$ Thao tác hoàn toàn thuận lợi, viền focus ring hiển thị rõ ràng tại mọi bước.
- [ ] **Demo 3:** Bật chế độ Responsive của trình duyệt ở kích thước iPhone SE (375px) $\rightarrow$ Menu thu gọn thành Hamburger, danh sách hồ sơ trình ký hiển thị dạng thẻ đẹp mắt, không có thanh cuộn ngang.
- [ ] **Demo 4:** Mở ứng dụng trên Chrome di động $\rightarrow$ Trình duyệt hiện nút "Thêm vào màn hình chính"; sau khi cài đặt, mở app lên toàn màn hình (Standalone) không có thanh địa chỉ của trình duyệt.
- [ ] **Demo 5:** Khởi chạy Docker container $\rightarrow$ Kiểm tra bằng lệnh `curl -I http://localhost` xác nhận có đầy đủ các header an ninh: `Content-Security-Policy`, `X-Frame-Options: SAMEORIGIN`, `X-Content-Type-Options: nosniff`.

### 4.2. Đánh giá Rủi ro Kỹ thuật FE-Sprint 08
| Rủi ro Kỹ thuật | Mức độ | Biện pháp Phòng ngừa |
|---|:---:|---|
| **Service Worker cache phiên bản cũ:** Người dùng không nhận được tính năng mới khi deploy | Cao | Sử dụng chiến lược `autoUpdate` của `vite-plugin-pwa` kết hợp thông báo: "Đã có phiên bản mới. Bấm vào đây để cập nhật". |
| **Chính sách CSP quá chặt chặn tài nguyên bên thứ ba:** Lỗi chặn font hoặc avatar OneDrive | Trung bình | Kiểm tra kỹ lưỡng các domain whitelist trong header `Content-Security-Policy` (`img-src 'self' data: https:`, `font-src 'self' data:`). |

