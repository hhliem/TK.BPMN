# Quy tắc Bắt buộc: Quản trị Cấu hình & Nguyên lý Thất bại Sớm (Configuration & Fail-Fast Governance)

Tất cả các AI Coding Agents khi phát triển hệ thống **Enterprise BPM Platform** PHẢI TUÂN THỦ TUYỆT ĐỐI các quy tắc sau:

## 1. Không Hardcode Chuỗi Kết nối & Không Fallback Cục bộ (Zero Hardcoded Connection Strings)
- **Tuyệt đối CẤM:** Không được hardcode bất kỳ chuỗi kết nối cục bộ nào trong mã nguồn (ví dụ: `Server=(localdb)\\mssqllocaldb;Database=...`, `localhost:6379`, v.v.).
- **Yêu cầu Bắt buộc:** 
  - Mọi cấu hình kết nối Database, Redis, Message Queue, SMTP, API Keys... **BẮT BUỘC** phải được đọc trực tiếp từ `IConfiguration` (appsettings.json / appsettings.{Environment}.json / Biến môi trường hệ thống).
  - Sử dụng cú pháp Fail-Fast nghiêm ngặt:
    ```csharp
    var connectionString = configuration.GetConnectionString("DefaultConnection") 
        ?? throw new InvalidOperationException("Chuỗi kết nối cơ sở dữ liệu 'DefaultConnection' chưa được cấu hình trong hệ thống.");
    ```
  - Nếu thiếu cấu hình, hệ thống phải **ném lỗi ngay lập tức (`InvalidOperationException`)** tại thời điểm khởi động (Fail Fast), không được tự ý fallback ngầm về LocalDB hay chuỗi kết nối giả lập.

## 2. Loại bỏ Hoàn toàn Cơ chế Nuốt lỗi (No Try-Fail / Silent Failure)
- **Tuyệt đối CẤM:** Không được bọc khối `try-catch` chỉ để ghi `logger.LogWarning` rồi tiếp tục chạy ứng dụng trong trạng thái lỗi (Silent Failure / Try-Fail).
- **Yêu cầu Bắt buộc:** 
  - Các thao tác khởi tạo hạ tầng cốt lõi (Migration, Seeding, Connection test) nếu thất bại phải báo lỗi rõ ràng hoặc ném Exception chặn tiến trình nếu hạ tầng đó là điều kiện tiên quyết của hệ thống.
  - Không được "nuốt" lỗi cấu hình hoặc giả vờ hoàn thành khi chưa có dữ liệu thực tế từ hệ thống.

## 3. Không Giả lập Danh tính / Dữ liệu Người dùng (No Fake User Context Fallback)
- **Yêu cầu Bắt buộc:**
  - `ICurrentUserService` chỉ được trích xuất dữ liệu danh tính từ Token/Claims thực tế hoặc Headers hợp lệ do API Gateway chuyển giao.
  - Nếu request là Anonymous hoặc chưa xác thực, các thuộc tính `UserId`, `UserName`, `UserPrincipalName`, `OrgUnitId` **phải trả về `null`**, tuyệt đối không được tự ý fallback về email giả lập (ví dụ: `?? "admin@enterprise.bpm"` hay `?? "Anonymous"`).

