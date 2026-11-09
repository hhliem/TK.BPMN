using EnterpriseBpm.AuditLog.Application.Contracts;
using EnterpriseBpm.AuditLog.Domain.Entities;
using EnterpriseBpm.AuditLog.Infrastructure.Persistence;
using Microsoft.Extensions.Logging;

namespace EnterpriseBpm.AuditLog.Infrastructure.Services;

public class AuditLogService : IAuditLogService
{
    private readonly AuditDbContext _context;
    private readonly ILogger<AuditLogService> _logger;

    public AuditLogService(AuditDbContext context, ILogger<AuditLogService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task LogAsync(AuditEntry entry, CancellationToken cancellationToken = default)
    {
        try
        {
            await _context.AuditEntries.AddAsync(entry, cancellationToken);
            await _context.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi xảy ra khi lưu vết kiểm toán cho Entity {EntityType}:{EntityId}", entry.EntityType, entry.EntityId);
        }
    }

    public async Task LogSecurityEventAsync(SecurityEvent securityEvent, CancellationToken cancellationToken = default)
    {
        try
        {
            await _context.SecurityEvents.AddAsync(securityEvent, cancellationToken);
            await _context.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi xảy ra khi lưu sự kiện bảo mật {EventType}", securityEvent.EventType);
        }
    }

    public async Task LogBatchAsync(IEnumerable<AuditEntry> entries, CancellationToken cancellationToken = default)
    {
        try
        {
            await _context.AuditEntries.AddRangeAsync(entries, cancellationToken);
            await _context.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi xảy ra khi lưu batch vết kiểm toán.");
        }
    }
}

