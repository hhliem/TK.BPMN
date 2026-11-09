namespace EnterpriseBpm.AuditLog.Domain.Entities;

/// <summary>
/// Thực thể ghi nhận vết kiểm toán mọi thao tác CUD trong hệ thống
/// Khóa chính BIGINT IDENTITY tối ưu hiệu năng ghi chèn (Append-Only)
/// </summary>
public class AuditEntry
{
    public long Id { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public Guid UserId { get; set; }
    public string UserName { get; set; } = default!;
    public string Action { get; set; } = default!;
    public string EntityType { get; set; } = default!;
    public string EntityId { get; set; } = default!;
    public string ModuleName { get; set; } = default!;
    public string? OldValues { get; set; }
    public string? NewValues { get; set; }
    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }
    public string? AdditionalData { get; set; }
}

