namespace EnterpriseBpm.AuditLog.Application.DTOs;

public record AuditEntryDto(
    long Id,
    DateTime Timestamp,
    Guid UserId,
    string UserName,
    string Action,
    string EntityType,
    string EntityId,
    string ModuleName,
    string? OldValues,
    string? NewValues,
    string? IpAddress,
    string? UserAgent
);

public record SecurityEventDto(
    long Id,
    DateTime Timestamp,
    Guid? UserId,
    string? UserName,
    string EventType,
    string? IpAddress,
    bool IsSuccess,
    string? FailureReason
);

