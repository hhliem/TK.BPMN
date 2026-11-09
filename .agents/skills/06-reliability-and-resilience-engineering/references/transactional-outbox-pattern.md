# Hướng dẫn Kỹ thuật Transactional Outbox Pattern

## 1. Vấn đề của Dual-Write trong Modular Monolith

Khi một thao tác phê duyệt đơn diễn ra:
```
Bước 1: Cập nhật WorkflowSubmissions (Approved) vào SQL Server -> Thành công!
Bước 2: Gọi MediatR.Publish(WorkflowApprovedEvent) -> Bắn sang Notification để gửi mail.
```
Nếu server mất điện, crash app hoặc restart ngay giữa Bước 1 và Bước 2:
- Trạng thái đơn đã lưu là `Approved` trong DB.
- Sự kiện trong RAM biến mất hoàn toàn.
- Người nộp đơn không nhận được email, tài liệu hợp đồng không được sinh ra, chỉ số BI bị sai lệch.
Đây là vấn đề kinh điển **Dual-Write Problem**.

---

## 2. Giải pháp: Transactional Outbox Pattern

Thay vì bắn event trực tiếp ra ngoài RAM, ta ghi nhận event vào bảng `[workflow].[OutboxMessages]` **trong cùng Database Transaction** với câu lệnh cập nhật Entity.

### Thiết kế Bảng Outbox (áp dụng chung cho các module)
```sql
CREATE TABLE [workflow].[OutboxMessages] (
    [Id] UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    [EventType] NVARCHAR(255) NOT NULL,
    [PayloadJson] NVARCHAR(MAX) NOT NULL,
    [CreatedAt] DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    [ProcessedAt] DATETIME2 NULL,
    [RetryCount] INT NOT NULL DEFAULT 0,
    [LastError] NVARCHAR(MAX) NULL
);

CREATE INDEX [IX_Outbox_Pending] 
ON [workflow].[OutboxMessages]([ProcessedAt], [RetryCount], [CreatedAt]);
```

---

## 3. Quy trình Xử lý của Outbox Background Worker

```
[HTTP Request / Controller]
        │
        ▼ (Single DB Transaction)
┌───────────────────────────────────────────┐
│ 1. UPDATE [workflow].[WorkflowSubmissions]│
│ 2. INSERT [workflow].[OutboxMessages]     │
└───────────────────────────────────────────┘
        │
        ▼
   Commit DB Thành công!
        │
        │ (Quét ngầm định kỳ 5 giây)
        ▼
┌───────────────────────────────────────────┐
│ OutboxProcessorWorker                     │
│ 1. Đọc các bản ghi ProcessedAt IS NULL    │
│ 2. Publish qua MediatR                   │
│ 3. Đánh dấu ProcessedAt = GETUTCDATE()   │
└───────────────────────────────────────────┘
```
- Nếu gặp lỗi: Tăng `RetryCount`, ghi lại `LastError`, thử lại theo khoảng thời gian lũy tiến.
- Đảm bảo **At-Least-Once Delivery**: Sự kiện chắc chắn sẽ được xử lý thành công!

