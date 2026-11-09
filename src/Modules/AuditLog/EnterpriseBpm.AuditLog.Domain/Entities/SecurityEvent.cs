namespace EnterpriseBpm.AuditLog.Domain.Entities;

/// <summary>
/// Thực thể ghi nhận các sự kiện bảo mật, phân quyền và đăng nhập
/// </summary>
public class SecurityEvent
{
    public long Id { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public Guid? UserId { get; set; }
    public string? UserName { get; set; }
    public string EventType { get; set; } = default!;
    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }
    public bool IsSuccess { get; set; } = true;
    public string? FailureReason { get; set; }
    public string? AdditionalData { get; set; }
}

