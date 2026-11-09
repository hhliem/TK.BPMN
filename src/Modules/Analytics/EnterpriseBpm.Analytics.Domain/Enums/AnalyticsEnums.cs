namespace EnterpriseBpm.Analytics.Domain.Enums;

public enum DayOfWeekType
{
    Sunday = 0,
    Monday = 1,
    Tuesday = 2,
    Wednesday = 3,
    Thursday = 4,
    Friday = 5,
    Saturday = 6
}

public enum ReportExportFormat
{
    Excel = 1,
    Csv = 2,
    Json = 3,
    Pdf = 4
}

public enum ReportFrequency
{
    OnDemand = 0,
    Daily = 1,
    Weekly = 2,
    Monthly = 3,
    Quarterly = 4
}

public enum BottleneckSeverity
{
    None = 0,
    Low = 1,      // Breach 10% - 25%
    Medium = 2,   // Breach 25% - 50%
    High = 3,     // Breach 50% - 100%
    Critical = 4  // Breach > 100% or Wait time > 200% SLA
}

public enum AggregationPeriodType
{
    Hourly = 1,
    Daily = 2,
    Weekly = 3,
    Monthly = 4,
    Yearly = 5
}

public enum DataSourceType
{
    WorkflowSubmissions = 1,
    ApprovalTasks = 2,
    FinancialExpenditure = 3,
    EmployeeProductivity = 4
}

