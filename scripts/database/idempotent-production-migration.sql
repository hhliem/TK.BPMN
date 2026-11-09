-- =========================================================================================
-- ENTERPRISE BPM PLATFORM - IDEMPOTENT PRODUCTION MIGRATION SCRIPT
-- Sprint 08: High-Performance Tuning, Resilience Engineering & Production Hardening
-- Support: Microsoft SQL Server 2019 / 2022
-- Multi-Schema Architecture: 11 Schemas (auth, audit, org, form, workflow, document, 
--                            notification, news, settings, ai, analytics)
-- =========================================================================================

SET NOCOUNT ON;
PRINT '>>> Starting Idempotent Production Migration for Enterprise BPM Platform...';

-- -----------------------------------------------------------------------------------------
-- 1. SCHEMA INITIALIZATION (11 Schemas)
-- -----------------------------------------------------------------------------------------
IF NOT EXISTS (SELECT * FROM sys.schemas WHERE name = N'auth') EXEC('CREATE SCHEMA [auth];');
IF NOT EXISTS (SELECT * FROM sys.schemas WHERE name = N'audit') EXEC('CREATE SCHEMA [audit];');
IF NOT EXISTS (SELECT * FROM sys.schemas WHERE name = N'org') EXEC('CREATE SCHEMA [org];');
IF NOT EXISTS (SELECT * FROM sys.schemas WHERE name = N'form') EXEC('CREATE SCHEMA [form];');
IF NOT EXISTS (SELECT * FROM sys.schemas WHERE name = N'workflow') EXEC('CREATE SCHEMA [workflow];');
IF NOT EXISTS (SELECT * FROM sys.schemas WHERE name = N'document') EXEC('CREATE SCHEMA [document];');
IF NOT EXISTS (SELECT * FROM sys.schemas WHERE name = N'notification') EXEC('CREATE SCHEMA [notification];');
IF NOT EXISTS (SELECT * FROM sys.schemas WHERE name = N'news') EXEC('CREATE SCHEMA [news];');
IF NOT EXISTS (SELECT * FROM sys.schemas WHERE name = N'settings') EXEC('CREATE SCHEMA [settings];');
IF NOT EXISTS (SELECT * FROM sys.schemas WHERE name = N'ai') EXEC('CREATE SCHEMA [ai];');
IF NOT EXISTS (SELECT * FROM sys.schemas WHERE name = N'analytics') EXEC('CREATE SCHEMA [analytics];');
PRINT '>>> [1/12] 11 Database Schemas verified.';

-- -----------------------------------------------------------------------------------------
-- 2. SCHEMA [auth] - Access Governance & RBAC
-- -----------------------------------------------------------------------------------------
IF OBJECT_ID(N'[auth].[Users]', N'U') IS NULL
BEGIN
    CREATE TABLE [auth].[Users] (
        [Id] UNIQUEIDENTIFIER NOT NULL PRIMARY KEY DEFAULT NEWSEQUENTIALID(),
        [Username] NVARCHAR(100) NOT NULL,
        [Email] NVARCHAR(256) NOT NULL,
        [FullName] NVARCHAR(250) NOT NULL,
        [IsActive] BIT NOT NULL DEFAULT 1,
        [CreatedAt] DATETIMEOFFSET NOT NULL DEFAULT SYSDATETIMEOFFSET(),
        CONSTRAINT [UQ_Auth_Users_Username] UNIQUE ([Username]),
        CONSTRAINT [UQ_Auth_Users_Email] UNIQUE ([Email])
    );
END

IF OBJECT_ID(N'[auth].[Roles]', N'U') IS NULL
BEGIN
    CREATE TABLE [auth].[Roles] (
        [Id] UNIQUEIDENTIFIER NOT NULL PRIMARY KEY DEFAULT NEWSEQUENTIALID(),
        [Code] NVARCHAR(50) NOT NULL,
        [Name] NVARCHAR(150) NOT NULL,
        [Description] NVARCHAR(500) NULL,
        [IsActive] BIT NOT NULL DEFAULT 1,
        CONSTRAINT [UQ_Auth_Roles_Code] UNIQUE ([Code])
    );
END

IF OBJECT_ID(N'[auth].[UserRoles]', N'U') IS NULL
BEGIN
    CREATE TABLE [auth].[UserRoles] (
        [UserId] UNIQUEIDENTIFIER NOT NULL,
        [RoleId] UNIQUEIDENTIFIER NOT NULL,
        PRIMARY KEY ([UserId], [RoleId]),
        CONSTRAINT [FK_UserRoles_Users] FOREIGN KEY ([UserId]) REFERENCES [auth].[Users]([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_UserRoles_Roles] FOREIGN KEY ([RoleId]) REFERENCES [auth].[Roles]([Id]) ON DELETE CASCADE
    );
END

IF OBJECT_ID(N'[auth].[Permissions]', N'U') IS NULL
BEGIN
    CREATE TABLE [auth].[Permissions] (
        [Id] UNIQUEIDENTIFIER NOT NULL PRIMARY KEY DEFAULT NEWSEQUENTIALID(),
        [Code] NVARCHAR(100) NOT NULL,
        [Name] NVARCHAR(200) NOT NULL,
        [Category] NVARCHAR(100) NOT NULL,
        CONSTRAINT [UQ_Auth_Permissions_Code] UNIQUE ([Code])
    );
END

IF OBJECT_ID(N'[auth].[RolePermissions]', N'U') IS NULL
BEGIN
    CREATE TABLE [auth].[RolePermissions] (
        [RoleId] UNIQUEIDENTIFIER NOT NULL,
        [PermissionId] UNIQUEIDENTIFIER NOT NULL,
        PRIMARY KEY ([RoleId], [PermissionId]),
        CONSTRAINT [FK_RolePermissions_Roles] FOREIGN KEY ([RoleId]) REFERENCES [auth].[Roles]([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_RolePermissions_Permissions] FOREIGN KEY ([PermissionId]) REFERENCES [auth].[Permissions]([Id]) ON DELETE CASCADE
    );
END
PRINT '>>> [2/12] Schema [auth] tables and constraints verified.';

-- -----------------------------------------------------------------------------------------
-- 3. SCHEMA [audit] - Immutable Audit Trail
-- -----------------------------------------------------------------------------------------
IF OBJECT_ID(N'[audit].[AuditLogs]', N'U') IS NULL
BEGIN
    CREATE TABLE [audit].[AuditLogs] (
        [Id] UNIQUEIDENTIFIER NOT NULL PRIMARY KEY DEFAULT NEWSEQUENTIALID(),
        [ActorId] NVARCHAR(100) NOT NULL,
        [Action] NVARCHAR(150) NOT NULL,
        [EntityName] NVARCHAR(150) NOT NULL,
        [EntityId] NVARCHAR(100) NOT NULL,
        [OldValues] NVARCHAR(MAX) NULL,
        [NewValues] NVARCHAR(MAX) NULL,
        [IpAddress] NVARCHAR(50) NULL,
        [CorrelationId] NVARCHAR(100) NULL,
        [Timestamp] DATETIMEOFFSET NOT NULL DEFAULT SYSDATETIMEOFFSET()
    );
    CREATE NONCLUSTERED INDEX [IX_AuditLogs_Entity_Timestamp] ON [audit].[AuditLogs] ([EntityName], [EntityId], [Timestamp] DESC);
    CREATE NONCLUSTERED INDEX [IX_AuditLogs_CorrelationId] ON [audit].[AuditLogs] ([CorrelationId]) WHERE [CorrelationId] IS NOT NULL;
END
PRINT '>>> [3/12] Schema [audit] table and indexes verified.';

-- -----------------------------------------------------------------------------------------
-- 4. SCHEMA [org] - Organization Hierarchy & Directory
-- -----------------------------------------------------------------------------------------
IF OBJECT_ID(N'[org].[OrgUnits]', N'U') IS NULL
BEGIN
    CREATE TABLE [org].[OrgUnits] (
        [Id] UNIQUEIDENTIFIER NOT NULL PRIMARY KEY DEFAULT NEWSEQUENTIALID(),
        [Code] NVARCHAR(50) NOT NULL,
        [Name] NVARCHAR(250) NOT NULL,
        [ParentId] UNIQUEIDENTIFIER NULL,
        [Level] INT NOT NULL DEFAULT 1,
        [IsActive] BIT NOT NULL DEFAULT 1,
        CONSTRAINT [UQ_Org_OrgUnits_Code] UNIQUE ([Code]),
        CONSTRAINT [FK_OrgUnits_Parent] FOREIGN KEY ([ParentId]) REFERENCES [org].[OrgUnits]([Id])
    );
END

IF OBJECT_ID(N'[org].[JobTitles]', N'U') IS NULL
BEGIN
    CREATE TABLE [org].[JobTitles] (
        [Id] UNIQUEIDENTIFIER NOT NULL PRIMARY KEY DEFAULT NEWSEQUENTIALID(),
        [Code] NVARCHAR(50) NOT NULL,
        [Name] NVARCHAR(200) NOT NULL,
        [Rank] INT NOT NULL DEFAULT 1,
        [IsActive] BIT NOT NULL DEFAULT 1,
        CONSTRAINT [UQ_Org_JobTitles_Code] UNIQUE ([Code])
    );
END

IF OBJECT_ID(N'[org].[Employees]', N'U') IS NULL
BEGIN
    CREATE TABLE [org].[Employees] (
        [Id] UNIQUEIDENTIFIER NOT NULL PRIMARY KEY DEFAULT NEWSEQUENTIALID(),
        [EmployeeCode] NVARCHAR(50) NOT NULL,
        [FullName] NVARCHAR(250) NOT NULL,
        [Email] NVARCHAR(256) NOT NULL,
        [OrgUnitId] UNIQUEIDENTIFIER NOT NULL,
        [JobTitleId] UNIQUEIDENTIFIER NOT NULL,
        [ManagerId] UNIQUEIDENTIFIER NULL,
        [IsActive] BIT NOT NULL DEFAULT 1,
        [CreatedAt] DATETIMEOFFSET NOT NULL DEFAULT SYSDATETIMEOFFSET(),
        CONSTRAINT [UQ_Org_Employees_EmployeeCode] UNIQUE ([EmployeeCode]),
        CONSTRAINT [FK_Employees_OrgUnit] FOREIGN KEY ([OrgUnitId]) REFERENCES [org].[OrgUnits]([Id]),
        CONSTRAINT [FK_Employees_JobTitle] FOREIGN KEY ([JobTitleId]) REFERENCES [org].[JobTitles]([Id]),
        CONSTRAINT [FK_Employees_Manager] FOREIGN KEY ([ManagerId]) REFERENCES [org].[Employees]([Id])
    );
    -- High Performance Covering Index for Org Directory lookups
    CREATE NONCLUSTERED INDEX [IX_Employees_OrgUnit_Active] ON [org].[Employees] ([OrgUnitId], [IsActive]) INCLUDE ([EmployeeCode], [FullName], [Email], [JobTitleId]);
END

IF OBJECT_ID(N'[org].[UserDelegations]', N'U') IS NULL
BEGIN
    CREATE TABLE [org].[UserDelegations] (
        [Id] UNIQUEIDENTIFIER NOT NULL PRIMARY KEY DEFAULT NEWSEQUENTIALID(),
        [DelegatorId] UNIQUEIDENTIFIER NOT NULL,
        [DelegateeId] UNIQUEIDENTIFIER NOT NULL,
        [StartDate] DATETIMEOFFSET NOT NULL,
        [EndDate] DATETIMEOFFSET NOT NULL,
        [Scope] NVARCHAR(100) NOT NULL,
        [IsActive] BIT NOT NULL DEFAULT 1,
        [CreatedAt] DATETIMEOFFSET NOT NULL DEFAULT SYSDATETIMEOFFSET()
    );
END
PRINT '>>> [4/12] Schema [org] tables and covering index verified.';

-- -----------------------------------------------------------------------------------------
-- 5. SCHEMA [form] - Dynamic Form.io Schema & Master Data
-- -----------------------------------------------------------------------------------------
IF OBJECT_ID(N'[form].[FormDefinitions]', N'U') IS NULL
BEGIN
    CREATE TABLE [form].[FormDefinitions] (
        [Id] UNIQUEIDENTIFIER NOT NULL PRIMARY KEY DEFAULT NEWSEQUENTIALID(),
        [FormKey] NVARCHAR(100) NOT NULL,
        [Title] NVARCHAR(250) NOT NULL,
        [Description] NVARCHAR(1000) NULL,
        [SchemaJson] NVARCHAR(MAX) NOT NULL,
        [Version] INT NOT NULL DEFAULT 1,
        [IsPublished] BIT NOT NULL DEFAULT 1,
        [CreatedAt] DATETIMEOFFSET NOT NULL DEFAULT SYSDATETIMEOFFSET(),
        CONSTRAINT [UQ_Form_Definitions_Key] UNIQUE ([FormKey])
    );
END
PRINT '>>> [5/12] Schema [form] tables verified.';

-- -----------------------------------------------------------------------------------------
-- 6. SCHEMA [workflow] - BPM Engine, Submissions, Tasks & Outbox
-- -----------------------------------------------------------------------------------------
IF OBJECT_ID(N'[workflow].[WorkflowSubmissions]', N'U') IS NULL
BEGIN
    CREATE TABLE [workflow].[WorkflowSubmissions] (
        [Id] UNIQUEIDENTIFIER NOT NULL PRIMARY KEY DEFAULT NEWSEQUENTIALID(),
        [FormKey] NVARCHAR(100) NOT NULL,
        [FormDataJson] NVARCHAR(MAX) NOT NULL,
        [SubmittedBy] NVARCHAR(100) NOT NULL,
        [DepartmentId] UNIQUEIDENTIFIER NOT NULL,
        [Status] NVARCHAR(50) NOT NULL DEFAULT 'Submitted',
        [Priority] NVARCHAR(50) NOT NULL DEFAULT 'Normal',
        [Notes] NVARCHAR(1000) NULL,
        [WorkflowInstanceId] NVARCHAR(100) NULL,
        [CreatedAt] DATETIMEOFFSET NOT NULL DEFAULT SYSDATETIMEOFFSET(),
        [UpdatedAt] DATETIMEOFFSET NULL
    );
    CREATE NONCLUSTERED INDEX [IX_WorkflowSubmissions_Status_Created] ON [workflow].[WorkflowSubmissions] ([Status], [CreatedAt] DESC);
    CREATE NONCLUSTERED INDEX [IX_WorkflowSubmissions_SubmittedBy] ON [workflow].[WorkflowSubmissions] ([SubmittedBy], [CreatedAt] DESC);
END

IF OBJECT_ID(N'[workflow].[ApprovalTasks]', N'U') IS NULL
BEGIN
    CREATE TABLE [workflow].[ApprovalTasks] (
        [Id] UNIQUEIDENTIFIER NOT NULL PRIMARY KEY DEFAULT NEWSEQUENTIALID(),
        [WorkflowSubmissionId] UNIQUEIDENTIFIER NOT NULL,
        [WorkflowInstanceId] NVARCHAR(100) NOT NULL,
        [ActivityId] NVARCHAR(100) NOT NULL,
        [TaskName] NVARCHAR(200) NOT NULL,
        [AssigneeId] NVARCHAR(100) NOT NULL,
        [Status] NVARCHAR(50) NOT NULL DEFAULT 'Pending',
        [DueDate] DATETIMEOFFSET NULL,
        [CompletedAt] DATETIMEOFFSET NULL,
        [CompletedBy] NVARCHAR(100) NULL,
        [Decision] NVARCHAR(50) NULL,
        [Comment] NVARCHAR(1000) NULL,
        [CreatedAt] DATETIMEOFFSET NOT NULL DEFAULT SYSDATETIMEOFFSET(),
        CONSTRAINT [FK_ApprovalTasks_Submission] FOREIGN KEY ([WorkflowSubmissionId]) REFERENCES [workflow].[WorkflowSubmissions]([Id]) ON DELETE CASCADE
    );
END

-- High Performance Filtered Index for Pending Tasks (Hot Read Path < 30ms)
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = N'IX_ApprovalTasks_PendingFilter' AND object_id = OBJECT_ID(N'[workflow].[ApprovalTasks]'))
BEGIN
    CREATE NONCLUSTERED INDEX [IX_ApprovalTasks_PendingFilter]
    ON [workflow].[ApprovalTasks] ([AssigneeId], [DueDate])
    INCLUDE ([TaskName], [WorkflowSubmissionId], [CreatedAt])
    WHERE [Status] = 'Pending';
    PRINT '>>> Filtered Index [IX_ApprovalTasks_PendingFilter] created.';
END

-- Transactional Outbox Table for At-Least-Once Delivery (Sprint 08)
IF OBJECT_ID(N'[workflow].[OutboxMessages]', N'U') IS NULL
BEGIN
    CREATE TABLE [workflow].[OutboxMessages] (
        [Id] UNIQUEIDENTIFIER NOT NULL PRIMARY KEY DEFAULT NEWSEQUENTIALID(),
        [EventType] NVARCHAR(250) NOT NULL,
        [Payload] NVARCHAR(MAX) NOT NULL,
        [CreatedAt] DATETIMEOFFSET NOT NULL DEFAULT SYSDATETIMEOFFSET(),
        [ProcessedAt] DATETIMEOFFSET NULL,
        [Error] NVARCHAR(MAX) NULL,
        [RetryCount] INT NOT NULL DEFAULT 0
    );
END

-- Filtered Index for Pending Outbox Polling
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = N'IX_OutboxMessages_PendingFilter' AND object_id = OBJECT_ID(N'[workflow].[OutboxMessages]'))
BEGIN
    CREATE NONCLUSTERED INDEX [IX_OutboxMessages_PendingFilter]
    ON [workflow].[OutboxMessages] ([CreatedAt])
    WHERE [ProcessedAt] IS NULL;
    PRINT '>>> Filtered Index [IX_OutboxMessages_PendingFilter] created.';
END
PRINT '>>> [6/12] Schema [workflow] tables and filtered indexes verified.';

-- -----------------------------------------------------------------------------------------
-- 7. SCHEMA [document] - Templates & Attachments
-- -----------------------------------------------------------------------------------------
IF OBJECT_ID(N'[document].[DocumentTemplates]', N'U') IS NULL
BEGIN
    CREATE TABLE [document].[DocumentTemplates] (
        [Id] UNIQUEIDENTIFIER NOT NULL PRIMARY KEY DEFAULT NEWSEQUENTIALID(),
        [TemplateCode] NVARCHAR(100) NOT NULL,
        [Name] NVARCHAR(250) NOT NULL,
        [FileType] NVARCHAR(50) NOT NULL,
        [StoragePath] NVARCHAR(500) NOT NULL,
        [Version] INT NOT NULL DEFAULT 1,
        [IsActive] BIT NOT NULL DEFAULT 1,
        [CreatedAt] DATETIMEOFFSET NOT NULL DEFAULT SYSDATETIMEOFFSET(),
        CONSTRAINT [UQ_Document_Templates_Code] UNIQUE ([TemplateCode])
    );
END

IF OBJECT_ID(N'[document].[WorkflowAttachments]', N'U') IS NULL
BEGIN
    CREATE TABLE [document].[WorkflowAttachments] (
        [Id] UNIQUEIDENTIFIER NOT NULL PRIMARY KEY DEFAULT NEWSEQUENTIALID(),
        [SubmissionId] UNIQUEIDENTIFIER NOT NULL,
        [FileName] NVARCHAR(255) NOT NULL,
        [ContentType] NVARCHAR(100) NOT NULL,
        [FileSizeBytes] BIGINT NOT NULL,
        [StorageRelativePath] NVARCHAR(500) NOT NULL,
        [StorageProvider] NVARCHAR(50) NOT NULL DEFAULT 'OneDrive',
        [UploadedBy] NVARCHAR(100) NOT NULL,
        [UploadedAt] DATETIMEOFFSET NOT NULL DEFAULT SYSDATETIMEOFFSET()
    );
    CREATE NONCLUSTERED INDEX [IX_WorkflowAttachments_Submission] ON [document].[WorkflowAttachments] ([SubmissionId]);
END
PRINT '>>> [7/12] Schema [document] tables verified.';

-- -----------------------------------------------------------------------------------------
-- 8. SCHEMA [notification] & [news]
-- -----------------------------------------------------------------------------------------
IF OBJECT_ID(N'[notification].[NotificationTemplates]', N'U') IS NULL
BEGIN
    CREATE TABLE [notification].[NotificationTemplates] (
        [Id] UNIQUEIDENTIFIER NOT NULL PRIMARY KEY DEFAULT NEWSEQUENTIALID(),
        [TemplateCode] NVARCHAR(100) NOT NULL,
        [Channel] INT NOT NULL,
        [SubjectTemplate] NVARCHAR(250) NOT NULL,
        [BodyTemplate] NVARCHAR(MAX) NOT NULL,
        [IsActive] BIT NOT NULL DEFAULT 1,
        [CreatedAt] DATETIMEOFFSET NOT NULL DEFAULT SYSDATETIMEOFFSET(),
        CONSTRAINT [UQ_Notif_Templates_Code] UNIQUE ([TemplateCode])
    );
END

IF OBJECT_ID(N'[notification].[Notifications]', N'U') IS NULL
BEGIN
    CREATE TABLE [notification].[Notifications] (
        [Id] UNIQUEIDENTIFIER NOT NULL PRIMARY KEY DEFAULT NEWSEQUENTIALID(),
        [Recipient] NVARCHAR(256) NOT NULL,
        [Channel] INT NOT NULL,
        [Subject] NVARCHAR(250) NOT NULL,
        [Body] NVARCHAR(MAX) NOT NULL,
        [Status] INT NOT NULL DEFAULT 0,
        [ErrorMessage] NVARCHAR(MAX) NULL,
        [RetryCount] INT NOT NULL DEFAULT 0,
        [SentAt] DATETIMEOFFSET NULL,
        [CreatedAt] DATETIMEOFFSET NOT NULL DEFAULT SYSDATETIMEOFFSET()
    );
    CREATE NONCLUSTERED INDEX [IX_Notifications_Status_Created] ON [notification].[Notifications] ([Status], [CreatedAt] ASC);
END

IF OBJECT_ID(N'[news].[NewsArticles]', N'U') IS NULL
BEGIN
    CREATE TABLE [news].[NewsArticles] (
        [Id] UNIQUEIDENTIFIER NOT NULL PRIMARY KEY DEFAULT NEWSEQUENTIALID(),
        [Title] NVARCHAR(300) NOT NULL,
        [Category] NVARCHAR(100) NOT NULL,
        [Summary] NVARCHAR(1000) NOT NULL,
        [ContentHtml] NVARCHAR(MAX) NOT NULL,
        [IsPinned] BIT NOT NULL DEFAULT 0,
        [ViewCount] INT NOT NULL DEFAULT 0,
        [AuthorName] NVARCHAR(150) NOT NULL,
        [PublishedAt] DATETIMEOFFSET NOT NULL DEFAULT SYSDATETIMEOFFSET(),
        [CreatedAt] DATETIMEOFFSET NOT NULL DEFAULT SYSDATETIMEOFFSET()
    );
    CREATE NONCLUSTERED INDEX [IX_News_Category_Published] ON [news].[NewsArticles] ([Category], [PublishedAt] DESC);
END
PRINT '>>> [8/12] Schemas [notification] and [news] tables verified.';

-- -----------------------------------------------------------------------------------------
-- 9. SCHEMA [settings] - System Config & SLA Calendars
-- -----------------------------------------------------------------------------------------
IF OBJECT_ID(N'[settings].[SystemConfigurations]', N'U') IS NULL
BEGIN
    CREATE TABLE [settings].[SystemConfigurations] (
        [Id] UNIQUEIDENTIFIER NOT NULL PRIMARY KEY DEFAULT NEWSEQUENTIALID(),
        [Category] NVARCHAR(100) NOT NULL,
        [Key] NVARCHAR(150) NOT NULL,
        [Value] NVARCHAR(MAX) NOT NULL,
        [Description] NVARCHAR(500) NULL,
        [IsEncrypted] BIT NOT NULL DEFAULT 0,
        [UpdatedAt] DATETIMEOFFSET NOT NULL DEFAULT SYSDATETIMEOFFSET(),
        CONSTRAINT [UQ_SystemConfig_Key] UNIQUE ([Key])
    );
END
PRINT '>>> [9/12] Schema [settings] tables verified.';

-- -----------------------------------------------------------------------------------------
-- 10. SCHEMA [ai] - AI Copilot & Knowledge Embeddings
-- -----------------------------------------------------------------------------------------
IF OBJECT_ID(N'[ai].[AiConfigs]', N'U') IS NULL
BEGIN
    CREATE TABLE [ai].[AiConfigs] (
        [Id] UNIQUEIDENTIFIER NOT NULL PRIMARY KEY DEFAULT NEWSEQUENTIALID(),
        [ActiveProvider] NVARCHAR(50) NOT NULL DEFAULT 'Gemini',
        [GeminiApiKey] NVARCHAR(500) NULL,
        [OpenAiApiKey] NVARCHAR(500) NULL,
        [ClaudeApiKey] NVARCHAR(500) NULL,
        [CustomEndpoint] NVARCHAR(500) NULL,
        [QdrantEndpoint] NVARCHAR(500) NOT NULL DEFAULT 'http://localhost:6333',
        [CollectionName] NVARCHAR(100) NOT NULL DEFAULT 'enterprise_regulations',
        [EnableAiAssistant] BIT NOT NULL DEFAULT 1,
        [UpdatedAt] DATETIMEOFFSET NOT NULL DEFAULT SYSDATETIMEOFFSET()
    );
END

IF OBJECT_ID(N'[ai].[AiAuditLogs]', N'U') IS NULL
BEGIN
    CREATE TABLE [ai].[AiAuditLogs] (
        [Id] UNIQUEIDENTIFIER NOT NULL PRIMARY KEY DEFAULT NEWSEQUENTIALID(),
        [UserId] NVARCHAR(100) NOT NULL,
        [Prompt] NVARCHAR(MAX) NOT NULL,
        [Response] NVARCHAR(MAX) NOT NULL,
        [Provider] NVARCHAR(50) NOT NULL,
        [ConfidenceScore] FLOAT NOT NULL DEFAULT 0.0,
        [Timestamp] DATETIMEOFFSET NOT NULL DEFAULT SYSDATETIMEOFFSET()
    );
END
PRINT '>>> [10/12] Schema [ai] tables verified.';

-- -----------------------------------------------------------------------------------------
-- 11. SCHEMA [analytics] - Working Calendar, Metrics & Scheduled Reports (Sprint 07)
-- -----------------------------------------------------------------------------------------
IF OBJECT_ID(N'[analytics].[WorkingCalendars]', N'U') IS NULL
BEGIN
    CREATE TABLE [analytics].[WorkingCalendars] (
        [Id] UNIQUEIDENTIFIER NOT NULL PRIMARY KEY DEFAULT NEWSEQUENTIALID(),
        [CalendarCode] NVARCHAR(50) NOT NULL,
        [Name] NVARCHAR(200) NOT NULL,
        [WorkingDaysOfWeek] NVARCHAR(50) NOT NULL DEFAULT 'Monday,Tuesday,Wednesday,Thursday,Friday',
        [WorkStartTime] TIME NOT NULL DEFAULT '08:30:00',
        [WorkEndTime] TIME NOT NULL DEFAULT '17:30:00',
        [LunchBreakMinutes] INT NOT NULL DEFAULT 60,
        [HolidaysJson] NVARCHAR(MAX) NOT NULL DEFAULT '[]',
        [IsDefault] BIT NOT NULL DEFAULT 1,
        [CreatedAt] DATETIMEOFFSET NOT NULL DEFAULT SYSDATETIMEOFFSET(),
        CONSTRAINT [UQ_Analytics_CalendarCode] UNIQUE ([CalendarCode])
    );
END

IF OBJECT_ID(N'[analytics].[ProcessMetrics]', N'U') IS NULL
BEGIN
    CREATE TABLE [analytics].[ProcessMetrics] (
        [Id] UNIQUEIDENTIFIER NOT NULL PRIMARY KEY DEFAULT NEWSEQUENTIALID(),
        [FormKey] NVARCHAR(100) NOT NULL,
        [ActivityName] NVARCHAR(200) NOT NULL,
        [AssigneeId] NVARCHAR(100) NOT NULL,
        [ProcessingTimeMinutes] FLOAT NOT NULL,
        [IsSlaBreached] BIT NOT NULL DEFAULT 0,
        [CompletedDate] DATETIMEOFFSET NOT NULL,
        [CreatedAt] DATETIMEOFFSET NOT NULL DEFAULT SYSDATETIMEOFFSET()
    );
    CREATE NONCLUSTERED INDEX [IX_ProcessMetrics_FormKey_Date] ON [analytics].[ProcessMetrics] ([FormKey], [CompletedDate] DESC);
    CREATE NONCLUSTERED INDEX [IX_ProcessMetrics_Assignee] ON [analytics].[ProcessMetrics] ([AssigneeId], [IsSlaBreached]);
END

IF OBJECT_ID(N'[analytics].[ReportDefinitions]', N'U') IS NULL
BEGIN
    CREATE TABLE [analytics].[ReportDefinitions] (
        [Id] UNIQUEIDENTIFIER NOT NULL PRIMARY KEY DEFAULT NEWSEQUENTIALID(),
        [ReportCode] NVARCHAR(100) NOT NULL,
        [Title] NVARCHAR(250) NOT NULL,
        [Description] NVARCHAR(1000) NULL,
        [ReportType] NVARCHAR(50) NOT NULL,
        [FilterConfigJson] NVARCHAR(MAX) NOT NULL,
        [DisplayColumnsJson] NVARCHAR(MAX) NOT NULL,
        [CreatedBy] NVARCHAR(100) NOT NULL,
        [CreatedAt] DATETIMEOFFSET NOT NULL DEFAULT SYSDATETIMEOFFSET(),
        CONSTRAINT [UQ_Analytics_ReportCode] UNIQUE ([ReportCode])
    );
END

IF OBJECT_ID(N'[analytics].[ScheduledReports]', N'U') IS NULL
BEGIN
    CREATE TABLE [analytics].[ScheduledReports] (
        [Id] UNIQUEIDENTIFIER NOT NULL PRIMARY KEY DEFAULT NEWSEQUENTIALID(),
        [ReportDefinitionId] UNIQUEIDENTIFIER NOT NULL,
        [Frequency] NVARCHAR(50) NOT NULL,
        [RecipientsJson] NVARCHAR(MAX) NOT NULL,
        [ExportFormat] NVARCHAR(20) NOT NULL DEFAULT 'XLSX',
        [NextRunAt] DATETIMEOFFSET NOT NULL,
        [LastRunAt] DATETIMEOFFSET NULL,
        [IsActive] BIT NOT NULL DEFAULT 1,
        [CreatedAt] DATETIMEOFFSET NOT NULL DEFAULT SYSDATETIMEOFFSET(),
        CONSTRAINT [FK_ScheduledReports_ReportDefinition] FOREIGN KEY ([ReportDefinitionId]) REFERENCES [analytics].[ReportDefinitions]([Id]) ON DELETE CASCADE
    );
END
PRINT '>>> [11/12] Schema [analytics] tables and performance indexes verified.';

-- -----------------------------------------------------------------------------------------
-- 12. COMPLETION SUMMARY
-- -----------------------------------------------------------------------------------------
PRINT '=========================================================================================';
PRINT '>>> SUCCESS: Enterprise BPM Platform Idempotent Production Migration Completed 100%!';
PRINT '>>> All 11 schemas, tables, primary keys, foreign keys, and covering/filtered indexes are ready.';
PRINT '=========================================================================================';

