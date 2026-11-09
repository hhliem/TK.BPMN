using EnterpriseBpm.Notification.Domain.Enums;
using EnterpriseBpm.SharedKernel;

namespace EnterpriseBpm.Notification.Domain.Entities;

public class NotificationLog : BaseEntity
{
    public string TemplateCode { get; set; } = string.Empty;
    public string? RecipientEmail { get; set; }
    public string? RecipientPhone { get; set; }
    public NotificationChannel Channel { get; set; } = NotificationChannel.Email;
    public NotificationStatus Status { get; set; } = NotificationStatus.Pending;
    public int RetryCount { get; set; } = 0;
    public string? LastError { get; set; }
    public Guid? SourceEventId { get; set; }
    public DateTime? SentAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public NotificationLog() { }

    public NotificationLog(string templateCode, NotificationChannel channel, string? recipientEmail, string? recipientPhone, Guid? sourceEventId)
    {
        TemplateCode = templateCode;
        Channel = channel;
        RecipientEmail = recipientEmail;
        RecipientPhone = recipientPhone;
        SourceEventId = sourceEventId;
        Status = NotificationStatus.Pending;
        RetryCount = 0;
        CreatedAt = DateTime.UtcNow;
    }

    public void MarkSent()
    {
        Status = NotificationStatus.Sent;
        SentAt = DateTime.UtcNow;
        LastError = null;
    }

    public void RecordFailure(string error)
    {
        RetryCount++;
        LastError = error;
        if (RetryCount >= 3)
        {
            Status = NotificationStatus.Failed;
        }
    }
}
