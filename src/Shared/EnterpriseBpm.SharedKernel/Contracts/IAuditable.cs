namespace EnterpriseBpm.SharedKernel.Contracts;

/// <summary>
/// Hợp đồng đánh dấu Entity hỗ trợ tự động ghi nhận thời gian và người tạo/sửa
/// </summary>
public interface IAuditable
{
    DateTime CreatedAt { get; set; }
    Guid? CreatedBy { get; set; }
    DateTime? UpdatedAt { get; set; }
    Guid? UpdatedBy { get; set; }
}

