# 🗃️ Master Product Backlog — Enterprise BPMN Platform

Master Product Backlog tập hợp toàn bộ các Yêu cầu Nghiệp vụ (Epics & User Stories) của nền tảng **Enterprise BPMN Platform**, được phân loại theo độ ưu tiên **MoSCoW (Must-have, Should-have, Could-have, Won't-have)** và ước lượng điểm nỗ lực **Story Points (SP)** theo dãy Fibonacci.

---

## 1. Danh mục Epics Dự án

| Mã Epic | Tên Epic | Module Phụ trách | Schema DB | Tổng SP Dự kiến |
|:---:|---|---|:---:|:---:|
| **EPIC-01** | Kiến trúc Cốt lõi, Kiểm toán & Quản trị Ranh giới | SharedKernel, AuditLog | `[audit]` | **45 SP** |
| **EPIC-02** | Quản trị Phân quyền & Kiểm soát Truy cập Toàn hệ thống | AccessGovernance | `[auth]` | **68 SP** |
| **EPIC-03** | Quản lý Cơ cấu Tổ chức, Chức danh & Danh bạ Nội bộ | OrgManagement | `[org]` | **52 SP** |
| **EPIC-04** | Biểu mẫu Động Form.io, Versioning & Master Data | FormMasterData | `[form]` | **55 SP** |
| **EPIC-05** | Động cơ Thực thi Quy trình Elsa v3 & Quản trị Trình ký | WorkflowEngine | `[workflow]` | **89 SP** |
| **EPIC-06** | Lưu trữ OneDrive Graph API & Tự động Sinh Văn bản Template | DocumentManagement | `[document]` | **50 SP** |
| **EPIC-07** | Cổng Thông báo Tập trung Đa kênh & Tự động Thử lại | Notification | `[notification]` | **42 SP** |
| **EPIC-08** | Bản tin Truyền thông Nội bộ & Cấu hình Thương hiệu | InternalNews, SystemSettings | `[news]`, `[settings]` | **38 SP** |
| **EPIC-09** | Trợ lý Trí tuệ Nhân tạo Local MCP & Tra cứu RAG Quy chế | AiCopilot | `[ai]` | **48 SP** |
| **EPIC-10** | Phân hệ Báo cáo Toàn hệ thống, Performance KPI & SLA | AnalyticsReporting | `[analytics]` | **73 SP** |
| **EPIC-11** | Tối ưu Hiệu năng Cao, Độ Ổn định & Sẵn sàng Vận hành | Cross-Cutting / All Modules | Toàn bộ | **55 SP** |
| **TỔNG CỘNG**| **Toàn bộ 11 Epics Dự án** | | | **615 SP** |

---

## 2. Chi tiết Toàn bộ User Stories trong Product Backlog

### EPIC-01: Kiến trúc Cốt lõi, Kiểm toán & Quản trị Ranh giới
| Mã US | Tên User Story | Mô tả Nghiệp vụ (As a... I want... So that...) | SP | MoSCoW | Sprint |
|:---:|---|---|:---:|:---:|:---:|
| **US-01** | Cấu trúc Solution & SharedKernel | Là Lập trình viên, tôi muốn có cấu trúc project chuẩn Clean Architecture và SharedKernel (BaseEntity, Result Pattern, Guard Clauses) để đảm bảo tính nhất quán toàn hệ thống. | 8 | Must | Sprint 01 |
| **US-02** | ISoftDeletable & Global Query Filter | Là Quản trị viên, tôi muốn các dữ liệu xóa được chuyển sang trạng thái xóa mềm (Soft Delete) có lưu thời gian và người xóa để có thể khôi phục khi cần. | 5 | Must | Sprint 01 |
| **US-03** | IAuditable SaveChangesInterceptor | Là Kiểm toán viên, tôi muốn mọi bản ghi tự động lưu `CreatedAt`, `CreatedBy`, `UpdatedAt`, `UpdatedBy` mà không cần code tay ở từng controller. | 5 | Must | Sprint 01 |
| **US-04** | Tự động Ghi Nhật ký Kiểm toán (AuditLog) | Là Kiểm toán viên, tôi muốn hệ thống tự động ghi vết thay đổi giá trị cũ/mới của mọi Entity vào bảng `[audit].[AuditEntries]` để truy vết gian lận. | 13 | Must | Sprint 01 |
| **US-05** | Trigger Bất biến Chống Sửa Xóa Log | Là Giám đốc Bảo mật, tôi muốn DB có Trigger cấm sửa hoặc xóa dữ liệu kiểm toán để ngăn chặn việc xóa dấu vết kiểm toán trái phép. | 5 | Must | Sprint 01 |
| **US-06** | API Tra cứu & Xuất Excel Lịch sử Kiểm toán | Là Ban Kiểm soát, tôi muốn tra cứu lịch sử thay đổi của một phiếu theo thời gian và xuất file Excel để phục vụ thanh tra. | 8 | Should | Sprint 01 |

---

### EPIC-02: Quản trị Phân quyền & Kiểm soát Truy cập (AccessGovernance)
| Mã US | Tên User Story | Mô tả Nghiệp vụ | SP | MoSCoW | Sprint |
|:---:|---|---|:---:|:---:|:---:|
| **US-07** | Quản lý Danh mục Quyền Hạt nhân | Là Quản trị viên, tôi muốn định nghĩa danh mục quyền chi tiết dạng `resource:action` để kiểm soát chặt chẽ từng tính năng của hệ thống. | 8 | Must | Sprint 01 |
| **US-08** | Quản lý Vai trò & Ma trận Phân quyền | Là Quản trị viên, tôi muốn tạo các vai trò (Role) và tích chọn phân quyền trên ma trận Role-Permission để giao quyền cho từng chức danh. | 13 | Must | Sprint 01 |
| **US-09** | Quản lý Nhóm Bảo mật (Security Groups) | Là Quản trị viên, tôi muốn gom người dùng vào các Security Group (VD: Hội đồng Duyệt chi, Ban Dự án) để gán quyền đồng loạt. | 8 | Should | Sprint 01 |
| **US-10** | Phân quyền Phạm vi Dữ liệu (ABAC Data Scope) | Là Trưởng phòng, tôi muốn cấu hình chỉ cho phép nhân viên xem hồ sơ trong phòng ban của mình (Scope.Department) hoặc chi nhánh (Scope.Division). | 13 | Must | Sprint 01 |
| **US-11** | Cấu hình Hạn mức Phê duyệt Tài chính | Là Giám đốc Tài chính, tôi muốn cấu hình hạn mức duyệt tiền tối đa cho từng chức danh/vai trò để ngăn chặn việc duyệt chi vượt thẩm quyền. | 8 | Must | Sprint 01 |
| **US-12** | Phân quyền Khởi tạo & Xem Biểu mẫu Trình ký | Là Quản trị viên, tôi muốn thiết lập ai được quyền tạo đơn loại nào và ai được xem danh sách đơn loại đó. | 8 | Must | Sprint 01 |
| **US-13** | Phân quyền Cây Menu Giao diện Frontend | Là Người dùng, tôi muốn cây menu AdminLTE tự động ẩn đi các tính năng mà tôi không có quyền truy cập để tránh nhầm lẫn. | 5 | Must | Sprint 01 |
| **US-14** | Hủy Cache Quyền Tức thì trên Redis | Là Chuyên viên An ninh mạng, tôi muốn khi có thay đổi quyền hạn của User thì cache Redis bị xóa ngay lập tức để quyền mới có hiệu lực ngay. | 5 | Must | Sprint 01 |

---

### EPIC-03: Cơ cấu Tổ chức, Chức danh & Danh bạ Nội bộ (OrgManagement)
| Mã US | Tên User Story | Mô tả Nghiệp vụ | SP | MoSCoW | Sprint |
|:---:|---|---|:---:|:---:|:---:|
| **US-15** | Quản lý Cây Đơn vị Tổ chức Đa cấp | Là Chuyên viên HR, tôi muốn quản lý cây phân cấp tổ chức (Tập đoàn $\rightarrow$ Khối $\rightarrow$ Phòng $\rightarrow$ Nhóm) để phục vụ định tuyến duyệt đơn. | 8 | Must | Sprint 02 |
| **US-16** | Chống Vòng lặp Cây Tổ chức | Là Lập trình viên, tôi muốn thuật toán chặn việc chuyển phòng ban cha làm con của phòng ban con (Circular Hierarchy) gây lặp vô tận. | 5 | Must | Sprint 02 |
| **US-17** | Quản lý Danh mục Chức danh Cấp bậc | Là Chuyên viên HR, tôi muốn quản lý danh mục chức danh (Positions: Giám đốc, Trưởng phòng, Chuyên viên) có xếp cấp bậc (Level). | 5 | Must | Sprint 02 |
| **US-18** | Quản lý Hồ sơ Nhân sự & Liên kết O365 | Là Chuyên viên HR, tôi muốn quản lý hồ sơ nhân viên, mã nhân viên, quản lý trực tiếp và liên kết tài khoản O365 UserPrincipalName. | 8 | Must | Sprint 02 |
| **US-19** | Tra cứu Danh bạ Nhân sự Nội bộ (Directory API) | Là Nhân viên, tôi muốn tìm kiếm nhanh số điện thoại máy lẻ, email, chức danh và vị trí ngồi của đồng nghiệp trên portal. | 8 | Should | Sprint 02 |
| **US-20** | Thiết lập Cơ chế Ủy quyền Phê duyệt | Là Trưởng phòng đi công tác, tôi muốn ủy quyền phê duyệt hồ sơ cho Phó phòng trong khoảng thời gian nhất định (Delegation Management). | 13 | Must | Sprint 02 |
| **US-21** | Chống Ủy quyền Vòng tròn & Audit Ký thay | Là Kiểm soát viên, tôi muốn hệ thống chặn việc ủy quyền chéo ($A \rightarrow B \rightarrow A$) và ghi nhận rõ danh tính người ký thay trên hồ sơ. | 5 | Must | Sprint 02 |

---

### EPIC-04: Biểu mẫu Động Form.io & Master Data (FormMasterData)
| Mã US | Tên User Story | Mô tả Nghiệp vụ | SP | MoSCoW | Sprint |
|:---:|---|---|:---:|:---:|:---:|
| **US-22** | Quản lý JSON Schema Form.io Tập trung | Là Quản trị viên, tôi muốn lưu trữ và cập nhật JSON Schema của Form.io trên giao diện quản trị để thay đổi form mà không cần deploy lại code. | 8 | Must | Sprint 02 |
| **US-23** | Quản lý Phiên bản Biểu mẫu (Form Versioning) | Là Người nộp đơn, tôi muốn các phiếu cũ đã nộp giữ nguyên giao diện lúc nộp (Version cũ), phiếu mới áp dụng Version mới nhất (`IsLatest`). | 13 | Must | Sprint 02 |
| **US-24** | Danh mục Master Data Phân cấp Cascading | Là Người nộp đơn, tôi muốn khi chọn Dự án thì danh sách Hạng mục con tự động tải đúng theo dự án đó với tốc độ phản hồi $< 30\text{ ms}$. | 8 | Must | Sprint 02 |
| **US-25** | Caching Danh mục Phân tầng trên Redis | Là Lập trình viên, tôi muốn cache dữ liệu Master Data trên Redis để tránh nghẽn Connection Pool khi hàng ngàn người mở form cùng lúc. | 8 | Must | Sprint 02 |
| **US-26** | Chống Giả mạo Dữ liệu Phụ thuộc (Anti-Tampering) | Là Kỹ sư Bảo mật, tôi muốn Server-side FluentValidation kiểm tra dữ liệu con thực sự trực thuộc dữ liệu cha, chặn request sửa qua DevTools. | 8 | Must | Sprint 02 |
| **US-27** | Làm sạch JSON Schema Chống XSS | Là Kỹ sư Bảo mật, tôi muốn loại bỏ các đoạn mã độc JavaScript có thể chèn vào thuộc tính `customDefaultValue` của Form.io. | 5 | Must | Sprint 02 |

---

### EPIC-05: Động cơ Quy trình Elsa v3 & Quản trị Trình ký (WorkflowEngine)
| Mã US | Tên User Story | Mô tả Nghiệp vụ | SP | MoSCoW | Sprint |
|:---:|---|---|:---:|:---:|:---:|
| **US-28** | Nhúng Visual Designer Elsa Studio | Là Chuyên viên Quy trình, tôi muốn vẽ luồng quy trình duyệt (BPMN Activity Designer) trực tiếp trên giao diện Admin Portal. | 13 | Must | Sprint 03 |
| **US-29** | Custom Activity Phê duyệt Ký số (ApproveFormActivity) | Là Lập trình viên, tôi muốn có Custom Activity dừng luồng quy trình (CreateBookmark), phân công người duyệt và chờ tín hiệu Resume. | 13 | Must | Sprint 03 |
| **US-30** | Khởi tạo & Nộp Phiếu Trình ký | Là Nhân viên, tôi muốn điền dữ liệu trên Form.io, đính kèm tài liệu và bấm Gửi duyệt để kích hoạt luồng quy trình. | 8 | Must | Sprint 03 |
| **US-31** | Hàng đợi Tác vụ Phê duyệt Cá nhân (Pending Tasks) | Là Người duyệt, tôi muốn danh sách công việc cần duyệt hiển thị rõ mức độ ưu tiên, hạn SLA và xem nhanh nội dung phiếu. | 8 | Must | Sprint 03 |
| **US-32** | Phê duyệt, Từ chối, Yêu cầu Bổ sung Phiếu | Là Người duyệt, tôi muốn bấm nút Đồng ý, Từ chối hoặc Trả lại yêu cầu sửa đổi kèm ý kiến phê duyệt để luồng rẽ nhánh tương ứng. | 8 | Must | Sprint 03 |
| **US-33** | Lịch sử Phê duyệt Bất biến (ApprovalHistory) | Là Kiểm toán viên, tôi muốn xem toàn bộ hành trình phê duyệt từng bước kèm mã băm SHA-256 bảo đảm không bị can thiệp sửa đổi. | 8 | Must | Sprint 03 |
| **US-34** | Khóa Phân tán Chống Duyệt Trùng lặp (Distributed Lock) | Là Lập trình viên, tôi muốn dùng Redis Lock để chặn việc người dùng click đúp hoặc 2 request đồng thời gây nhảy cóc trạng thái quy trình. | 5 | Must | Sprint 03 |
| **US-35** | Quản trị SLA & Cảnh báo Quá hạn (Escalation Worker) | Là Ban Giám đốc, tôi muốn có tiến trình ngầm tự động quét và phát hiện các task quá hạn để gửi cảnh báo khẩn cấp và leo thang cấp trên. | 13 | Must | Sprint 05 |
| **US-36** | Điều chuyển Tác vụ khi Nhân sự Nghỉ việc | Là Quản trị viên, tôi muốn khi nhân sự thôi việc, toàn bộ tác vụ Pending của họ tự động được chuyển sang Quản lý trực tiếp. | 8 | Should | Sprint 05 |
| **US-37** | Xác thực Ký điện tử Webhook Ngoài (HMAC-SHA256) | Là Kỹ sư Tích hợp, tôi muốn các webhook kích hoạt từ ERP/SAP phải được ký HMAC-SHA256 để chống tấn công giả mạo. | 5 | Should | Sprint 03 |

---

### EPIC-06: Quản lý Tài liệu OneDrive & Sinh Văn bản Template (DocumentManagement)
| Mã US | Tên User Story | Mô tả Nghiệp vụ | SP | MoSCoW | Sprint |
|:---:|---|---|:---:|:---:|:---:|
| **US-38** | Upload File Đính kèm lên OneDrive for Business | Là Người nộp đơn, tôi muốn upload file đính kèm (hợp đồng, báo giá) đẩy thẳng lên OneDrive qua MS Graph API mà không phình DB. | 13 | Must | Sprint 03 |
| **US-39** | Xem Trước Tài liệu Bảo mật (Signed Preview URL) | Là Người duyệt, tôi muốn bấm vào xem tài liệu đính kèm trực tiếp qua link ký số có thời hạn (Time-limited Signed URL) an toàn. | 8 | Must | Sprint 03 |
| **US-40** | Kiểm soát Định dạng File & Quét Dung lượng | Là Kỹ sư An toàn thông tin, tôi muốn hệ thống chỉ cho phép tải file tài liệu hợp lệ (.pdf, .docx, .xlsx, ảnh $\le 25\text{MB}$) và chặn file thực thi. | 5 | Must | Sprint 03 |
| **US-41** | Quản lý Mẫu Tài liệu Động (Document Templates) | Là Quản trị viên, tôi muốn tải lên file mẫu Word (.docx) chứa các placeholder (`{{TenNguoiKy}}`, `{{SoTien}}`) để làm template sinh văn bản. | 8 | Should | Sprint 05 |
| **US-42** | Tự động Sinh File Quyết định/Hợp đồng PDF | Là Chuyên viên Pháp chế, tôi muốn khi hồ sơ được duyệt hoàn tất, hệ thống tự động điền dữ liệu vào template và sinh file PDF ký duyệt lưu vào OneDrive. | 13 | Should | Sprint 05 |
| **US-43** | Xử lý Tài liệu khi Hủy Phiếu Trình ký | Là Lập trình viên, tôi muốn khi phiếu bị hủy thì các file đính kèm được đánh dấu xóa mềm để tránh lãng phí dung lượng. | 3 | Could | Sprint 05 |

---

### EPIC-07: Cổng Thông báo Tập trung Đa kênh (Notification)
| Mã US | Tên User Story | Mô tả Nghiệp vụ | SP | MoSCoW | Sprint |
|:---:|---|---|:---:|:---:|:---:|
| **US-44** | Quản lý Mẫu Thông báo Đa ngôn ngữ (Templates) | Là Quản trị viên, tôi muốn cấu hình nội dung mẫu email/tin nhắn bằng cú pháp Fluid Template (`{{ FormKey }}`, `{{ RequesterName }}`). | 8 | Must | Sprint 04 |
| **US-45** | Gửi Email Thông báo qua Microsoft Graph Mail API | Là Người duyệt, tôi muốn nhận email từ hòm thư doanh nghiệp khi có phiếu mới được giao với nút bấm mở thẳng vào phiếu duyệt. | 8 | Must | Sprint 04 |
| **US-46** | Bắn Tin Thông báo qua Microsoft Teams Webhook | Là Lãnh đạo Khối, tôi muốn nhận thông báo tức thời trên kênh Microsoft Teams của Ban Lãnh đạo khi có phiếu khẩn cấp. | 5 | Should | Sprint 04 |
| **US-47** | Hàng đợi Gửi tin & Thử lại Lũy tiến (Exponential Backoff) | Là Lập trình viên, tôi muốn hàng đợi gửi tin tự động thử lại 3 lần nếu dịch vụ mail bị bảo trì, tránh làm rớt thông báo người dùng. | 8 | Must | Sprint 04 |
| **US-48** | Khử Trùng lặp Thông báo (Deduplication) | Là Người dùng, tôi muốn không bị nhận 2 email trùng nhau cho cùng một sự kiện do lỗi mạng retry gây ra. | 5 | Must | Sprint 04 |
| **US-49** | Thông báo Đa luồng: Từ chối, Hủy, Quá hạn SLA | Là Người nộp đơn, tôi muốn nhận email thông báo chi tiết lý do khi phiếu bị từ chối hoặc bị hủy. | 8 | Must | Sprint 04 |

---

### EPIC-08: Bản tin Truyền thông Nội bộ & Cấu hình Hệ thống (InternalNews & SystemSettings)
| Mã US | Tên User Story | Mô tả Nghiệp vụ | SP | MoSCoW | Sprint |
|:---:|---|---|:---:|:---:|:---:|
| **US-50** | Đăng Bài viết Bản tin & Thông cáo Doanh nghiệp | Là Ban Truyền thông, tôi muốn soạn thảo bài viết với trình soạn thảo Rich Text (hình ảnh, định dạng) và xuất bản lên bảng tin nội bộ. | 8 | Must | Sprint 04 |
| **US-51** | Hẹn giờ Xuất bản & Tự động Lưu trữ Bản tin | Là Ban Truyền thông, tôi muốn hẹn giờ xuất bản bài viết vào đúng thời điểm và tự động lưu trữ (Archive) khi hết hạn hiển thị. | 8 | Should | Sprint 04 |
| **US-52** | Xác nhận Bắt buộc Đã đọc Tin (Read Receipts) | Là Giám đốc Nhân sự, tôi muốn với các thông cáo quan trọng bắt buộc toàn bộ nhân viên phải bấm nút Xác nhận đã đọc để đo mức độ tuân thủ. | 8 | Should | Sprint 04 |
| **US-53** | Quản lý Tham số Hệ thống & White-labeling | Là Quản trị viên, tôi muốn tùy chỉnh Logo, Favicon, Tên ứng dụng, Gam màu chủ đạo để phù hợp với bộ nhận diện tập đoàn. | 8 | Must | Sprint 04 |
| **US-54** | Mã hóa Dữ liệu Nhạy cảm & Lưu Vết Đổi Cấu hình | Là Kỹ sư Bảo mật, tôi muốn các API Keys được mã hóa At-Rest và mọi thay đổi cấu hình phải lưu lịch sử người sửa và lý do. | 6 | Must | Sprint 04 |

---

### EPIC-09: Trợ lý AI Doanh nghiệp Đa Nền tảng (Gemini, ChatGPT, Claude, Local MCP) & Tra cứu RAG (AiAssistant)
| Mã US | Tên User Story | Mô tả Nghiệp vụ | SP | MoSCoW | Sprint |
|:---:|---|---|:---:|:---:|:---:|
| **US-55** | Multi-Provider AI Gateway & Local MCP Server | Là Kỹ sư AI, tôi muốn hệ thống hỗ trợ cắm-và-chạy linh hoạt giữa Google Gemini, OpenAI, Claude và Local LLM (Ollama/vLLM) qua chuẩn MCP Server mà không sửa đổi mã nguồn. | 14 | Must | Sprint 06 |
| **US-56** | Indexing Quy chế Chính sách vào Qdrant Vector DB | Là Chuyên viên Pháp chế, tôi muốn tải các file quy chế, chính sách nội bộ lên để hệ thống tự động bẻ chunk và đánh index vector vào Qdrant (hỗ trợ cả Local & Cloud Embeddings). | 12 | Must | Sprint 06 |
| **US-57** | Unified RAG C# Tool Tra cứu Quy chế Nội bộ | Là Nhân viên, tôi muốn chat hỏi quy định công tác phí và AI (Gemini/ChatGPT/Claude/Local) gọi Tool trích dẫn đúng trang tài liệu quy định kèm Citation Pills. | 10 | Must | Sprint 06 |
| **US-58** | Unified C# Tool Tra cứu Tác vụ & Số liệu Scoped RBAC/ABAC | Là Lãnh đạo/Nhân viên, tôi muốn tra cứu task và tiến độ quy trình bằng ngôn ngữ tự nhiên với cơ chế tự động tiêm context người dùng bảo mật phân quyền. | 10 | Must | Sprint 06 |
| **US-59** | Widget Chat AI Nổi (Floating Assistant Widget) & SSE Streaming | Là Người dùng, tôi muốn có widget chat trợ lý AI nổi ở góc phải màn hình portal hỗ trợ Streaming SSE, Markdown và chọn Provider/Model. | 8 | Should | Sprint 06 |

---

### EPIC-10: Báo cáo Toàn hệ thống, KPI & Quản trị SLA (AnalyticsReporting)
| Mã US | Tên User Story | Mô tả Nghiệp vụ | SP | MoSCoW | Sprint |
|:---:|---|---|:---:|:---:|:---:|
| **US-60** | Bảng Tổng hợp Chỉ số Materialized Metrics | Là Lập trình viên, tôi muốn có tiến trình ngầm gom chỉ số trình ký hàng giờ vào bảng tổng hợp để API Dashboard phản hồi $< 50\text{ ms}$. | 8 | Must | Sprint 07 |
| **US-61** | Thuật toán Tính SLA theo Giờ Làm việc Thực tế | Là Ban Giám đốc, tôi muốn thời gian xử lý phiếu được tính chính xác theo giờ hành chính (loại trừ ngoài giờ, cuối tuần, ngày lễ). | 13 | Must | Sprint 07 |
| **US-62** | Dashboard Báo cáo Hiệu suất SLA & Cảnh báo Trễ hạn | Là Lãnh đạo Khối, tôi muốn biểu đồ trực quan về tỷ lệ tuân thủ SLA, số đơn trễ hạn theo phòng ban và theo loại biểu mẫu. | 8 | Must | Sprint 07 |
| **US-63** | Phân tích Nút thắt Cổ chai Quy trình (Bottlenecks) | Là Ban Giám đốc, tôi muốn hệ thống chỉ rõ bước nào trong quy trình đang làm chậm tiến độ chung và tỷ lệ duyệt ngay (First-Time-Right). | 8 | Should | Sprint 07 |
| **US-64** | Thước đo Năng suất Nhân sự (Scorecard Cá nhân) | Là Trưởng phòng, tôi muốn xem bảng đo năng suất của nhân viên: số task xử lý, số task tồn đọng, tỷ lệ đúng hạn để đánh giá thi đua. | 8 | Should | Sprint 07 |
| **US-65** | Báo cáo Dòng tiền & Ngân sách Trình ký | Là Giám đốc Tài chính, tôi muốn báo cáo tổng hợp giá trị các khoản chi đã duyệt theo Dự án và Trung tâm Chi phí. | 8 | Should | Sprint 07 |
| **US-66** | Công cụ Tạo Báo cáo Động (Dynamic Report Builder) | Là Người quản trị, tôi muốn tự kéo thả trường dữ liệu, đặt điều kiện lọc, gom nhóm để sinh báo cáo tùy biến theo nhu cầu. | 13 | Should | Sprint 07 |
| **US-67** | Tự động Xuất & Gửi Báo cáo theo Lịch Định kỳ | Là Tổng Giám đốc, tôi muốn 08:00 sáng Thứ Hai hàng tuần nhận được email đính kèm file Excel báo cáo tổng hợp vận hành toàn tập đoàn. | 8 | Should | Sprint 07 |

---

### EPIC-11: Tối ưu Hiệu năng Cao, Độ Ổn định & Go-Live (Cross-Cutting)
| Mã US | Tên User Story | Mô tả Nghiệp vụ | SP | MoSCoW | Sprint |
|:---:|---|---|:---:|:---:|:---:|
| **US-68** | Caching 2 Tầng L1 Memory + L2 Redis | Là Kỹ sư Hiệu năng, tôi muốn cấu hình cache 2 tầng cho Master Data và SystemSettings để đạt P99 Latency $< 30\text{ ms}$. | 8 | Must | Sprint 08 |
| **US-69** | CQRS Đọc Tải Cao bằng Dapper | Là Kỹ sư Backend, tôi muốn chuyển đổi các truy vấn danh sách phân trang sang Dapper để giảm $85\%$ chi phí bộ nhớ GC Allocation. | 8 | Must | Sprint 08 |
| **US-70** | Đánh Chỉ mục SQL Server Covering & Filtered Index | Là DBA, tôi muốn cấu hình đầy đủ Covering Indexes và Filtered Indexes để loại bỏ Key Lookups trên SQL Server. | 8 | Must | Sprint 08 |
| **US-71** | Transactional Outbox Pattern cho Integration Events | Là Kiến trúc sư, tôi muốn mọi sự kiện liên module quan trọng được lưu qua bảng Outbox để đảm bảo không bao giờ bị rơi rớt khi crash app. | 13 | Must | Sprint 08 |
| **US-72** | Polly v8 Resilience Pipeline cho Graph API & Mail | Là Kỹ sư Vận hành, tôi muốn tích hợp Circuit Breaker và Retry Jitter cho toàn bộ kết nối dịch vụ ngoài để hệ thống không bị treo luồng. | 8 | Must | Sprint 08 |
| **US-73** | Health Checks & Triển khai Cắt chuyển Go-Live | Là Đội ngũ DevOps, tôi muốn endpoint `/healthz/ready` giám sát toàn bộ hạ tầng và quy trình chạy script migration cắt chuyển Go-Live thành công. | 10 | Must | Sprint 08 |

