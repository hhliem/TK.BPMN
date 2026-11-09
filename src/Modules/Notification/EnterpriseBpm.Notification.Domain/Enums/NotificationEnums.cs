namespace EnterpriseBpm.Notification.Domain.Enums;

public enum NotificationChannel
{
    Email,
    Teams,
    Zalo,
    Sms
}

public enum NotificationStatus
{
    Pending,
    Sent,
    Failed
}

