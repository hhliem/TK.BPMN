namespace EnterpriseBpm.Notification.Domain.Exceptions;

public class NotificationTemplateNotFoundException : Exception
{
    public string TemplateCode { get; }

    public NotificationTemplateNotFoundException(string templateCode)
        : base($"Không tìm thấy mẫu thông báo với mã: '{templateCode}'.")
    {
        TemplateCode = templateCode;
    }
}

public class NotificationDispatchException : Exception
{
    public NotificationDispatchException(string message, Exception? innerException = null)
        : base(message, innerException)
    {
    }
}

