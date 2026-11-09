# 🏢 Enterprise BPM, Dynamic Form.io & Workflow Designer Architecture

Tài liệu đặc tả các mẫu thiết kế và tích hợp giao diện doanh nghiệp chuyên sâu: Dynamic Form.io, Cascading Master-Detail, Nhúng Visual Workflow Designer và Real-time Notification Badges.

---

## 1. Dynamic Forms & Form.io Cascading Loading Pattern

### 1.1. Nguyên lý Hoạt động
Trong các ứng dụng quy trình nội bộ, biểu mẫu thường có các trường dữ liệu phụ thuộc nhau theo cấu trúc phân cấp (Cascading / Master-Detail):
- **Trường cha (Parent Field):** Ví dụ chọn *Khối / Ban* (`departmentId`) hoặc *Tỉnh / Thành phố* (`provinceCode`).
- **Trường con (Child Field):** Tự động lọc danh sách tương ứng, ví dụ *Phòng ban / Chức danh* (`positionId`) hoặc *Quận / Huyện* (`districtCode`).

### 1.2. Tiêu chuẩn JSON Schema của Form.io
```json
{
  "key": "positionId",
  "type": "select",
  "label": "Chức danh / Vị trí duyệt",
  "dataSrc": "url",
  "data": {
    "url": "/api/org/positions?departmentId={{ data.departmentId }}"
  },
  "valueProperty": "id",
  "template": "<span>{{ item.title }} (Mã: {{ item.code }})</span>",
  "refreshOn": "departmentId",
  "clearOnRefresh": true,
  "validate": {
    "required": true
  },
  "customConditional": "show = !!data.departmentId;"
}
```

### 1.3. Các Quy tắc Bắt buộc:
1. **`refreshOn: "fieldName"`:** Kích hoạt tải lại dữ liệu khi trường cha thay đổi.
2. **`clearOnRefresh: true`:** **Bắt buộc có.** Khi người dùng đổi trường cha, giá trị đã chọn ở trường con phải bị xóa ngay lập tức để tránh tình trạng dữ liệu "rác" hoặc không khớp logic nghiệp vụ.
3. **`customConditional`:** Chỉ hiển thị hoặc kích hoạt trường con khi trường cha đã có giá trị (`!!data.parentId`).
4. **Bảo mật Server-side:** Không tin tưởng hoàn toàn client-side cascading. Phía backend (FluentValidation) luôn phải xác minh xem `positionId` có thực sự thuộc về `departmentId` đã gửi hay không (Anti-Tampering).

---

## 2. Elsa Workflows v3 Studio Designer Embedding

### 2.1. Nhúng qua Web Components
Elsa Studio cung cấp Web Components sẵn sàng nhúng vào bất kỳ ứng dụng SPA hoặc Razor/MVC nào:
```html
<!-- Nạp tài nguyên Elsa Studio -->
<link rel="stylesheet" href="/_content/Elsa.Studio.Core/elsa-studio.css">
<script src="/_content/Elsa.Studio.Core/elsa-studio.js"></script>

<!-- Thẻ Designer -->
<elsa-workflow-definition-editor 
    workflow-definition-id="@Model.DefinitionId" 
    server-url="@Model.ElsaServerUrl" 
    monaco-lib-path="/_content/Elsa.Studio.Core/monaco-editor/min">
</elsa-workflow-definition-editor>
```

### 2.2. Nhúng qua IFrame cách ly an toàn (Sandbox Mode)
Nếu ứng dụng frontend sử dụng framework độc lập (React, Angular, Vue) hoặc môi trường micro-frontends:
```html
<iframe 
    id="elsa-designer-frame"
    src="/elsa/designer?definitionId=123"
    style="width: 100%; height: calc(100vh - 120px); border: none;"
    sandbox="allow-scripts allow-same-origin allow-forms allow-popups">
</iframe>
```
- **Xác thực JWT:** Truyền Token xác thực qua `postMessage` hoặc qua Header trung gian Reverse Proxy.

---

## 3. Giao diện Doanh nghiệp (AdminLTE 3 / Enterprise SPA) & Real-time Badges

### 3.1. Cấu trúc Layout Chuẩn
- **Sidebar Điều hướng:** Hỗ trợ collapse, ghi nhớ trạng thái mở/đóng trong `localStorage`.
- **Top Navigation Bar:**
  - Bộ chọn vai trò người dùng (Role Switcher).
  - Chuông thông báo (Notification Bell) hiển thị Badge số lượng tác vụ cần duyệt.
  - Nút chuyển đổi Dark/Light Mode.
- **Breadcrumb Navigation:** Thể hiện rõ vị trí người dùng trong cây phân cấp hệ thống.

### 3.2. Real-time Status Badges qua SignalR / WebSocket
Cập nhật tức thời số lượng công việc đến hạn SLA mà không cần người dùng phải bấm F5:
```javascript
const connection = new signalR.HubConnectionBuilder()
    .withUrl("/hubs/workflow-notifications")
    .withAutomaticReconnect([0, 2000, 5000, 10000])
    .build();

connection.on("TaskAssigned", function (task) {
    // 1. Tăng bộ đếm badge
    incrementBadgeCount("#pending-tasks-badge");
    // 2. Hiển thị Toast thông báo nổi góc phải
    toastr.info(`Nhiệm vụ mới: ${task.title} (Hạn: ${task.dueDate})`);
    // 3. Cập nhật dòng mới vào Data Table nếu đang ở trang Inbox
    if (window.refreshTaskTable) window.refreshTaskTable();
});
```

