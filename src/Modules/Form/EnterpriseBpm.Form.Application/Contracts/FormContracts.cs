using EnterpriseBpm.Form.Application.DTOs;

namespace EnterpriseBpm.Form.Application.Contracts;

public interface IFormVersionService
{
    /// <summary>
    /// Đăng ký biểu mẫu mới lần đầu tiên (Version 1, IsLatest = true)
    /// </summary>
    Task<Guid> RegisterFormAsync(RegisterFormCommand command, Guid? publishedBy, CancellationToken cancellationToken = default);

    /// <summary>
    /// Tạo phiên bản mới từ phiên bản hiện hành. Tự động tăng Version, 
    /// đặt IsLatest=0 cho bản cũ, IsLatest=1 cho bản mới.
    /// </summary>
    Task<FormDefinitionDto> CreateNewVersionAsync(
        string formKey,
        string schemaJson,
        string? changeNotes,
        Guid? publishedBy,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Lấy phiên bản cụ thể của form theo FormKey + Version.
    /// Nếu không truyền version, trả về bản IsLatest=1.
    /// </summary>
    Task<FormDefinitionDto?> GetVersionAsync(
        string formKey,
        int? version,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Rollback về phiên bản cũ: tạo bản sao của targetVersion thành phiên bản mới nhất.
    /// KHÔNG xóa bản ghi cũ để bảo toàn lịch sử.
    /// </summary>
    Task<FormDefinitionDto> RollbackToVersionAsync(
        string formKey,
        int targetVersion,
        string? rollbackReason,
        Guid? rolledBackBy,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Lấy lịch sử tất cả các phiên bản của một FormKey
    /// </summary>
    Task<IReadOnlyList<FormVersionHistoryDto>> GetVersionHistoryAsync(
        string formKey, 
        CancellationToken cancellationToken = default);
}

public interface IMasterDataService
{
    /// <summary>
    /// Lấy danh sách danh mục con phục vụ Cascading Loading (< 10ms qua Redis/Memory cache)
    /// </summary>
    Task<IReadOnlyList<MasterItemDto>> GetItemsAsync(
        string categoryKey, 
        Guid? parentId, 
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Kiểm tra tính hợp lệ quan hệ cha-con (chống giả mạo dữ liệu Client)
    /// </summary>
    Task<bool> ValidateItemBelongsToParentAsync(
        Guid childId, 
        Guid parentId, 
        CancellationToken cancellationToken = default);

    Task<Guid> CreateItemAsync(
        CreateMasterItemCommand command, 
        CancellationToken cancellationToken = default);
}

