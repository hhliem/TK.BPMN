# Hướng dẫn Kỹ thuật Caching Đa Tầng & Tối ưu Đọc với Dapper

## 1. Kiến trúc Caching Đa Tầng (L1 MemoryCache + L2 Redis)

```
Client Request
      │
      ▼
┌──────────────┐      HIT (< 1ms)
│   L1 Cache   │ ───────────────► Return Data
│ (IMemory)    │
└──────┬───────┘
      │ MISS
      ▼
┌──────────────┐      HIT (< 5ms)
│   L2 Cache   │ ───────────────► Populate L1 ──► Return Data
│   (Redis)    │
└──────┬───────┘
      │ MISS
      ▼
┌──────────────┐
│  SQL Server  │ ───────────────► Populate L2 & L1 ──► Return Data
└──────────────┘
```

### Triển khai Cache-Aside Pattern với Redis:
- **Key Naming Convention:** `{module}:{entity}:{identifier}` (ví dụ: `form:masterdata:CostCenters:parent:root`)
- **Serialization:** Sử dụng `System.Text.Json` với `JsonSerializerOptions` đã được tối ưu hóa (Static cached instance).
- **TTL Dynamic Strategy:**
  - Danh mục ít biến động (Positions, Categories): TTL 4 - 8 giờ.
  - Form Definitions: TTL 1 giờ (kèm xóa cache ngay lập tức khi xuất bản version mới).
  - Metrics Analytics: TTL 5 - 15 phút.

---

## 2. CQRS: Tối ưu Truy vấn Đọc Bằng Dapper

Đối với các API tra cứu danh sách có lượng truy cập lớn (như Bảng danh sách phiếu cần duyệt, Danh bạ nhân sự, Báo cáo KPI), Entity Framework Core mang lại chi phí quản lý đối tượng (Allocation) không cần thiết. Áp dụng Dapper cho đường đọc:

### So sánh Hiệu năng (10,000 bản ghi đọc):
| Phương thức | Thời gian thực thi | Bộ nhớ Cấp phát (Memory Allocation) |
|---|---|---|
| EF Core Tracking | $145\text{ ms}$ | $12.4\text{ MB}$ |
| EF Core AsNoTracking | $78\text{ ms}$ | $6.2\text{ MB}$ |
| **Dapper Raw Query** | **$31\text{ ms}$** | **$1.8\text{ MB}$** |

```csharp
public class DapperApprovalTaskQueryService : IApprovalTaskQueryService
{
    private readonly IDbConnectionFactory _connectionFactory;

    public DapperApprovalTaskQueryService(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<PagedResult<ApprovalTaskItemDto>> GetUserPendingTasksAsync(
        Guid userId, int pageIndex, int pageSize, CancellationToken cancellationToken)
    {
        const string sql = @"
            SELECT 
                t.[Id], t.[WorkflowInstanceId], t.[ActivityId], t.[TargetRole],
                t.[Status], t.[DueDate], t.[IsEscalated], t.[CreatedAt],
                s.[FormKey], s.[SubmittedByUserId], s.[Priority]
            FROM [workflow].[ApprovalTasks] t WITH (NOLOCK)
            INNER JOIN [workflow].[WorkflowSubmissions] s WITH (NOLOCK) 
                ON t.[WorkflowInstanceId] = s.[WorkflowInstanceId]
            WHERE t.[AssignedToUserId] = @UserId 
              AND t.[Status] = 'Pending'
            ORDER BY t.[DueDate] ASC, t.[CreatedAt] DESC
            OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY;

            SELECT COUNT(1) 
            FROM [workflow].[ApprovalTasks] WITH (NOLOCK)
            WHERE [AssignedToUserId] = @UserId AND [Status] = 'Pending';";

        using var connection = _connectionFactory.CreateConnection();
        using var multi = await connection.QueryMultipleAsync(
            new CommandDefinition(sql, new { 
                UserId = userId, 
                Offset = (pageIndex - 1) * pageSize, 
                PageSize = pageSize 
            }, cancellationToken: cancellationToken));

        var items = (await multi.ReadAsync<ApprovalTaskItemDto>()).ToList();
        var totalCount = await multi.ReadSingleAsync<int>();

        return new PagedResult<ApprovalTaskItemDto>(items, totalCount, pageIndex, pageSize);
    }
}
```

