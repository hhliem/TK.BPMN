# ⚡ Frontend Sprint 04: Workflow Task Inbox, Approvals & BPMN Visualizer

---

## 1. Thông tin Chung về FE-Sprint 04

- **Tên Sprint:** Frontend Sprint 04 — Workflow Task Inbox, Document Approvals, History Timeline & BPMN Visualizer
- **Thời gian Thực hiện:** Tuần 7 - Tuần 8 (10 ngày làm việc)
- **Mục tiêu Sprint (Sprint Goal):**
  > Xây dựng Hộp thư Công việc Trình ký (Task Inbox) đa tab với bộ lọc thông minh và chỉ báo SLA nguy cấp, Màn hình Chi tiết Hồ sơ Trình ký 2 cột hiện đại, Thanh tác vụ Phê duyệt đa năng (Duyệt, Từ chối kèm lý do bắt buộc, Chuyển tiếp, Ký số), Dòng thời gian Lịch sử Phê duyệt (Approval History Timeline) và Nhúng Trình xem Đồ họa Quy trình (Elsa Studio / BPMN Visual Canvas) thể hiện trực quan bước đang xử lý.
- **Tổng Story Points cam kết:** **60 SP**
- **Tổng Giờ Kỹ thuật khả dụng (Capacity):** **300 Giờ** (Team 3 Frontend Devs + 1 QA + Tech Lead hỗ trợ)

---

## 2. Sprint Backlog & Acceptance Criteria (Gherkin)

### FE-US-17 & FE-US-18: Hộp thư Công việc & Cảnh báo Hạn SLA (26 SP)
- **User Story:** Là Lãnh đạo / Cán bộ xử lý, tôi muốn có hộp thư công việc phân loại rõ ràng các việc cần làm gấp, việc được người khác ủy quyền và cảnh báo màu sắc về thời hạn SLA để không bỏ sót hồ sơ.
- **Acceptance Criteria:**
  ```gherkin
  Scenario: Điều hướng tab và phân loại danh sách công việc
    Given người dùng truy cập trang Hộp thư Công việc /workflow/inbox
    When người dùng bấm chuyển giữa các tab: "Cần duyệt", "Đã xử lý", "Được ủy quyền", "Theo dõi"
    Then bảng dữ liệu nạp danh sách tác vụ tương ứng từ API GET /api/workflow/tasks?tab=...
    And hiển thị số đếm công việc còn tồn đọng (Badge counter) trên từng tab.

  Scenario: Hiển thị cảnh báo SLA theo thời gian thực
    Given một tác vụ có thời hạn hoàn thành còn dưới 2 giờ làm việc
    When tác vụ hiển thị trên danh sách
    Then Badge trạng thái SLA đổi sang màu đỏ nhấp nháy "Sắp trễ hạn" kèm đồng hồ đếm ngược
    And các tác vụ trễ hạn được tự động đẩy lên đầu danh sách theo sắp xếp ưu tiên.
  ```

### FE-US-19 & FE-US-20: Màn hình Chi tiết Hồ sơ & Thanh Tác vụ Ký duyệt Đa năng (21 SP)
- **User Story:** Là Người phê duyệt, tôi muốn xem toàn bộ dữ liệu tờ trình, các tệp chứng từ đính kèm và có thanh công cụ ký duyệt nhanh chóng (Đồng ý, Trả lại, Chuyển tiếp).
- **Acceptance Criteria:**
  ```gherkin
  Scenario: Ký duyệt hồ sơ thành công
    Given Người phê duyệt mở hồ sơ trình ký tại /workflow/tasks/{id}
    When Người phê duyệt kiểm tra dữ liệu form và bấm nút "Phê duyệt"
    Then hệ thống mở Dialog xác nhận kèm ô nhập ý kiến chỉ đạo (tùy chọn)
    And sau khi bấm Xác nhận, API POST /api/workflow/tasks/{id}/approve được thực thi
    And giao diện hiển thị Toast thành công và tự động chuyển tiếp sang hồ sơ cần duyệt tiếp theo.

  Scenario: Từ chối hồ sơ bắt buộc nhập lý do
    Given Người phê duyệt phát hiện hóa đơn không hợp lệ
    When bấm nút "Từ chối / Yêu cầu bổ sung"
    Then Modal bật lên yêu cầu nhập "Lý do từ chối" (bắt buộc tối thiểu 10 ký tự)
    And nếu để trống hoặc nhập < 10 ký tự, nút "Xác nhận từ chối" bị vô hiệu hóa (disabled)
    And khi nhập đủ lý do và xác nhận, hồ sơ được trả về người tạo kèm thông báo nguyên do.
  ```

### FE-US-21 & FE-US-22: Lịch sử Phê duyệt & Nhúng Sơ đồ BPMN Trực quan (13 SP)
- **User Story:** Là người theo dõi hồ sơ, tôi muốn xem toàn bộ tiến trình lịch sử ai đã duyệt lúc nào và nhìn trực quan trên sơ đồ quy trình đồ họa xem hồ sơ đang dừng tại bước nào.
- **Acceptance Criteria:**
  ```gherkin
  Scenario: Hiển thị dòng thời gian lịch sử phê duyệt
    Given hồ sơ đã trải qua 2 cấp duyệt (Trưởng phòng và Kế toán trưởng)
    When người dùng mở tab "Lịch sử phê duyệt"
    Then dòng thời gian (Timeline) hiển thị đầy đủ avatar, tên người ký, chức vụ, hành động (Đã duyệt), ý kiến ghi chú và dấu thời gian chính xác
    And bước tiếp theo (Giám đốc tài chính) hiển thị trạng thái "Đang chờ xử lý" màu vàng.

  Scenario: Trực quan hóa tiến độ trên sơ đồ Elsa Studio BPMN Canvas
    Given người dùng bấm vào tab "Sơ đồ Quy trình"
    When Canvas quy trình được render
    Then các bước đã hoàn thành có viền xanh lá cây (Success)
    And bước hiện tại đang chờ duyệt có viền vàng phát sáng nhấp nháy (Active pulse)
    And các bước chưa tới có màu xám nhạt (Pending).
  ```

---

## 3. Danh mục Task Kỹ thuật Chi tiết (Tasks Breakdown)

| Mã Task | Phân hệ / Tầng | Hạng mục Kỹ thuật | Mô tả Chi tiết Công việc Cần Code | Giờ (h) | Người Phụ trách |
|:---:|:---:|---|---|:---:|:---:|
| **TSK-FE-401** | Task Inbox | Inbox Shell & Tabs Navigation | Dựng `TaskInboxView.vue` với hệ thống Tabs: Cần duyệt, Đã duyệt, Ủy quyền, Theo dõi kèm badge counter số lượng. | 16 | Frontend Dev 1 |
| **TSK-FE-402** | Task Inbox | Advanced Data Table | Dựng bảng dữ liệu tác vụ phân trang server-side, sắp xếp theo cột ngày nộp, mức độ ưu tiên, người tạo, loại hồ sơ. | 20 | Frontend Dev 2 |
| **TSK-FE-403** | Task Inbox | SLA Countdown Badge | Viết component `SlaBadge.vue` tự động tính thời gian còn lại theo giờ làm việc, đổi màu xanh/vàng/đỏ tương ứng. | 14 | Frontend Dev 2 |
| **TSK-FE-404** | Document Detail | 2-Column Responsive Layout | Dựng `DocumentDetailView.vue` bố cục 2 cột (Cột trái: Form dữ liệu & Tệp đính kèm; Cột phải: Thông tin tóm tắt & Lịch sử). | 20 | Frontend Dev 1 |
| **TSK-FE-405** | Document Detail | Step-based Form State | Điều khiển trạng thái Form.io Renderer: Đặt `disabled: true` cho dữ liệu của người nộp, mở trường ghi chú cho người duyệt hiện tại. | 16 | Frontend Dev 1 |
| **TSK-FE-406** | Document Detail | Document Attachments List | Dựng danh mục tài liệu đính kèm kèm icon định dạng, dung lượng, nút Tải về toàn bộ dạng ZIP hoặc Xem trước từng tệp. | 14 | Frontend Dev 3 |
| **TSK-FE-407** | Approval Actions | Multi-Action Bar Component | Dựng thanh công cụ nổi cố định cuối trang (`ApprovalActionBar.vue`): Duyệt, Từ chối, Yêu cầu làm rõ, Chuyển tiếp, Ký số. | 18 | Frontend Dev 2 |
| **TSK-FE-408** | Approval Actions | Reject & Clarify Modals | Dựng Modal từ chối với ràng buộc nhập lý do tối thiểu 10 ký tự, chọn lý do mẫu từ dropdown và đính kèm hướng dẫn sửa. | 14 | Frontend Dev 3 |
| **TSK-FE-409** | Approval Actions | Delegate & Transfer Modal | Dựng Modal chuyển giao việc cho cán bộ khác kèm ô tìm kiếm nhân sự và nhập lý do chuyển tiếp. | 14 | Frontend Dev 3 |
| **TSK-FE-410** | Approval History | Timeline Component | Dựng component `ApprovalTimeline.vue` trực quan hóa dòng sự kiện: Người duyệt, avatar, chức danh, hành động, comment và timestamp. | 18 | Frontend Dev 2 |
| **TSK-FE-411** | BPMN Visualizer | Elsa Studio Canvas Integration | Nhúng trình hiển thị quy trình BPMN bằng Web Component hoặc IFrame an toàn, highlight bước hiện tại đang chờ duyệt. | 24 | Tech Lead |
| **TSK-FE-412** | Digital Signature | Cloud PKI / USB Token UI | Dựng modal ký số điện tử: hiển thị chứng thư số, tên chủ thể chứng thư, nhà cấp phát và trạng thái ký hợp lệ. | 18 | Frontend Dev 1 |
| **TSK-FE-413** | QA Testing | Unit Tests Inbox & Approvals | Viết Unit Tests cho SLA countdown helper, permission check trên thanh action bar và form validation logic (>40 test cases). | 20 | QA Engineer |
| **TSK-FE-414** | QA Testing | E2E Tests Complete Approval Flow | Kịch bản E2E: Mở Inbox -> Lọc tác vụ sắp trễ hạn -> Mở hồ sơ -> Nhập ý kiến và bấm Duyệt -> Kiểm tra cập nhật dòng thời gian. | 24 | QA Engineer |
| **TỔNG** | | | **Tổng số 14 Tasks Kỹ thuật** | **250h** | |

---

## 4. Kế hoạch Đánh giá & Tiêu chí Release FE-Sprint 04

### 4.1. Sprint Review / Demo Checklist
- [ ] **Demo 1:** Mở Hộp thư Công việc, chuyển đổi qua lại giữa các tab mượt mà, số lượng hồ sơ trên badge đồng bộ chính xác với số bản ghi trong bảng.
- [ ] **Demo 2:** Các hồ sơ có SLA còn dưới 2 giờ hiển thị badge đỏ rực; hồ sơ bình thường hiển thị badge xanh lá; sắp xếp theo hạn SLA đưa việc gấp lên đầu bảng.
- [ ] **Demo 3:** Mở một hồ sơ chi tiết $\rightarrow$ Cột trái hiển thị toàn bộ form dữ liệu với chế độ chỉ đọc không cho sửa đổi các trường gốc của người nộp.
- [ ] **Demo 4:** Bấm nút "Từ chối" $\rightarrow$ Modal yêu cầu nhập lý do; thử gõ "sai" (3 ký tự) $\rightarrow$ Nút Xác nhận vẫn bị khóa; gõ đủ 12 ký tự $\rightarrow$ Nút sáng lên và gửi API thành công.
- [ ] **Demo 5:** Chuyển sang tab "Sơ đồ Quy trình" $\rightarrow$ Canvas hiển thị sơ đồ luồng BPMN, bước hiện tại đang phát sáng viền vàng; tab "Lịch sử" hiển thị đúng từng bước duyệt trước đó.

### 4.2. Đánh giá Rủi ro Kỹ thuật FE-Sprint 04
| Rủi ro Kỹ thuật | Mức độ | Biện pháp Phòng ngừa |
|---|:---:|---|
| **BPMN Canvas tải chậm hoặc nặng bộ nhớ:** Khi quy trình phức tạp có nhiều nhánh rẽ | Cao | Tối ưu hóa bundle Elsa Studio, sử dụng cơ chế Lazy Loading chỉ nạp thư viện đồ họa khi người dùng bấm vào tab Sơ đồ. |
| **Double Submit khi người dùng bấm Duyệt nhiều lần:** Gây lỗi trùng lặp phê duyệt | Trung bình | Tự động vô hiệu hóa (disabled) nút bấm ngay sau click đầu tiên và hiển thị loading spinner cho đến khi API phản hồi. |

