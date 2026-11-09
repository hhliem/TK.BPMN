using EnterpriseBpm.Notification.Domain.Enums;
using EnterpriseBpm.SharedKernel;

namespace EnterpriseBpm.Notification.Domain.Entities;

public class NotificationTemplate : BaseEntity
{
    public string TemplateCode { get; set; } = string.Empty;
    public NotificationChannel Channel { get; set; } = NotificationChannel.Email;
    public string? SubjectTemplate { get; set; }
    public string BodyTemplate { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public NotificationTemplate() { }

    public NotificationTemplate(string templateCode, NotificationChannel channel, string? subjectTemplate, string bodyTemplate)
    {
        TemplateCode = templateCode;
        Channel = channel;
        SubjectTemplate = subjectTemplate;
        BodyTemplate = bodyTemplate;
        IsActive = true;
        UpdatedAt = DateTime.UtcNow;
    }
}
