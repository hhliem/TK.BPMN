using System.Text.Json;
using EnterpriseBpm.AuditLog.Application.Contracts;
using EnterpriseBpm.AuditLog.Domain.Entities;
using EnterpriseBpm.SharedKernel.Contracts;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace EnterpriseBpm.AuditLog.Infrastructure.Interceptors;

public class AuditSaveChangesInterceptor : SaveChangesInterceptor
{
    private readonly IAuditLogService _auditService;
    private readonly ICurrentUserService _currentUser;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public AuditSaveChangesInterceptor(
        IAuditLogService auditService,
        ICurrentUserService currentUser,
        IHttpContextAccessor httpContextAccessor)
    {
        _auditService = auditService;
        _currentUser = currentUser;
        _httpContextAccessor = httpContextAccessor;
    }

    public override async ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        var context = eventData.Context;
        if (context == null) return result;

        // Tránh lặp vô hạn khi chính AuditDbContext lưu AuditEntries
        if (context.GetType().Name == "AuditDbContext")
            return result;

        var httpContext = _httpContextAccessor.HttpContext;
        var ipAddress = httpContext?.Connection?.RemoteIpAddress?.ToString();
        var userAgent = httpContext?.Request?.Headers["User-Agent"].ToString();

        var modifiedEntries = context.ChangeTracker.Entries()
            .Where(e => e.State is EntityState.Added or EntityState.Modified or EntityState.Deleted)
            .ToList();

        if (!modifiedEntries.Any()) return result;

        var auditEntries = new List<AuditEntry>();

        foreach (var entry in modifiedEntries)
        {
            var entityId = entry.Properties.FirstOrDefault(p => p.Metadata.IsPrimaryKey())?.CurrentValue?.ToString() ?? "Unknown";
            var entityType = entry.Entity.GetType().Name;

            var auditEntry = new AuditEntry
            {
                UserId = _currentUser.UserId ?? Guid.Empty,
                UserName = _currentUser.UserName ?? "System",
                Action = entry.State.ToString(),
                EntityType = entityType,
                EntityId = entityId,
                ModuleName = ResolveModuleName(entry.Entity.GetType()),
                OldValues = entry.State != EntityState.Added ? SerializeProperties(entry.OriginalValues) : null,
                NewValues = entry.State != EntityState.Deleted ? SerializeProperties(entry.CurrentValues) : null,
                IpAddress = ipAddress,
                UserAgent = userAgent,
                Timestamp = DateTime.UtcNow
            };

            auditEntries.Add(auditEntry);
        }

        if (auditEntries.Any())
        {
            await _auditService.LogBatchAsync(auditEntries, cancellationToken);
        }

        return result;
    }

    private static string? SerializeProperties(PropertyValues values)
    {
        var dictionary = values.Properties.ToDictionary(p => p.Name, p => values[p]);
        return JsonSerializer.Serialize(dictionary);
    }

    private static string ResolveModuleName(Type entityType)
    {
        var ns = entityType.Namespace ?? "";
        if (ns.Contains("Org")) return "OrgManagement";
        if (ns.Contains("Form")) return "FormMasterData";
        if (ns.Contains("Workflow")) return "WorkflowEngine";
        if (ns.Contains("Document")) return "DocumentManagement";
        if (ns.Contains("Notification")) return "Notification";
        if (ns.Contains("Ai")) return "AiCopilot";
        if (ns.Contains("Settings")) return "SystemSettings";
        if (ns.Contains("News")) return "InternalNews";
        if (ns.Contains("AccessGovernance") || ns.Contains("Auth")) return "AccessGovernance";
        if (ns.Contains("Analytics")) return "AnalyticsReporting";
        return "System";
    }
}

