using EnterpriseBpm.AuditLog.Application.DTOs;

namespace EnterpriseBpm.AuditLog.Application.Contracts;

public interface IAuditQueryService
{
    Task<PagedResult<AuditEntryDto>> GetEntityHistoryAsync(
        string entityType, 
        string entityId, 
        int pageIndex, 
        int pageSize, 
        CancellationToken cancellationToken = default);

    Task<PagedResult<AuditEntryDto>> GetUserActivityAsync(
        Guid userId, 
        DateTime? from, 
        DateTime? to, 
        int pageIndex, 
        int pageSize, 
        CancellationToken cancellationToken = default);

    Task<PagedResult<SecurityEventDto>> GetSecurityEventsAsync(
        DateTime? from, 
        DateTime? to, 
        string? eventType, 
        int pageIndex, 
        int pageSize, 
        CancellationToken cancellationToken = default);

    Task<byte[]> ExportExcelAsync(
        DateTime from, 
        DateTime to, 
        string? moduleName, 
        CancellationToken cancellationToken = default);
}

