# 🔔 Frontend Sprint 05: Real-time SignalR Notifications & Internal News

---

## 1. Thông tin Chung về FE-Sprint 05

- **Tên Sprint:** Frontend Sprint 05 — SignalR Realtime Hub, Notification Drawer & Internal News Portal
- **Thời gian Thực hiện:** Tuần 9 - Tuần 10 (10 ngày làm việc)
- **Mục tiêu Sprint (Sprint Goal):**
  > Thiết lập kết nối WebSocket liên tục qua `@microsoft/signalr` với cơ chế tự động kết nối lại khi mất mạng, xây dựng Chuông thông báo & Drawer trượt đa phân loại (Tác vụ mới, Cảnh báo SLA, Tin tức), Toast tương tác tức thời khi nhận sự kiện nghiệp vụ từ Backend, và Cổng Bản tin Truyền thông Nội bộ (News Portal) hiển thị tin tức điều hành, chính sách mới với Typography chuẩn mực từ Skill 02.
- **Tổng Story Points cam kết:** **48 SP**
- **Tổng Giờ Kỹ thuật khả dụng (Capacity):** **240 Giờ** (Team 3 Frontend Devs + 1 QA)

---

## 2. Sprint Backlog & Acceptance Criteria (Gherkin)

### FE-US-23 & FE-US-24: Kết nối WebSocket SignalR & Chuông Thông báo (16 SP)
- **User Story:** Là người dùng, tôi muốn nhận được thông báo ngay lập tức khi có hồ sơ mới cần duyệt mà không cần phải bấm F5 tải lại trang, đồng thời biểu tượng chuông hiển thị số lượng việc chưa đọc.
- **Acceptance Criteria:**
  ```gherkin
  Scenario: Kết nối SignalR Hub tự động duy trì phiên
    Given người dùng đã đăng nhập thành công vào hệ thống
    When Pinia NotificationStore khởi tạo kết nối HubConnection tới /hubs/notifications
    Then kết nối WebSocket được thiết lập thành công kèm Access Token hợp lệ
    And nếu đường truyền Internet bị ngắt, thư viện tự động thử kết nối lại theo chu kỳ (0s, 2s, 10s, 30s)
    And khi có mạng trở lại, biểu tượng trạng thái kết nối chuyển xanh tức thì.

  Scenario: Nhận thông báo giao việc thời gian thực
    Given người dùng đang làm việc trên một trang bất kỳ
    When Backend phát ra sự kiện "TaskAssignedEvent" cho người dùng này
    Then số đếm trên Chuông thông báo (Unread Badge) tăng thêm 1
    And biểu tượng chuông kích hoạt hiệu ứng rung nhẹ (gentle ringing animation).
  ```

### FE-US-25 & FE-US-26: Drawer Thông báo Phân loại & Toasts Tương tác (13 SP)
- **User Story:** Là nhân viên, tôi muốn mở danh sách thông báo xem chi tiết các việc được giao hoặc cảnh báo gấp, có thể bấm vào thông báo để chuyển thẳng đến màn hình duyệt hồ sơ.
- **Acceptance Criteria:**
  ```gherkin
  Scenario: Mở Drawer trượt xem thông báo và điều hướng nhanh
    Given người dùng bấm vào biểu tượng Chuông thông báo trên Header
    When Drawer trượt ra từ cạnh phải màn hình
    Then danh sách thông báo hiển thị phân chia theo tabs: "Tất cả", "Công việc", "Cảnh báo SLA", "Tin tức"
    And nhấp vào một thông báo chưa đọc sẽ đánh dấu thông báo đó thành "Đã đọc"
    And màn hình tự động chuyển hướng đến chi tiết hồ sơ trình ký tương ứng.

  Scenario: Toast Notification tương tác bật lên góc màn hình
    Given người dùng nhận được thông báo khẩn cấp (Sắp trễ hạn SLA)
    When sự kiện đến client
    Then một Toast màu hổ phách (Amber toast) trượt vào góc trên bên phải màn hình
    And hiển thị tiêu đề hồ sơ và nút "Xử lý ngay"
    And toast tự động biến mất sau 6 giây nếu người dùng không tương tác.
  ```

### FE-US-27 & FE-US-28: Cổng Bản tin Nội bộ & Chi tiết Bài viết (19 SP)
- **User Story:** Là cán bộ nhân viên, tôi muốn theo dõi các bản tin điều hành, quy định mới của công ty với giao diện đẹp mắt, đọc bài viết dễ chịu và tải về các văn bản đính kèm.
- **Acceptance Criteria:**
  ```gherkin
  Scenario: Xem trang chủ Cổng Bản tin Nội bộ
    Given người dùng truy cập trang /news
    When hệ thống tải danh sách tin tức từ API GET /api/news/articles
    Then bài viết quan trọng nhất hiển thị ở khu vực Hero Banner nổi bật
    And các tin tức tiếp theo hiển thị dạng lưới Bento Grid cards kèm ảnh bìa, danh mục, ngày đăng và số lượt xem
    And người dùng có thể lọc tin tức theo danh mục: "Thông báo Điều hành", "Quy chế mới", "Văn hóa Doanh nghiệp".

  Scenario: Đọc chi tiết bài viết tin tức
    Given người dùng bấm vào một bài viết
    When màn hình chi tiết bài viết được nạp
    Then nội dung hiển thị theo chuẩn Typography Swiss Minimalist (font chữ dễ đọc, khoảng cách dòng thoáng)
    And các hình ảnh minh họa có thể bấm vào để phóng to toàn màn hình (Image Lightbox)
    And danh sách văn bản đính kèm có nút tải về trực tiếp.
  ```

---

## 3. Danh mục Task Kỹ thuật Chi tiết (Tasks Breakdown)

| Mã Task | Phân hệ / Tầng | Hạng mục Kỹ thuật | Mô tả Chi tiết Công việc Cần Code | Giờ (h) | Người Phụ trách |
|:---:|:---:|---|---|:---:|:---:|
| **TSK-FE-501** | SignalR Engine | Hub Connection Manager | Viết `useNotificationHubStore.ts` quản lý vòng đời HubConnection, JWT bearer token injection và policy tự động reconnect. | 20 | Tech Lead |
| **TSK-FE-502** | SignalR Engine | Connection Status Dot | Dựng component `SignalRStatusIndicator.vue` hiển thị chấm xanh (Connected), vàng (Reconnecting), đỏ (Disconnected) trên App Header. | 8 | Frontend Dev 2 |
| **TSK-FE-503** | Notification UI | Bell & Animated Badge | Dựng `NotificationBell.vue` với số đếm tác vụ chưa đọc, hiệu ứng pulse rung khi có tin mới và âm thanh clicky tinh tế. | 12 | Frontend Dev 1 |
| **TSK-FE-504** | Notification UI | Slide-over Drawer | Dựng `NotificationDrawer.vue` trượt mượt mà từ phải sang, phân tab thông báo, nút "Đánh dấu tất cả đã đọc" và phân trang cuộn vô tận. | 20 | Frontend Dev 1 |
| **TSK-FE-505** | Notification UI | Interactive Toast System | Tích hợp thư viện thông báo nổi tương tác (`vue-sonner` / Custom Toast) hiển thị thẻ tác vụ kèm nút bấm hành động chuyển trang. | 16 | Frontend Dev 2 |
| **TSK-FE-506** | Notification UI | Sound Notification Service | Viết Web Audio service phát tiếng "ding" nhẹ nhàng khi có thông báo quan trọng, có nút tùy chọn Bật/Tắt âm trong User Settings. | 10 | Frontend Dev 3 |
| **TSK-FE-507** | News Portal | Hero Banner & Bento Grid | Dựng `NewsPortalView.vue` với Hero Banner bài viết ghim, Bento Grid các bài viết mới nhất và thanh tìm kiếm tin tức. | 22 | Frontend Dev 1 |
| **TSK-FE-508** | News Portal | Category Tabs & Search Filter | Dựng thanh chuyển đổi danh mục tin tức và bộ lọc ngày xuất bản, từ khóa tiêu đề (Debounce search 200ms). | 14 | Frontend Dev 2 |
| **TSK-FE-509** | News Portal | Article Detail View | Dựng `NewsArticleDetailView.vue` với Typography tối ưu (Prose Tailwind), thông tin tác giả, thời gian đọc ước tính, nút chia sẻ link nội bộ. | 18 | Frontend Dev 3 |
| **TSK-FE-510** | News Portal | Image Lightbox & Attachments | Tích hợp thư viện xem ảnh toàn màn hình (`vue-easy-lightbox`) và bảng tải tệp quyết định/quy chế đính kèm. | 14 | Frontend Dev 3 |
| **TSK-FE-511** | QA Testing | Unit Tests SignalR & Store | Viết Unit Tests cho Notification Hub store, event listeners dispatching và badge counter calculation (>30 test cases). | 18 | QA Engineer |
| **TSK-FE-512** | QA Testing | E2E Tests Realtime Alerts | Viết kịch bản E2E: Kết nối WebSocket -> Gửi sự kiện giả lập TaskAssigned -> Kiểm tra Toast xuất hiện -> Bấm Toast chuyển đến hồ sơ. | 20 | QA Engineer |
| **TỔNG** | | | **Tổng số 12 Tasks Kỹ thuật** | **192h** | |

---

## 4. Kế hoạch Đánh giá & Tiêu chí Release FE-Sprint 05

### 4.1. Sprint Review / Demo Checklist
- [ ] **Demo 1:** Mở 2 trình duyệt với 2 tài khoản (Người nộp đơn và Người duyệt) $\rightarrow$ Người nộp vừa bấm "Nộp hồ sơ", tài khoản Người duyệt lập tức nhảy chuông rung và hiện Toast "Bạn có hồ sơ mới cần duyệt" trong vòng $\le 500\text{ms}$.
- [ ] **Demo 2:** Ngắt kết nối mạng WiFi của máy $\rightarrow$ Chấm trạng thái trên Header đổi sang màu vàng cảnh báo; cắm lại mạng $\rightarrow$ Hệ thống tự động kết nối lại và chuyển sang chấm xanh không cần tải lại trang.
- [ ] **Demo 3:** Mở Drawer thông báo $\rightarrow$ Danh sách hiển thị rõ ràng thông báo công việc và cảnh báo SLA; bấm nút "Đánh dấu tất cả đã đọc" $\rightarrow$ Số đếm trên chuông về 0 ngay lập tức.
- [ ] **Demo 4:** Vào Cổng Bản tin $\rightarrow$ Bài viết ghim hiển thị nổi bật ở trên cùng; chuyển đổi giữa các danh mục tin bài hoạt động mượt mà không bị giật trang.
- [ ] **Demo 5:** Bấm mở một bài tin tức $\rightarrow$ Hình ảnh bài viết nhấp vào hiển thị lightbox phóng to sắc nét, tài liệu PDF đính kèm bấm tải về bình thường.

### 4.2. Đánh giá Rủi ro Kỹ thuật FE-Sprint 05
| Rủi ro Kỹ thuật | Mức độ | Biện pháp Phòng ngừa |
|---|:---:|---|
| **Rò rỉ bộ nhớ WebSocket (Memory Leak):** Khi chuyển qua lại giữa các view mà không giải phóng SignalR listeners | Cao | Bắt buộc dọn dẹp các sự kiện lắng nghe (`hubConnection.off(...)`) trong hook `onUnmounted` của Vue. |
| **Spam Toast khi nhận nhiều thông báo cùng lúc:** Gây rối màn hình | Trung bình | Cấu hình Toast System chỉ hiển thị tối đa 3 toasts cùng một thời điểm, các thông báo tiếp theo được xếp hàng đợi (Toast Queue). |

