namespace EnterpriseBpm.SharedKernel.Contracts;

/// <summary>
/// Hợp đồng đánh dấu Entity hỗ trợ xóa mềm (Soft Delete)
/// </summary>
public interface ISoftDeletable
{
    DateTime? DeletedAt { get; set; }
    Guid? DeletedBy { get; set; }
}

