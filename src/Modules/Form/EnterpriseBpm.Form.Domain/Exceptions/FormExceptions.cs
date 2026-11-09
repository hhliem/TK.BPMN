namespace EnterpriseBpm.Form.Domain.Exceptions;

public class FormNotFoundException : Exception
{
    public string FormKey { get; }
    public int? Version { get; }

    public FormNotFoundException(string formKey, int? version = null)
        : base(version.HasValue 
            ? $"Không tìm thấy định nghĩa biểu mẫu '{formKey}' với phiên bản '{version.Value}'."
            : $"Không tìm thấy định nghĩa biểu mẫu hiện hành cho '{formKey}'.")
    {
        FormKey = formKey;
        Version = version;
    }
}

public class InvalidFormSchemaException : Exception
{
    public InvalidFormSchemaException(string message) : base(message)
    {
    }
}

public class TamperedDataException : Exception
{
    public Guid ChildId { get; }
    public Guid ProposedParentId { get; }

    public TamperedDataException(Guid childId, Guid proposedParentId)
        : base($"Phát hiện giả mạo dữ liệu phụ thuộc: Mục '{childId}' không thuộc danh mục cha '{proposedParentId}'. Yêu cầu nộp đơn bị từ chối.")
    {
        ChildId = childId;
        ProposedParentId = proposedParentId;
    }
}

