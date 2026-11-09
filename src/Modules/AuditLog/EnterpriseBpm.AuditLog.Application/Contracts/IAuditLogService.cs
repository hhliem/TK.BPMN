using EnterpriseBpm.AuditLog.Domain.Entities;

namespace EnterpriseBpm.AuditLog.Application.Contracts;

public interface IAuditLogService
{
    Task LogAsync(AuditEntry entry, CancellationToken cancellationToken = default);
    Task LogSecurityEventAsync(SecurityEvent securityEvent, CancellationToken cancellationToken = default);
    Task LogBatchAsync(IEnumerable<AuditEntry> entries, CancellationToken cancellationToken = default);
}

