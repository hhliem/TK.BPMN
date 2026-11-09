namespace EnterpriseBpm.Document.Domain.Exceptions;

public class InvalidFileException : Exception
{
    public InvalidFileException(string message) : base(message) { }
}

public class FileTooLargeException : Exception
{
    public FileTooLargeException(long sizeBytes, long maxSizeBytes)
        : base($"Dung lượng tệp ({sizeBytes / 1024 / 1024} MB) vượt quá giới hạn tối đa cho phép ({maxSizeBytes / 1024 / 1024} MB).")
    {
    }
}

public class DocumentNotFoundException : Exception
{
    public DocumentNotFoundException(string identifier)
        : base($"Không tìm thấy tài liệu với định danh: {identifier}")
    {
    }
}

public class TemplateNotFoundException : Exception
{
    public TemplateNotFoundException(string templateKey)
        : base($"Không tìm thấy mẫu tài liệu với khóa: {templateKey}")
    {
    }
}

public class StorageServiceException : Exception
{
    public StorageServiceException(string message, Exception? inner = null)
        : base(message, inner)
    {
    }
}

