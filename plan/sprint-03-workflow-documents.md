# 🚀 Sprint 03: Elsa BPMN Engine & OneDrive Document Storage

---

## 1. Thông tin Chung về Sprint 03

- **Tên Sprint:** Sprint 03 — Động cơ Quy trình Elsa v3, Quản trị Trình ký & Lưu trữ Tài liệu OneDrive
- **Thời gian Thực hiện:** Tuần 5 - Tuần 6 (10 ngày làm việc)
- **Mục tiêu Sprint (Sprint Goal):**
  > Hoàn thành "trái tim" điều phối luồng quy trình của hệ thống: nhúng Visual Designer Elsa Studio, xây dựng Custom Activity `ApproveFormActivity` tạo Bookmark và Resume quy trình phê duyệt, lưu trữ lịch sử duyệt bất biến (`ApprovalHistory`) với mã băm chữ ký số; tích hợp Microsoft Graph API đẩy toàn bộ file đính kèm trực tiếp lên OneDrive for Business và cung cấp Signed Preview URLs bảo mật.
- **Tổng Story Points cam kết:** **76 SP**
- **Tổng Giờ Kỹ thuật khả dụng (Capacity):** **340 Giờ** (Team 5 Dev + 1 QA)

---

## 2. Sprint Backlog & Acceptance Criteria (Gherkin)

### US-28 & US-29: Elsa Studio Designer & Custom Approval Activity (26 SP)
- **Acceptance Criteria:**
  ```gherkin
  Scenario: Dừng và đánh thức quy trình khi có hành động phê duyệt
    Given một luồng quy trình đang chạy đến Activity "Phê duyệt"
    When Elsa thực thi ApproveFormActivity
    Then một task mới được tạo trong bảng [workflow].[ApprovalTasks] với trạng thái "Pending"
    And Elsa tạo Bookmark dừng quy trình chờ tín hiệu Resume từ API với payload ApprovalResultPayload.
  ```

### US-30, US-31 & US-32: Nộp Phiếu, Danh sách Chờ duyệt & Thao tác Ký (24 SP)
- **Acceptance Criteria:**
  ```gherkin
  Scenario: Người duyệt bấm nút Phê duyệt hồ sơ
    Given người dùng đang có task duyệt hợp lệ trong danh sách Pending Tasks
    When người dùng nhập ý kiến và bấm nút "Đồng ý Phê duyệt"
    Then API kiểm tra khóa phân tán Redis, cập nhật task thành "Completed", ghi nhận vào ApprovalHistory
    And Elsa được Resume để chuyển tiếp sang bước phê duyệt tiếp theo trong quy trình.
  ```

### US-33 & US-34: Lịch sử Duyệt Bất biến & Khóa Phân tán Redis (13 SP)
- **Acceptance Criteria:**
  ```gherkin
  Scenario: Chặn click đúp duyệt trùng lặp
    Given người dùng click 2 lần nút Phê duyệt trong vòng 500ms
    When request thứ 2 gửi đến server
    Then Redis Distributed Lock từ chối request thứ 2 và trả thông báo "Nhiệm vụ đang được xử lý".
  ```

### US-38, US-39 & US-40: Tích hợp OneDrive for Business qua Graph API (21 SP)
- **Acceptance Criteria:**
  ```gherkin
  Scenario: Upload file đính kèm hợp đồng lên OneDrive
    Given người dùng nộp đơn đính kèm file hợp đồng PDF dung lượng 15MB
    When hệ thống xử lý upload
    Then file được truyền stream trực tiếp lên thư mục OneDrive doanh nghiệp qua Microsoft Graph API
    And SQL Server chỉ lưu DriveItemId và đường dẫn metadata, dung lượng DB không bị tăng.
  ```

---

## 3. Danh mục Task Kỹ thuật Chi tiết (Tasks Breakdown)

| Mã Task | Module | Hạng mục Kỹ thuật | Mô tả Chi tiết Công việc Cần Code | Giờ (h) | Người Phụ trách |
|:---:|:---:|---|---|:---:|:---:|
| **TSK-301** | Workflow | Database Schema [workflow] | Tạo bảng `[workflow].[WorkflowSubmissions]`, `[workflow].[ApprovalTasks]`, `[workflow].[ApprovalHistory]` kèm Indexes. | 8 | Backend Dev 1 |
| **TSK-302** | Workflow | Elsa v3 Service Registration | Cấu hình Elsa v3 Engine trong `Program.cs`, nạp các dịch vụ Workflow Management và Workflow Runtime. | 8 | Backend Dev 1 |
| **TSK-303** | Workflow | ApproveFormActivity C# | Triển khai Custom Activity kế thừa `Activity<string>`, tạo Bookmark với name `Approval_{ActivityId}`. | 12 | Backend Dev 1 |
| **TSK-304** | Workflow | Task Dispatcher Service | Triển khai `IApprovalTaskDispatcher` phân giải danh tính người duyệt từ `IOrgHierarchyService` và `IDelegationResolver`. | 10 | Backend Dev 2 |
| **TSK-305** | Workflow | Resume Workflow API | Xây dựng API `/api/workflows/resume` tiếp nhận quyết định duyệt, resume Elsa Bookmark và cập nhật Submission status. | 12 | Backend Dev 1 |
| **TSK-306** | Workflow | Immutable Approval History | Triển khai `IApprovalHistoryService` tự động tính mã băm SHA-256 (`DigitalSignatureHash`) lưu vết hành trình duyệt. | 8 | Backend Dev 2 |
| **TSK-307** | Workflow | Distributed Lock with RedLock | Tích hợp Redis Distributed Lock (`lock:approval_task:{taskId}`) chặn triệt để race condition và duplicate submit. | 6 | Backend Dev 2 |
| **TSK-308** | Document | Database Schema [document] | Tạo bảng `[document].[WorkflowAttachments]` lưu metadata, ID định danh trên Graph API. | 6 | Backend Dev 3 |
| **TSK-309** | Document | Microsoft.Graph SDK Setup | Cấu hình `GraphServiceClient` sử dụng `ClientSecretCredential` xác thực với Azure Entra ID / Microsoft 365 Tenant. | 8 | Backend Dev 3 |
| **TSK-310** | Document | OneDrive Streaming Upload | Triển khai phương thức stream trực tiếp file nhị phân lên cây thư mục `/BPMN_Enterprise_Storage/{Year}/{Month}/{InstanceId}`. | 12 | Backend Dev 3 |
| **TSK-311** | Document | Time-limited Signed URLs | Triển khai API sinh URL xem trước tài liệu có chữ ký số hết hạn trong 15 phút, chặn truy cập public. | 8 | Backend Dev 3 |
| **TSK-312** | Document | Whitelist & File Size Guard | Viết Middleware kiểm tra header `Content-Type`, định dạng đuôi file (.pdf, .docx, .xlsx, ảnh) và dung lượng tối đa $25\text{MB}$. | 6 | Backend Dev 2 |
| **TSK-313** | UI | Elsa Studio Designer View | Nhúng giao diện Elsa Studio BPMN Designer vào trang quản trị quy trình, cấu hình CORS và JWT Authorization. | 18 | Frontend Dev 1 |
| **TSK-314** | UI | Form Submission Portal | Dựng màn hình nộp đơn tích hợp Form.io Renderer và component kéo thả tải lên nhiều file đính kèm. | 18 | Frontend Dev 2 |
| **TSK-315** | UI | Pending Approval Tasks Grid | Dựng bảng danh sách công việc cần duyệt với bộ lọc trạng thái, badge độ ưu tiên và hiển thị thời hạn SLA. | 16 | Frontend Dev 1 |
| **TSK-316** | UI | Approval Action Modal | Dựng modal xem nội dung phiếu, xem file đính kèm và các nút bấm hành động (Đồng ý, Từ chối, Yêu cầu bổ sung). | 14 | Frontend Dev 2 |
| **TSK-317** | UI | Approval Timeline Component | Dựng component hiển thị dòng thời gian (Timeline) các bước đã duyệt, người duyệt, thời gian và ý kiến xử lý. | 12 | Frontend Dev 1 |
| **TSK-318** | QA | End-to-End Workflow Tests | Viết kịch bản test tự động E2E: Tạo đơn $\rightarrow$ Phân công Trưởng phòng $\rightarrow$ Ký thay $\rightarrow$ Duyệt $\rightarrow$ Hoàn tất. | 20 | QA Engineer |
| **TSK-319** | QA | Test Tải & Chống Click Đúp | Sử dụng công cụ kiểm thử tải giả lập 50 requests resume đồng thời cho cùng 1 task để kiểm tra Redis Lock. | 12 | QA Engineer |
| **TSK-320** | QA | Test Tích hợp OneDrive Mock | Viết integration test giả lập tải file, kiểm tra tính toàn vẹn metadata và xử lý lỗi ngắt kết nối Graph API. | 12 | QA Engineer |
| **TỔNG** | | | **Tổng số 20 Tasks Kỹ thuật** | **236h** | |

---

## 4. Kế hoạch Đánh giá & Tiêu chí Release Sprint 03

### 4.1. Sprint Review / Demo Checklist
- [ ] **Demo 1:** Mở Elsa Studio Designer trên AdminLTE $\rightarrow$ Kéo thả thiết kế luồng duyệt 2 cấp (Trưởng phòng $\rightarrow$ Giám đốc).
- [ ] **Demo 2:** Nhân viên nộp đơn "Đề nghị Mua sắm" có đính kèm file báo giá PDF $\rightarrow$ Kiểm tra OneDrive thấy file xuất hiện đúng cây thư mục tháng/năm; kiểm tra SQL Server chỉ thấy metadata nhẹ vài KB.
- [ ] **Demo 3:** Đăng nhập tài khoản Trưởng phòng $\rightarrow$ Thấy task mới xuất hiện trong danh sách Pending Tasks $\rightarrow$ Bấm mở xem trước PDF trực tiếp mượt mà.
- [ ] **Demo 4:** Trưởng phòng bấm "Đồng ý Duyệt" $\rightarrow$ Task chuyển sang cho Giám đốc $\rightarrow$ Kiểm tra bảng `[workflow].[ApprovalHistory]` thấy ghi nhận bước 1 kèm mã băm SHA-256.
- [ ] **Demo 5:** Giám đốc duyệt $\rightarrow$ Trạng thái phiếu đổi thành `Approved`, quy trình hoàn tất.

### 4.2. Đánh giá Rủi ro Sprint 03
| Rủi ro Kỹ thuật | Mức độ | Biện pháp Phòng ngừa |
|---|:---:|---|
| Token Azure Entra ID hết hạn khi upload file lớn | Trung bình | Dùng `ClientSecretCredential` với cơ chế token auto-refresh ngầm của Azure SDK. |
| Mạng chập chờn khi resume Elsa Bookmark | Cao | Sử dụng Database Transaction bọc việc cập nhật Task và Bookmark Resume trong cùng Unit of Work. |

