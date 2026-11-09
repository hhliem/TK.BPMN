# 🚀 Báo Cáo Nghiệm Thu Phát Triển Module 03: WorkflowEngine

---

## 1. Tổng Quan & Phạm Vi Triển Khai

Căn cứ theo tài liệu đặc tả kiến trúc [Module 03: WorkflowEngine](file:///d:/My%20Project/TK.BPMN/documents/03-workflow-engine.md) và kế hoạch [Sprint 03](file:///d:/My%20Project/TK.BPMN/plan/sprint-03-workflow-documents.md), phân hệ **WorkflowEngine** đóng vai trò "trái tim điều phối" toàn bộ quy trình trình ký doanh nghiệp đa cấp, bảo đảm tính toàn vẹn trạng thái, khả năng phục hồi và lịch sử bất biến có chữ ký số.

Module đã được xây dựng theo chuẩn mực **Clean Architecture** và mô hình **Modular Monolith** gồm 4 dự án độc lập:
1. [`EnterpriseBpm.Workflow.Domain`](file:///d:/My%20Project/TK.BPMN/src/Modules/Workflow/EnterpriseBpm.Workflow.Domain/): Chứa thực thể cốt lõi, Enums, ngoại lệ nghiệp vụ và dịch vụ bảo mật chữ ký số.
2. [`EnterpriseBpm.Workflow.Application`](file:///d:/My%20Project/TK.BPMN/src/Modules/Workflow/EnterpriseBpm.Workflow.Application/): Chứa các DTOs, hợp đồng (Contracts), Integration Events và Event Handlers.
3. [`EnterpriseBpm.Workflow.Infrastructure`](file:///d:/My%20Project/TK.BPMN/src/Modules/Workflow/EnterpriseBpm.Workflow.Infrastructure/): Chứa DbContext cho schema `[workflow]`, dịch vụ phân giải người duyệt, khóa phân tán Redis, background worker quét SLA và lưu trữ lịch sử bất biến.
4. [`EnterpriseBpm.Workflow.Presentation`](file:///d:/My%20Project/TK.BPMN/src/Modules/Workflow/EnterpriseBpm.Workflow.Presentation/): Cung cấp bộ REST API Controllers cho nộp phiếu, quản lý nhiệm vụ chờ duyệt và tra cứu lịch sử duyệt.

---

## 2. Chi Tiết Các Hạng Mục Kỹ Thuật Đã Hoàn Thành

### 2.1. Thiết kế Cơ sở Dữ liệu (Schema [workflow])
Triển khai trong [`WorkflowDbContext.cs`](file:///d:/My%20Project/TK.BPMN/src/Modules/Workflow/EnterpriseBpm.Workflow.Infrastructure/Persistence/WorkflowDbContext.cs):
- **Bảng `[workflow].[WorkflowSubmissions]`**:
  - Lưu trữ vòng đời phiếu trình ký: `WorkflowInstanceId` (Unique), `WorkflowDefinitionId`, `FormKey`, `SubmittedByUserId`, `RequesterOrgUnitId`, `FormDataJson`, `CurrentStatus`, `Priority`, `SubmittedAt`, `CompletedAt`.
  - Tích hợp `IAuditable` và `ISoftDeletable` kế thừa từ `SharedKernel` với Global Query Filter (`DeletedAt == null`).
  - Đánh chỉ mục hiệu năng cao: `IX_WorkflowSubmissions_Status`, `IX_WorkflowSubmissions_OrgUnit`.
- **Bảng `[workflow].[ApprovalTasks]`**:
  - Quản lý nhiệm vụ duyệt đang chờ: `ActivityId`, `TargetRole`, `AssignedToUserId`, `DelegatedFromUserId`, `Status` (Pending/Completed/Cancelled/Reassigned), `Decision`, `DueDate`, `IsEscalated`.
  - Đánh chỉ mục: `IX_ApprovalTasks_UserPending` (`AssignedToUserId`, `Status`), `IX_ApprovalTasks_WorkflowInstance`.
- **Bảng `[workflow].[ApprovalHistory]`**:
  - Lưu vết toàn trình bất biến (Immutable): `StepOrder`, `StepName`, `ActorUserId`, `OnBehalfOfUserId`, `DelegationId`, `Decision`, `Comment`, `ActionTimestamp`, `DigitalSignatureHash`.
  - Đánh chỉ mục: `IX_ApprovalHistory_Workflow`, `IX_ApprovalHistory_Actor`.

### 2.2. Dịch vụ Bảo Mật Chữ Ký Số Bất Biến (Digital Signature Service)
Triển khai trong [`DigitalSignatureService.cs`](file:///d:/My%20Project/TK.BPMN/src/Modules/Workflow/EnterpriseBpm.Workflow.Domain/Services/DigitalSignatureService.cs) & [`ApprovalHistoryService.cs`](file:///d:/My%20Project/TK.BPMN/src/Modules/Workflow/EnterpriseBpm.Workflow.Infrastructure/Services/ApprovalHistoryService.cs):
- Tự động sinh mã băm SHA-256 xác thực:
  $$\text{DigitalSignatureHash} = \text{SHA256}(\text{WorkflowInstanceId} + \text{StepOrder} + \text{ActorUserId} + \text{Decision} + \text{ActionTimestamp} + \text{Salt})$$
- Kiểm soát toàn vẹn: Cung cấp phương thức `VerifyHash()` phát hiện bất kỳ sự can thiệp dữ liệu trái phép nào vào lịch sử phê duyệt.
- Salt được cấu hình an toàn qua `IConfiguration` (`Security:SignatureSalt`), tuân thủ quy tắc Không hardcode thông tin bảo mật.

### 2.3. Khóa Phân Tán Redis Chống Duyệt Trùng Lặp (Idempotency & RedLock)
Triển khai trong [`RedisDistributedLockService.cs`](file:///d:/My%20Project/TK.BPMN/src/Modules/Workflow/EnterpriseBpm.Workflow.Infrastructure/Services/RedisDistributedLockService.cs):
- Khóa phân tán dựa trên trừu tượng `IDistributedCache` chuẩn hóa do tầng Host (`EnterpriseBpm.ApiHost`) quản lý tập trung qua `Microsoft.Extensions.Caching.StackExchangeRedis`.
- Hỗ trợ kết nối máy chủ Redis độc lập (Docker Container, Dedicated VM hoặc Managed Cloud Redis).
- Áp dụng khóa theo key `lock:approval_task:{taskId}` với TTL 10 giây.
- Kết hợp `SemaphoreSlim` cục bộ để tối ưu hóa tải truy vấn đến Redis bus.
- Tự động từ chối và ném `DistributedLockAcquisitionException` hoặc `TaskAlreadyProcessedException` nếu người dùng click đúp hoặc gửi request đồng thời.

### 2.4. Phân Giải Người Duyệt Đa Cấp & Ủy Quyền (Task Dispatcher)
Triển khai trong [`ApprovalTaskDispatcher.cs`](file:///d:/My%20Project/TK.BPMN/src/Modules/Workflow/EnterpriseBpm.Workflow.Infrastructure/Services/ApprovalTaskDispatcher.cs):
- Tích hợp liền mạch với Public Contracts của module `Org`:
  - Gọi `IOrgHierarchyService.GetApproversByRoleAsync()` hoặc `GetDirectManagerIdAsync()` để tìm người duyệt theo cơ cấu tổ chức doanh nghiệp.
  - Gọi `IDelegationResolver.ResolveActualApproverAsync()` kiểm tra ủy quyền hợp lệ trong khung thời gian và phạm vi FormKey.
  - Ghi nhận `DelegatedFromUserId` khi phát hiện trường hợp ký thay.
- Tự động phát `TaskAssignedEvent` qua MediatR.

### 2.5. Giao Dịch Đồng Bộ Trạng Thái Nguyên Tử (Atomic State Synchronization)
Triển khai trong [`ApprovalTaskService.cs`](file:///d:/My%20Project/TK.BPMN/src/Modules/Workflow/EnterpriseBpm.Workflow.Infrastructure/Services/ApprovalTaskService.cs) & [`WorkflowRuntimeEngine.cs`](file:///d:/My%20Project/TK.BPMN/src/Modules/Workflow/EnterpriseBpm.Workflow.Infrastructure/Services/WorkflowRuntimeEngine.cs):
- Toàn bộ hành động: Cập nhật Task $\rightarrow$ Ghi nhận Lịch sử duyệt bất biến $\rightarrow$ Resume Bookmark và cập nhật Submission $\rightarrow$ Phát Sự kiện tích hợp đều được bao bọc trong một Database Transaction duy nhất (`IUnitOfWork`).
- Nếu có bất kỳ lỗi nào xảy ra trong chuỗi xử lý, giao dịch sẽ tự động Rollback toàn bộ trạng thái để ngăn ngừa phân rã dữ liệu.

### 2.6. Quản Lý Cam Kết Chất Lượng Dịch Vụ (SLA Escalation Worker)
Triển khai trong [`SlaEscalationBackgroundWorker.cs`](file:///d:/My%20Project/TK.BPMN/src/Modules/Workflow/EnterpriseBpm.Workflow.Infrastructure/Workers/SlaEscalationBackgroundWorker.cs):
- Background Service quét định kỳ các nhiệm vụ duyệt bị trễ hạn (`DueDate < DateTime.UtcNow && Status == Pending && !IsEscalated`).
- Tự động đánh dấu `IsEscalated = true` và phát `TaskSlaBreachedEvent` qua MediatR để các phân hệ Notification (gửi tin khẩn) và Analytics (tính toán KPI) xử lý tức thì.

### 2.7. Xử Lý Nhân Sự Nghỉ Việc (Orphaned Task Prevention)
Triển khai trong [`EmployeeDeactivatedWorkflowHandler.cs`](file:///d:/My%20Project/TK.BPMN/src/Modules/Workflow/EnterpriseBpm.Workflow.Application/Handlers/EmployeeDeactivatedWorkflowHandler.cs):
- Lắng nghe `EmployeeDeactivatedEvent` từ module Org.
- Tự động thu hồi và điều chuyển các nhiệm vụ Pending của nhân sự thôi việc sang Quản lý trực tiếp kế nhiệm hoặc hủy task an toàn nếu không tìm thấy quản lý.

### 2.8. Danh Mục REST API Endpoints
Triển khai trong thư mục Presentation Controllers:
- **`WorkflowSubmissionsController`**:
  - `POST /api/workflows/submissions`: Nộp hồ sơ/phiếu trình ký mới.
  - `GET /api/workflows/submissions/{id}`: Tra cứu chi tiết phiếu trình ký.
  - `GET /api/workflows/submissions/instance/{instanceId}`: Tra cứu phiếu theo mã thực thi quy trình.
  - `POST /api/workflows/submissions/{id}/cancel`: Người nộp đơn hủy phiếu.
- **`ApprovalTasksController`**:
  - `GET /api/workflows/tasks/pending`: Danh sách nhiệm vụ cần duyệt của user đang đăng nhập (kèm phân trang, lọc trạng thái).
  - `GET /api/workflows/tasks/{id}`: Chi tiết một nhiệm vụ duyệt.
  - `POST /api/workflows/tasks/{id}/decision`: Phê duyệt / Từ chối / Yêu cầu bổ sung kèm bình luận.
- **`ApprovalHistoryController`**:
  - `GET /api/workflows/history/{instanceId}`: Toàn bộ dòng thời gian duyệt kèm cờ kiểm tra tính toàn vẹn chữ ký số `IsValidSignature`.

---

## 3. Kết Quả Kiểm Thử & Đảm Bảo Chất Lượng

### 3.1. Unit Tests Nghiệp Vụ Cốt Lõi
Tạo mới bộ kiểm thử trong [`WorkflowDomainTests.cs`](file:///d:/My%20Project/TK.BPMN/tests/EnterpriseBpm.SharedKernel.UnitTests/WorkflowDomainTests.cs):
- **Test 1:** `DigitalSignatureService_ShouldComputeValidHash_AndVerifySuccessfully` $\rightarrow$ Xác nhận tính toán và xác thực mã băm SHA-256 64 ký tự hex chính xác.
- **Test 2:** `DigitalSignatureService_ShouldFailVerification_WhenTampered` $\rightarrow$ Xác nhận phát hiện ngay lập tức dữ liệu bị sửa đổi (kết quả trả về `false`).
- **Test 3:** `WorkflowSubmission_StateTransitions_ShouldWorkCorrectly` $\rightarrow$ Xác nhận luồng chuyển trạng thái và ngăn chặn hành động không hợp lệ khi phiếu đã hủy.
- **Test 4:** `ApprovalTask_ShouldPreventDuplicateCompletion_WhenAlreadyProcessed` $\rightarrow$ Xác nhận ném `TaskAlreadyProcessedException` khi thao tác lặp lại trên cùng một task.

### 3.2. Biên Dịch Toàn Bộ Solution
Biên dịch kiểm tra toàn bộ 29 dự án trong `EnterpriseBpmPlatform.sln`:
```text
Build succeeded.
    0 Warning(s)
    0 Error(s)
```

---

## 4. Đánh Giá Tuân Thủ Kiến Trúc & Quy Tắc Quản Trị

| Tiêu Chí Quản Trị | Trạng Thái | Mô Tả Đánh Giá |
|---|:---:|---|
| **Zero Hardcoded Config** |  Đạt | Toàn bộ ConnectionString, Redis URI, SignatureSalt đọc từ `IConfiguration`. Ném `InvalidOperationException` ngay khi thiếu key. |
| **Fail-Fast Policy** |  Đạt | Không sử dụng cơ chế fallback ngầm `(localdb)` hay nuốt exception. |
| **Module Isolation** |  Đạt | Schema riêng `[workflow]`, DbContext độc lập, không join chéo bảng sang schema khác. |
| **Public Contracts** |  Đạt | Giao tiếp với Org qua `IDelegationResolver` và `IOrgHierarchyService`. Giao tiếp sự kiện qua `BaseIntegrationEvent`. |
| **Audit & Soft-Delete** |  Đạt | Áp dụng `IAuditable` và `ISoftDeletable` trên `WorkflowSubmission` với `AuditableFieldsInterceptor`. |
| **Immutability & Integrity**|  Đạt | `ApprovalHistory` không có hàm update, bảo vệ bằng mã băm SHA-256. |
| **Concurrency Control** |  Đạt | Redis RedLock kiểm soát tranh chấp đồng thời trên từng Task Id. |

