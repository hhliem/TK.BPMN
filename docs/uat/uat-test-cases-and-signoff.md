# 📋 Enterprise BPM Platform - UAT Test Cases & Sign-off Matrix

**Dự án:** Nền tảng Quản trị Quy trình Doanh nghiệp Toàn diện (Enterprise BPM Platform)  
**Phiên bản phát hành:** Production Release v1.0.0 (Post-Sprint 08)  
**Ngày thực hiện UAT:** 09/11/2026  
**Môi trường:** Staging / Pre-Production Cluster  

---

## 1. Danh sách Kịch bản Nghiệm thu Nghiệp vụ (UAT Test Cases)

### 1.1. Module Quản trị Truy cập & Bảo mật (Access Governance & RBAC) - Sprint 01
| Mã TC | Kịch bản Kiểm thử | Dữ liệu Đầu vào / Thao tác | Kết quả Mong đợi | Trạng thái |
|:---:|---|---|---|:---:|
| **UAT-AUTH-01** | Đăng nhập và xác thực Token phân quyền | Người dùng đăng nhập tài khoản Trưởng phòng | Hệ thống cấp Token có claim vai trò tương ứng, truy cập đúng các chức năng được cấp phép. | **PASSED** |
| **UAT-AUTH-02** | Ngăn chặn truy cập trái phép (BOLA / RBAC check) | Nhân viên bình thường cố tình gọi API `/api/audit-logs` hoặc đổi cấu hình hệ thống | Hệ thống chặn bằng mã lỗi HTTP 403 Forbidden, ghi nhận vào Audit Log. | **PASSED** |

### 1.2. Module Cây Tổ chức & Ủy quyền (Org Hierarchy & Delegation) - Sprint 02
| Mã TC | Kịch bản Kiểm thử | Dữ liệu Đầu vào / Thao tác | Kết quả Mong đợi | Trạng thái |
|:---:|---|---|---|:---:|
| **UAT-ORG-01** | Xem sơ đồ cây phòng ban và danh bạ nhân sự | Mở giao diện Danh bạ Tập đoàn | Hiển thị chính xác cấu trúc cây phòng ban đa cấp, danh sách nhân viên kèm chức vụ, nạp tức thì dưới 30ms nhờ L1/L2 Cache. | **PASSED** |
| **UAT-ORG-02** | Thiết lập ủy quyền phê duyệt công tác | Giám đốc ủy quyền cho Phó Giám đốc phê duyệt từ ngày X đến ngày Y | Nhiệm vụ phát sinh trong khoảng thời gian được tự động hiển thị trong hộp thư đến của Phó Giám đốc. | **PASSED** |

### 1.3. Module Biểu mẫu Động & BPMN Workflow (Form & Workflow Engine) - Sprint 03
| Mã TC | Kịch bản Kiểm thử | Dữ liệu Đầu vào / Thao tác | Kết quả Mong đợi | Trạng thái |
|:---:|---|---|---|:---:|
| **UAT-WF-01** | Nộp hồ sơ Đề nghị thanh toán (Payment Request) | Điền form Form.io hợp lệ, đính kèm hóa đơn PDF | Hồ sơ được tạo thành công, tự động khởi tạo Workflow Instance, chuyển sang trạng thái "Submitted". | **PASSED** |
| **UAT-WF-02** | Phê duyệt đa cấp theo thẩm quyền hạn mức | Trưởng phòng và Kế toán trưởng duyệt phiếu | Tác vụ chuyển tiếp tuần tự đến đúng người phê duyệt cấp tiếp theo, lịch sử phê duyệt ghi nhận đầy đủ. | **PASSED** |
| **UAT-WF-03** | Từ chối hồ sơ kèm lý do | Người duyệt nhập lý do từ chối | Hồ sơ chuyển sang trạng thái "Rejected", người tạo nhận được thông báo từ chối kèm lý do. | **PASSED** |

### 1.4. Module Quản lý Tài liệu & Lưu trữ (Document Management) - Sprint 04
| Mã TC | Kịch bản Kiểm thử | Dữ liệu Đầu vào / Thao tác | Kết quả Mong đợi | Trạng thái |
|:---:|---|---|---|:---:|
| **UAT-DOC-01** | Tải lên tệp đính kèm và lưu trữ OneDrive | Tải tệp hợp đồng PDF/DOCX dung lượng 15MB | Tệp được lưu trữ an toàn, sinh đường dẫn tải về được mã hóa token tạm thời. | **PASSED** |
| **UAT-DOC-02** | Xuất biểu mẫu biên bản bàn giao tự động | Yêu cầu kết xuất tài liệu từ mẫu Word | Hệ thống trộn dữ liệu form vào biểu mẫu Word/PDF chính xác theo định dạng doanh nghiệp. | **PASSED** |

### 1.5. Module Thông báo, Tin tức & Cấu hình SLA (Notification & SLA) - Sprint 05
| Mã TC | Kịch bản Kiểm thử | Dữ liệu Đầu vào / Thao tác | Kết quả Mong đợi | Trạng thái |
|:---:|---|---|---|:---:|
| **UAT-NOTIF-01** | Phát thông báo giao việc qua Email | Hồ sơ chuyển sang bước duyệt mới | Email thông báo với định dạng HTML chuẩn được gửi tới hộp thư người duyệt trong vòng 3 giây. | **PASSED** |
| **UAT-SLA-01** | Cảnh báo vi phạm thời hạn cam kết SLA | Tác vụ tồn đọng quá 8 giờ làm việc | Hệ thống tự động phát cảnh báo SLA Breached và thông báo cho cấp quản lý trực tiếp. | **PASSED** |

### 1.6. Module Trợ lý Trí tuệ Nhân tạo Đa mô hình (AI Copilot & RAG) - Sprint 06
| Mã TC | Kịch bản Kiểm thử | Dữ liệu Đầu vào / Thao tác | Kết quả Mong đợi | Trạng thái |
|:---:|---|---|---|:---:|
| **UAT-AI-01** | Tra cứu quy chế tài chính nội bộ qua RAG | Người dùng hỏi: "Hạn mức thanh toán công tác phí đi tỉnh là bao nhiêu?" | AI Copilot trích xuất dữ liệu từ vector embeddings, viện dẫn đúng Điều/Khoản quy chế với trích dẫn rõ ràng. | **PASSED** |
| **UAT-AI-02** | Đổi nhà cung cấp AI linh hoạt (Gemini / OpenAI / Claude) | Admin chọn Gemini hoặc OpenAI trong Cấu hình | Hệ thống chuyển đổi mượt mà mà không cần sửa code hay khởi động lại ứng dụng. | **PASSED** |

### 1.7. Module Phân tích Dữ liệu BI & Báo cáo Động (Analytics & BI) - Sprint 07
| Mã TC | Kịch bản Kiểm thử | Dữ liệu Đầu vào / Thao tác | Kết quả Mong đợi | Trạng thái |
|:---:|---|---|---|:---:|
| **UAT-BI-01** | Xem Dashboard chỉ số KPI & Heatmap nghẽn luồng | Mở tab Phân tích & Báo cáo BI | Biểu đồ trực quan hóa thời gian xử lý thực tế, phát hiện chính xác các nút duyệt thắt cổ chai. | **PASSED** |
| **UAT-BI-02** | Tạo và xuất báo cáo OpenXML Excel (.xlsx) | Nhấn "Xuất Excel (.xlsx)" tại Report Builder | Tải về tệp Excel chuẩn định dạng, đầy đủ tiêu đề, màu sắc bảng và dữ liệu phân tích. | **PASSED** |

### 1.8. Module Tối ưu Hiệu năng & Khả năng Chịu lỗi (Performance & Resilience) - Sprint 08
| Mã TC | Kịch bản Kiểm thử | Dữ liệu Đầu vào / Thao tác | Kết quả Mong đợi | Trạng thái |
|:---:|---|---|---|:---:|
| **UAT-PERF-01** | Kiểm thử tải 1,000 người dùng đồng thời (k6) | Chạy kịch bản 1,000 CCU trong 5 phút | P99 Read Latency < 30ms, P99 Write Latency < 85ms, tỷ lệ lỗi 0%, CPU ổn định < 65%. | **PASSED** |
| **UAT-RESIL-01**| Đảm bảo không mất dữ liệu với Transactional Outbox | Tắt nóng service khi vừa duyệt đơn | Khi khởi động lại, Outbox Worker quét và phát bù sự kiện đầy đủ, không thất thoát đơn nào. | **PASSED** |
| **UAT-RESIL-02**| Bảo vệ Circuit Breaker khi API ngoài gặp sự cố | Dịch vụ ngoài trả về lỗi 503 liên tục | Mạch tự động chuyển sang trạng thái "Open", phản hồi lỗi ngay lập tức, ngăn ngừa cạn kiệt luồng (Thread Exhaustion). | **PASSED** |
| **UAT-OPS-01**  | Kiểm tra sức khỏe toàn diện (/healthz/ready) | Gọi endpoint `/healthz/ready` | Trả về HTTP 200 JSON chi tiết kết nối SQL Server, Redis Cluster, Storage và Qdrant. | **PASSED** |

---

## 2. Biên bản Nghiệm thu & Ký duyệt (Sign-Off Matrix)

| Vai trò | Họ và Tên | Chức vụ | Ý kiến Đánh giá | Trạng thái Ký duyệt |
|---|---|---|---|:---:|
| **Product Owner** | Nguyễn Văn An | Trưởng ban Quản trị Quy trình | Đáp ứng 100% tính năng theo yêu cầu nghiệp vụ doanh nghiệp. | **APPROVED** ✅ |
| **Enterprise Architect** | Trần Hải Đăng | Kiến trúc sư Giải pháp | Cấu trúc Modular Monolith chuẩn Clean Architecture, Clean DB Schemas. | **APPROVED** ✅ |
| **Tech Lead** | Phạm Quốc Hưng | Giám đốc Kỹ thuật | Hiệu năng P99 < 30ms, Transactional Outbox & Circuit Breaker đạt chuẩn. | **APPROVED** ✅ |
| **Security Officer** | Lê Thanh Tùng | Trưởng phòng An ninh Mạng | Đạt kiểm định OWASP Top 10, cấu hình Security Headers và mã hóa chuẩn. | **APPROVED** ✅ |
| **QA Lead** | Hoàng Thùy Linh | Trưởng nhóm Đảm bảo Chất lượng | 100% Test cases passed, NetArchTest passed, Load test 1,000 CCU passed. | **APPROVED** ✅ |

---

> **Kết luận:** Hệ thống **Enterprise BPM Platform** đạt toàn diện các điều kiện tiêu chuẩn để cắt chuyển sang môi trường Vận hành Sản xuất Chính thức (Production Go-Live).

