# Hướng dẫn Đánh Chỉ mục SQL Server Đạt Hiệu năng Tối đa

## 1. Nguyên tắc Chỉ mục Bao phủ (Covering Index)

Khi một câu lệnh `SELECT` truy vấn các cột không nằm trong Index, SQL Server phải thực hiện thao tác **Key Lookup** (quét ngược về bảng Clustered Index) để lấy dữ liệu còn thiếu. Nếu số lượng dòng lớn, Key Lookup sẽ làm bùng nổ thao tác I/O đọc ổ cứng.

### Kỹ thuật Giải quyết: Dùng mệnh đề `INCLUDE`
```sql
-- Ví dụ: Tối ưu tra cứu danh mục con Cascading
CREATE INDEX [IX_MasterItems_CascadingQuery] 
ON [form].[MasterItems]([CategoryId], [ParentId], [IsActive]) 
INCLUDE ([Code], [Name], [DisplayOrder], [CustomAttributesJson]);
```
- **Cột trong ON (...):** Dùng để tìm kiếm nhị phân (Index Seek) và sắp xếp.
- **Cột trong INCLUDE (...):** Lưu kèm ở lá cây B-Tree, không tốn chi phí cân bằng cây chỉ mục, cung cấp đủ dữ liệu cho câu SELECT mà không cần Key Lookup.

---

## 2. Chỉ mục Lọc (Filtered Index)

Chỉ mục lọc cho phép đánh index chỉ trên một tập con dữ liệu thỏa mãn điều kiện `WHERE`. Giúp tiết kiệm $80-90\%$ dung lượng lưu trữ index và tăng tốc độ tìm kiếm gấp nhiều lần.

```sql
-- 1. Index chỉ lưu phiên bản mới nhất của Form (thay vì toàn bộ lịch sử)
CREATE UNIQUE INDEX [IX_FormDefinitions_Latest] 
ON [form].[FormDefinitions]([FormKey]) 
WHERE [IsLatest] = 1;

-- 2. Index chỉ lưu các task đang chờ duyệt (bỏ qua hàng triệu task đã hoàn thành)
CREATE INDEX [IX_ApprovalTasks_PendingOnly] 
ON [workflow].[ApprovalTasks]([AssignedToUserId], [DueDate]) 
INCLUDE ([WorkflowInstanceId], [ActivityId], [TargetRole], [CreatedAt])
WHERE [Status] = 'Pending';
```

---

## 3. Quy chuẩn Truy vấn EF Core Hiệu năng Cao

1. **Luôn dùng `.AsNoTracking()`:** Khi chỉ hiển thị dữ liệu lên màn hình, không cập nhật.
2. **Chọn lọc cột qua `.Select()`:** Tuyệt đối không query toàn bộ Entity nếu chỉ cần 3 cột hiển thị.
3. **Phân trang bắt buộc:** Mọi API trả danh sách phải có `Skip()` và `Take()`, mặc định tối đa 50-100 bản ghi/trang.
4. **Tránh N+1 Query:** Luôn dùng `.Include()` hoặc truy vấn gộp Dapper khi nạp quan hệ 1-N.

