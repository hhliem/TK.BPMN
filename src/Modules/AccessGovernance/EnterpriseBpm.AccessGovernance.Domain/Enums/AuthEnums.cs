namespace EnterpriseBpm.AccessGovernance.Domain.Enums;

public enum RoleType
{
    System = 1,
    Business = 2,
    Custom = 3
}

public enum DataScopeType
{
    Self = 1,
    Department = 2,
    Division = 3,
    Global = 4,
    CustomOrgFilter = 5
}

public enum SubjectType
{
    Role = 1,
    Group = 2,
    OrgUnit = 3,
    User = 4
}

