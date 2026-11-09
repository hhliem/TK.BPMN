# 🚀 Sprint 04: Multichannel Notification, Internal News & System Settings

---

## 1. Thông tin Chung về Sprint 04

- **Tên Sprint:** Sprint 04 — Cổng Thông báo Đa kênh, Truyền thông Nội bộ & Cấu hình Hệ thống
- **Thời gian Thực hiện:** Tuần 7 - Tuần 8 (10 ngày làm việc)
- **Mục tiêu Sprint (Sprint Goal):**
  > Xây dựng Cổng thông báo tập trung đa kênh (`Notification`) hỗ trợ gửi Email qua MS Graph Mail API, Teams Webhook, Zalo ZNS và SMS Gateway với hàng đợi ngầm có cơ chế tự động thử lại (Exponential Backoff) và chống trùng lặp tin; phát triển phân hệ Truyền thông nội bộ (`InternalNews`) hỗ trợ hẹn giờ đăng bài và xác nhận bắt buộc đã đọc tin; hoàn thiện phân hệ Quản trị cấu hình thương hiệu (`SystemSettings`) với lịch sử thay đổi cấu hình.
- **Tổng Story Points cam kết:** **72 SP**
- **Tổng Giờ Kỹ thuật khả dụng (Capacity):** **340 Giờ** (Team 5 Dev + 1 QA)

---

## 2. Sprint Backlog & Acceptance Criteria (Gherkin)

### US-44 & US-45: Mẫu Thông báo Động & Gửi Email O365 qua Graph API (16 SP)
- **Acceptance Criteria:**
  ```gherkin
  Scenario: Tự động gửi email thông báo khi có nhiệm vụ mới
    Given một phiếu trình ký vừa được gửi và gán cho người duyệt B
    When sự kiện TaskAssignedEvent được phát sinh
    Then Notification Module render nội dung theo mẫu TPL_TASK_ASSIGNED
    And email được gửi thành công đến hòm thư O365 của người duyệt B qua Graph Mail API.
  ```

### US-46 & US-47: Teams Webhook & Hàng đợi Thử lại Lũy tiến (13 SP)
- **Acceptance Criteria:**
  ```gherkin
  Scenario: Tự động thử lại khi Gateway gửi tin gặp sự cố
    Given dịch vụ gửi tin tạm thời bị mất kết nối mạng
    When tiến trình gửi tin thất bại lần 1
    Then hệ thống tự động thử lại lần 2 sau 1 phút, lần 3 sau 5 phút và cập nhật trạng thái NotificationLogs.
  ```

### US-50, US-51 & US-52: Bản tin Nội bộ, Hẹn giờ & Xác nhận Đã đọc (24 SP)
- **Acceptance Criteria:**
  ```gherkin
  Scenario: Nhân viên xác nhận đã đọc thông cáo bắt buộc
    Given một bài viết thông cáo có gắn cờ IsMandatoryRead = 1
    When nhân viên mở đọc bài viết và bấm nút "Tôi đã đọc và hiểu rõ nội dung"
    Then hệ thống lưu bản ghi vào [news].[NewsReadReceipts] và cập nhật tỷ lệ đọc của phòng ban.
  ```

### US-53 & US-54: Cấu hình Thương hiệu White-label & Lưu vết Sửa đổi (19 SP)
- **Acceptance Criteria:**
  ```gherkin
  Scenario: Quản trị viên thay đổi Logo công ty
    Given người dùng có vai trò SuperAdmin
    When thay đổi giá trị cấu hình "Brand:LogoUrl" kèm lý do thay đổi
    Then Logo mới được cập nhật ngay trên giao diện, cache IMemoryCache bị xóa
    And bảng [settings].[SettingChangeHistory] ghi nhận giá trị cũ, giá trị mới và ID người sửa.
  ```

---

## 3. Danh mục Task Kỹ thuật Chi tiết (Tasks Breakdown)

| Mã Task | Module | Hạng mục Kỹ thuật | Mô tả Chi tiết Công việc Cần Code | Giờ (h) | Người Phụ trách |
|:---:|:---:|---|---|:---:|:---:|
| **TSK-401** | Notif | Database Schema [notification] | Tạo bảng `[notification].[NotificationTemplates]`, `[notification].[NotificationLogs]` kèm Filtered Index chống trùng lặp `IX_NotificationLogs_Dedup`. | 6 | Backend Dev 1 |
| **TSK-402** | Notif | Fluid Template Rendering Engine | Tích hợp thư viện `Fluid` biên dịch mẫu thông báo động, khử nhiễm HTML Sanitization chống mã độc. | 8 | Backend Dev 1 |
| **TSK-403** | Notif | O365 Graph Mail Provider | Triển khai `O365GraphMailProvider` gửi email HTML qua mailbox dịch vụ doanh nghiệp với Microsoft Graph SDK. | 10 | Backend Dev 1 |
| **TSK-404** | Notif | Teams Webhook Provider | Xây dựng Provider gửi Adaptive Cards tương tác trực tiếp vào kênh Microsoft Teams. | 8 | Backend Dev 2 |
| **TSK-405** | Notif | Bounded Channel Queue Worker | Triển khai Background Worker sử dụng `System.Threading.Channels` quản lý hàng đợi gửi tin ngầm, chống tràn bộ nhớ. | 10 | Backend Dev 2 |
| **TSK-406** | Notif | Event Handlers Multi-Channel | Viết MediatR Handlers lắng nghe `TaskAssignedEvent`, `WorkflowApprovedEvent`, `WorkflowRejectedEvent`, `WorkflowCancelledEvent`. | 10 | Backend Dev 2 |
| **TSK-407** | News | Database Schema [news] | Tạo bảng `[news].[NewsArticles]`, `[news].[NewsReadReceipts]` kèm Index hỗ trợ bài viết hẹn giờ đăng. | 6 | Backend Dev 3 |
| **TSK-408** | News | Scheduled News Publisher Worker | Xây dựng Background Worker quét mỗi 5 phút tự động chuyển trạng thái `Published` cho bài viết đến giờ hẹn và `Archived` cho bài hết hạn. | 10 | Backend Dev 3 |
| **TSK-409** | News | Mandatory Read Compliance API | Xây dựng API ghi nhận xác nhận đọc tin (`NewsReadReceipts`) và API thống kê tỷ lệ tuân thủ đọc tin theo phòng ban. | 8 | Backend Dev 3 |
| **TSK-410** | Settings | Database Schema [settings] | Tạo bảng `[settings].[SystemSettings]`, `[settings].[SettingChangeHistory]` có cờ mã hóa nhạy cảm `IsEncrypted`. | 6 | Backend Dev 1 |
| **TSK-411** | Settings | At-Rest Encryption & Caching | Tích hợp `IDataProtectionProvider` mã hóa ClientSecret/API Keys và quản lý cache `IMemoryCache` tự động làm mới. | 8 | Backend Dev 1 |
| **TSK-412** | Settings | Setting Audit Trail Service | Triển khai service tự động ghi vết thay đổi tham số cấu hình vào bảng `SettingChangeHistory`. | 6 | Backend Dev 1 |
| **TSK-413** | UI | Notification Templates Manager | Dựng giao diện quản lý mẫu thông báo, có khung preview kết quả render template theo dữ liệu mẫu. | 14 | Frontend Dev 1 |
| **TSK-414** | UI | Internal News Portal & Editor | Tích hợp trình soạn thảo Rich Text (CKEditor 5), quản lý danh mục tin tức và giao diện xem bài viết tin tức. | 18 | Frontend Dev 2 |
| **TSK-415** | UI | Mandatory Read Modal & Tracking | Xây dựng modal bắt buộc xác nhận đã đọc đối với các thông cáo chính sách quan trọng của doanh nghiệp. | 12 | Frontend Dev 2 |
| **TSK-416** | UI | White-labeling Settings Page | Dựng trang cấu hình thương hiệu: Upload Logo, Favicon, chọn bảng màu chủ đạo (Color Picker) và xem trước trực tiếp. | 14 | Frontend Dev 1 |
| **TSK-417** | UI | Notification Center Bell Widget | Xây dựng widget hình chiếc chuông trên Header thông báo realtime các tin tức và cập nhật nhiệm vụ mới. | 14 | Frontend Dev 1 |
| **TSK-418** | QA | Test Hàng đợi Gửi Email & Retry | Viết test case giả lập ngắt mạng O365, kiểm tra hàng đợi thử lại đúng lịch trình lũy tiến (1m, 5m, 15m) và log lỗi. | 16 | QA Engineer |
| **TSK-419** | QA | Test Hẹn giờ Xuất bản Tin tức | Kiểm tra worker xuất bản đúng thời điểm hẹn giờ và phát sinh sự kiện `NewsPublishedEvent` sang notification. | 12 | QA Engineer |
| **TSK-420** | QA | Test Bảo mật Mã hóa Cấu hình | Kiểm tra trực tiếp dữ liệu trong SQL Server để xác nhận các API Keys đã được mã hóa không thể đọc chuỗi gốc. | 10 | QA Engineer |
| **TỔNG** | | | **Tổng số 20 Tasks Kỹ thuật** | **206h** | |

---

## 4. Kế hoạch Đánh giá & Tiêu chí Release Sprint 04

### 4.1. Sprint Review / Demo Checklist
- [ ] **Demo 1:** Gửi một phiếu trình ký $\rightarrow$ Ngay lập tức người duyệt nhận được Email thông báo từ hòm thư doanh nghiệp với định dạng HTML chuyên nghiệp và link mở phiếu trực tiếp.
- [ ] **Demo 2:** Mô phỏng lỗi gửi mail $\rightarrow$ Kiểm tra bảng `NotificationLogs` thấy `RetryCount = 1`, sau đó tự động gửi lại thành công.
- [ ] **Demo 3:** Soạn thảo bài viết "Chính sách Bảo hiểm 2027", hẹn giờ đăng lúc 10:00 $\rightarrow$ Đúng 10:00 bài viết tự động chuyển sang `Published` và phát thông báo.
- [ ] **Demo 4:** Nhân viên mở bài thông cáo bắt buộc $\rightarrow$ Bấm nút "Xác nhận đã đọc" $\rightarrow$ Admin xem báo cáo thấy tỷ lệ đọc phòng ban tăng lên.
- [ ] **Demo 5:** Admin đổi màu sắc chủ đạo từ Xanh sang Đỏ và đổi Logo tập đoàn $\rightarrow$ Toàn bộ giao diện AdminLTE đổi màu và logo ngay lập tức.

### 4.2. Đánh giá Rủi ro Sprint 04
| Rủi ro Kỹ thuật | Mức độ | Biện pháp Phòng ngừa |
|---|:---:|---|
| Bị Microsoft Graph Rate Limit (HTTP 429) khi gửi mail hàng loạt | Cao | Dùng Bounded Channel khống chế tối đa 20 emails/giây và áp dụng Polly Retry Jitter. |
| XSS trong nội dung bài viết tin tức Rich Text | Cao | Sử dụng thư viện `Ganss.Xss.HtmlSanitizer` làm sạch toàn bộ thẻ HTML trước khi lưu vào DB. |

