---
name: bpmn-high-performance-tuning
description: Hướng dẫn chuyên sâu tối ưu hóa hiệu năng cao (High Performance) cho nền tảng Enterprise BPMN, Redis Caching 2 tầng, CQRS Dapper Reads, Tối ưu SQL Server Indexing & Connection Pooling.
---

# Skill: Enterprise High-Performance Tuning (.NET Core & SQL Server)

## 1. Nguyên tắc Trọng tâm (Performance Engineering Pillars)

Hệ thống Enterprise BPMN phục vụ hàng ngàn người dùng đồng thời, hàng chục triệu bản ghi lịch sử và hàng trăm lượt submit đơn/phút cần đáp ứng các chỉ tiêu hiệu năng chuẩn:
- **P99 Read Latency:** $< 30\text{ ms}$ cho Master Data / Cascading API, $< 50\text{ ms}$ cho Dashboard BI.
- **P99 Write Latency:** $< 100\text{ ms}$ cho Submission & Approval Task Resume.
- **Throughput Target:** Khả năng phục vụ tối thiểu $1,000\text{ RPS}$ (Requests Per Second) trên một máy chủ API tiêu chuẩn.

### 4 Trụ cột Kỹ thuật Bắt buộc
1. **Multi-Level Caching (L1 + L2):**
   - **L1 Cache (In-Memory `IMemoryCache`):** Lưu trữ các tham số SystemSettings, Form Schema tĩnh (TTL: 5-15 phút) truy cập gần như 0ms latency.
   - **L2 Cache (Distributed `StackExchange.Redis`):** Lưu Master Data theo khóa `masterdata:{category}:{parentId}`, tự động xóa cache (Invalidation) theo tiền tố khóa khi có thay đổi dữ liệu.
2. **CQRS Tách biệt Đường Đọc & Ghi (Read-Write Separation):**
   - **Write Path (Command):** Sử dụng Entity Framework Core với Change Tracking tối ưu, bọc trong Transaction ngắn gọn.
   - **Read Path (Query):** 100% truy vấn đọc danh sách, phân trang, báo cáo dùng `.AsNoTracking()` hoặc thư viện **Dapper** để loại bỏ hoàn toàn chi phí Change Tracker và Entity Materialization overhead.
3. **Chiến lược Đánh Chỉ mục SQL Server (Strategic Indexing):**
   - Phải có **Covering Index** (`INCLUDE` columns) cho các truy vấn lọc thường xuyên để đạt được *Index Seek* và tránh *Key Lookup*.
   - Áp dụng **Filtered Index** cho các bảng có trạng thái nhị phân (ví dụ: `WHERE [IsLatest] = 1` trên FormDefinitions hoặc `WHERE [Status] = 'Pending'` trên ApprovalTasks).
4. **Database Connection & Resource Pooling:**
   - Dùng `AddDbContextPool<TDbContext>` thay vì `AddDbContext<TDbContext>` để tái sử dụng instance DbContext, giảm GC Allocations.
   - Luôn truyền `CancellationToken` vào tất cả các phương thức bất đồng bộ (`ToListAsync`, `FirstOrDefaultAsync`, `ExecuteAsync`) để hủy truy vấn ngay khi client ngắt kết nối, giải phóng Connection Pool cho SQL Server.

---

## 2. Quy trình Thực thi cho AI Agent khi Viết Code

1. **Khi viết Query API đọc dữ liệu:**
   - Kiểm tra xem API này có tần suất gọi cao không (Master Data, Dropdown, Menu, Dashboard).
   - Nếu có, áp dụng Cache-Aside Pattern với Redis hoặc MemoryCache.
   - Tuyệt đối không dùng `_context.Entity.ToList()` không có `AsNoTracking()` và không có phân trang `Skip().Take()`.
2. **Khi viết Command xử lý nghiệp vụ:**
   - Gom các câu lệnh ghi trong một lần `SaveChangesAsync()` duy nhất.
   - Với các tác vụ Insert/Update hàng loạt (> 50 bản ghi), sử dụng `EFCore.BulkExtensions` hoặc SQL Batching thay vì vòng lặp `_context.Add()`.
3. **Khi thiết kế Entity & Migration:**
   - Mọi Foreign Key (`OrgUnitId`, `PositionId`, `CategoryId`, `WorkflowInstanceId`) bắt buộc phải có Non-Clustered Index.
   - Thêm cột `INCLUDE` cho các trường hiển thị phổ biến (`Code`, `Name`, `Status`, `CreatedAt`).

