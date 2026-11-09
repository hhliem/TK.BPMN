# Quy tắc Bắt buộc: Quản trị Báo cáo Sprint (Sprint Reporting Governance Rule)

Tất cả các AI Coding Agents khi làm việc trên dự án **Enterprise BPM Platform** PHẢI TUÂN THỦ NGHIÊM NGẶT quy tắc này:

## 1. Vị trí Lưu trữ Báo cáo
- Mọi báo cáo nghiệm thu hoàn tất từng Sprint **BẮT BUỘC** phải được ghi vào thư mục:
  `d:\My Project\TK.BPMN\reports/`
- Đặt tên file báo cáo theo quy ước chuẩn:
  `reports/sprint-{sprint_number}-completion-report.md` (ví dụ: `sprint-01-completion-report.md`, `sprint-02-completion-report.md`, ...)
- Tuyệt đối không lưu file hoàn thành sprint rải rác trong `plan/` hoặc thư mục gốc.

## 2. Cấu trúc Nội dung Báo cáo Bắt buộc
Mỗi file báo cáo hoàn tất Sprint phải có đầy đủ 3 phần cốt lõi:

### Phần 1: Báo cáo Tiến độ (Sprint Progress & Delivered Capabilities)
- Tổng số Story Points cam kết vs hoàn thành thực tế (Velocity).
- Danh sách các User Stories / Tasks kỹ thuật đã hoàn thành 100% kèm đường dẫn liên kết code (`file:///...`).
- Chi tiết các phân hệ / bảng dữ liệu / APIs / DTOs / Services đã xây dựng theo Clean Architecture 4 tầng.

### Phần 2: Kết quả Kiểm thử & Biên dịch (Test Results & Build Verification)
- Trạng thái biên dịch của toàn bộ Solution và từng dự án (Build status, Errors, Warnings).
- Kết quả kiểm thử kiến trúc (`NetArchTest`) bảo đảm tính độc lập của các phân tầng và Bounded Context của Modular Monolith.
- Kết quả kiểm thử đơn vị (`Unit Tests`), kiểm thử tích hợp (`Integration Tests`).
- Tiêu chí nghiệm thu (Acceptance Criteria / Gherkin) đã được đáp ứng ra sao.

### Phần 3: Review Tổng thể Dự án (Overall Project Health & Review)
- Đánh giá chất lượng mã nguồn và sự tuân thủ các quy tắc thiết kế (SOLID, DDD, Clean Architecture, CQRS).
- Đánh giá rủi ro kỹ thuật còn tồn đọng (Technical Debt, Performance Bottlenecks, Security Governance).
- Đánh giá tính sẵn sàng chuyển giao (Definition of Done) và điều kiện tiên quyết cho Sprint tiếp theo.

