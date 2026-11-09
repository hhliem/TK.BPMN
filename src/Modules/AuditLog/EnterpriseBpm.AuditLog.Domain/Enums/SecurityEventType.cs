namespace EnterpriseBpm.AuditLog.Domain.Enums;

public enum SecurityEventType
{
    Login = 1,
    Logout = 2,
    LoginFailed = 3,
    PasswordChanged = 4,
    RoleAssigned = 5,
    RoleRevoked = 6,
    PermissionChanged = 7,
    DelegationCreated = 8,
    ApprovalLimitModified = 9
}

