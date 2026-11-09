using EnterpriseBpm.AccessGovernance.Infrastructure;
using EnterpriseBpm.AccessGovernance.Infrastructure.Persistence;
using EnterpriseBpm.AccessGovernance.Infrastructure.Seed;
using EnterpriseBpm.AccessGovernance.Presentation.Controllers;
using EnterpriseBpm.ApiHost.Seed;
using EnterpriseBpm.ApiHost.Services;
using EnterpriseBpm.AuditLog.Infrastructure;
using EnterpriseBpm.AuditLog.Infrastructure.Persistence;
using EnterpriseBpm.AuditLog.Presentation.Controllers;
using EnterpriseBpm.Document.Infrastructure;
using EnterpriseBpm.Document.Infrastructure.Persistence;
using EnterpriseBpm.Document.Presentation.Controllers;
using EnterpriseBpm.Form.Infrastructure;
using EnterpriseBpm.Form.Infrastructure.Persistence;
using EnterpriseBpm.Form.Presentation.Controllers;
using EnterpriseBpm.InternalNews.Infrastructure;
using EnterpriseBpm.InternalNews.Infrastructure.Persistence;
using EnterpriseBpm.InternalNews.Presentation.Controllers;
using EnterpriseBpm.Notification.Infrastructure;
using EnterpriseBpm.Notification.Infrastructure.Persistence;
using EnterpriseBpm.Notification.Presentation.Controllers;
using EnterpriseBpm.Org.Infrastructure;
using EnterpriseBpm.Org.Infrastructure.Persistence;
using EnterpriseBpm.Org.Presentation.Controllers;
using EnterpriseBpm.SharedKernel.Contracts;
using EnterpriseBpm.SystemSettings.Infrastructure;
using EnterpriseBpm.SystemSettings.Infrastructure.Persistence;
using EnterpriseBpm.SystemSettings.Presentation.Controllers;
using EnterpriseBpm.AiAssistant.Infrastructure.Extensions;
using EnterpriseBpm.AiAssistant.Infrastructure.Persistence;
using EnterpriseBpm.AiAssistant.Presentation.Controllers;
using EnterpriseBpm.Analytics.Domain.Entities;
using EnterpriseBpm.Analytics.Infrastructure.Extensions;
using EnterpriseBpm.Analytics.Infrastructure.Persistence;
using EnterpriseBpm.Analytics.Presentation.Controllers;
using EnterpriseBpm.Workflow.Infrastructure;
using EnterpriseBpm.Workflow.Infrastructure.Persistence;
using EnterpriseBpm.Workflow.Presentation.Controllers;
using EnterpriseBpm.SharedKernel.Caching;
using EnterpriseBpm.SharedKernel.Health;
using EnterpriseBpm.SharedKernel.Logging;
using EnterpriseBpm.SharedKernel.Security;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

// 1. Core Services & Current User Context
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUserService, CurrentUserService>();

// 2. Multi-Schema Infrastructure Registration (AuditLog, AccessGovernance, Org, Form, Workflow, Document, Notification, InternalNews, SystemSettings)
builder.Services.AddAuditLogInfrastructure(builder.Configuration);
builder.Services.AddAccessGovernanceInfrastructure(builder.Configuration);
builder.Services.AddOrgInfrastructure(builder.Configuration);
builder.Services.AddFormInfrastructure(builder.Configuration);
builder.Services.AddWorkflowInfrastructure(builder.Configuration);
builder.Services.AddDocumentInfrastructure(builder.Configuration);
builder.Services.AddNotificationInfrastructure(builder.Configuration);
builder.Services.AddInternalNewsInfrastructure(builder.Configuration);
builder.Services.AddSystemSettingsInfrastructure(builder.Configuration);
builder.Services.AddAiAssistantModule(builder.Configuration);
builder.Services.AddAnalyticsModule(builder.Configuration);

// 3. Distributed Cache Configuration (Redis Server riêng hoặc In-Memory Fallback)
var redisConnectionString = builder.Configuration.GetConnectionString("Redis") 
    ?? builder.Configuration["Redis:ConnectionString"];
var redisInstanceName = builder.Configuration["Redis:InstanceName"] ?? "EnterpriseBpm:";

if (!string.IsNullOrWhiteSpace(redisConnectionString))
{
    builder.Services.AddStackExchangeRedisCache(options =>
    {
        options.Configuration = redisConnectionString;
        options.InstanceName = redisInstanceName;
    });
    Console.WriteLine($"[Redis Cache] Đã cấu hình Redis Distributed Cache trỏ tới: '{redisConnectionString}' (Instance Prefix: '{redisInstanceName}')");
}
else
{
    builder.Services.AddDistributedMemoryCache();
    Console.WriteLine("[Redis Cache] Không tìm thấy cấu hình Redis. Hệ thống tự động fallback sử dụng DistributedMemoryCache (In-Memory).");
}

// 3.1. Multi-Level Caching (L1 In-Memory + L2 Distributed Redis) & Stampede Lock (TSK-803)
builder.Services.AddMemoryCache();
builder.Services.AddScoped<IMultiLevelCacheService, MultiLevelCacheService>();

// 3.2. Health Checks (TSK-808)
builder.Services.AddHealthChecks()
    .AddCheck<SqlDatabaseHealthCheck>("sql_server", tags: new[] { "ready" })
    .AddCheck<RedisDistributedCacheHealthCheck>("redis_cluster", tags: new[] { "ready" })
    .AddCheck<StorageHealthCheck>("onedrive_storage", tags: new[] { "ready" })
    .AddCheck<QdrantVectorHealthCheck>("qdrant_vector_db", tags: new[] { "ready" });

// 4. Authentication & Authorization
builder.Services.AddAuthentication("DevToken")
    .AddScheme<AuthenticationSchemeOptions, DevAuthenticationHandler>("DevToken", null);

builder.Services.AddAuthorization();

// 5. Controllers từ tất cả các Module Presentation
builder.Services.AddControllers()
    .AddApplicationPart(typeof(AuditController).Assembly)
    .AddApplicationPart(typeof(AccessGovernanceController).Assembly)
    .AddApplicationPart(typeof(OrgUnitsController).Assembly)
    .AddApplicationPart(typeof(FormDefinitionsController).Assembly)
    .AddApplicationPart(typeof(WorkflowSubmissionsController).Assembly)
    .AddApplicationPart(typeof(WorkflowAttachmentsController).Assembly)
    .AddApplicationPart(typeof(NotificationTemplatesController).Assembly)
    .AddApplicationPart(typeof(NewsArticlesController).Assembly)
    .AddApplicationPart(typeof(SystemSettingsController).Assembly)
    .AddApplicationPart(typeof(AiAssistantController).Assembly)
    .AddApplicationPart(typeof(AnalyticsDashboardController).Assembly);

// 6. Swagger & OpenAPI Documentation
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Enterprise BPM Platform API - Modular Monolith",
        Version = "v1.0",
        Description = "Nền tảng Quản trị Quy trình Doanh nghiệp Toàn diện (Sprint 08: High-Performance, Resilience & Production Hardening)"
    });

    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "Nhập Header xác thực: 'Bearer {token}' hoặc 'Bearer superadmin'",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

var app = builder.Build();

// 7. Database Migration & Seeding lúc Startup
using (var scope = app.Services.CreateScope())
{
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
    var shouldMigrate = app.Environment.IsDevelopment() || app.Configuration.GetValue<bool>("Database:AutoMigrate", false);

    if (shouldMigrate)
    {
        var authDb = scope.ServiceProvider.GetRequiredService<AuthDbContext>();
        if (authDb.Database.CanConnect())
        {
            await authDb.Database.MigrateAsync();
            var auditDb = scope.ServiceProvider.GetRequiredService<AuditDbContext>();
            await auditDb.Database.MigrateAsync();
            var orgDb = scope.ServiceProvider.GetRequiredService<OrgDbContext>();
            await orgDb.Database.MigrateAsync();
            var formDb = scope.ServiceProvider.GetRequiredService<FormDbContext>();
            await formDb.Database.MigrateAsync();
            var workflowDb = scope.ServiceProvider.GetRequiredService<WorkflowDbContext>();
            await workflowDb.Database.MigrateAsync();
            var documentDb = scope.ServiceProvider.GetRequiredService<DocumentDbContext>();
            await documentDb.Database.MigrateAsync();
            var notifDb = scope.ServiceProvider.GetRequiredService<NotificationDbContext>();
            await notifDb.Database.MigrateAsync();
            var newsDb = scope.ServiceProvider.GetRequiredService<NewsDbContext>();
            await newsDb.Database.MigrateAsync();
            var settingsDb = scope.ServiceProvider.GetRequiredService<SettingsDbContext>();
            await settingsDb.Database.MigrateAsync();
            var aiDb = scope.ServiceProvider.GetRequiredService<AiDbContext>();
            await aiDb.Database.MigrateAsync();
            var analyticsDb = scope.ServiceProvider.GetRequiredService<AnalyticsDbContext>();
            await analyticsDb.Database.MigrateAsync();

            await AuthPermissionSeeder.SeedAsync(authDb, logger);
            await SeedNotificationAndSettingsAsync(notifDb, settingsDb, logger);
            await SeedDocumentTemplatesAsync(documentDb, logger);
            await SeedAiConfigurationsAsync(aiDb, logger);
            await SeedAnalyticsWorkingCalendarAsync(analyticsDb, logger);
            await ProductionDataSeeder.SeedEnterpriseDirectoryAsync(orgDb, logger);
        }
    }
}

// 8. HTTP Request Pipeline
if (app.Environment.IsDevelopment() || app.Configuration.GetValue<bool>("Swagger:Enabled", false))
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Enterprise BPM API v1");
        c.RoutePrefix = "swagger";
    });
}

app.UseDefaultFiles();
app.UseStaticFiles();

// Sprint 08 Middlewares: X-Correlation-ID tracing (TSK-809) & OWASP Security Headers (TSK-810)
app.UseMiddleware<CorrelationIdMiddleware>();
app.UseMiddleware<SecurityHeadersMiddleware>();

app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

// Health Checks Endpoints (TSK-808)
app.MapHealthChecks("/healthz/live", new HealthCheckOptions
{
    Predicate = _ => false
});

app.MapHealthChecks("/healthz/ready", new HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("ready"),
    ResponseWriter = HealthCheckResponseWriter.WriteCustomHealthReportJson
});

app.MapGet("/health", () => Results.Ok(new
{
    Status = "Healthy",
    Version = "1.8.0",
    Timestamp = DateTime.UtcNow,
    Modules = new[] { "SharedKernel", "EventBus", "AuditLog", "AccessGovernance", "OrgManagement", "FormMasterData", "WorkflowEngine", "DocumentManagement", "Notification", "InternalNews", "SystemSettings", "AiAssistant", "Analytics" }
}));

app.Run();

static async Task SeedNotificationAndSettingsAsync(NotificationDbContext notifDb, SettingsDbContext settingsDb, ILogger logger)
{
    if (!await notifDb.NotificationTemplates.AnyAsync())
    {
        notifDb.NotificationTemplates.AddRange(
            new EnterpriseBpm.Notification.Domain.Entities.NotificationTemplate(
                "TPL_TASK_ASSIGNED",
                EnterpriseBpm.Notification.Domain.Enums.NotificationChannel.Email,
                "[Giao việc] Nhiệm vụ duyệt phiếu {{ FormKey }}",
                "<p>Kính gửi,</p><p>Bạn vừa được phân công nhiệm vụ phê duyệt cho phiếu <strong>{{ FormKey }}</strong> (Mã quy trình: {{ WorkflowInstanceId }}).</p><p>Thời hạn xử lý (SLA): <strong>{{ DueDate }}</strong>.</p><p><a href='/#tasks'>Nhấn vào đây để mở phiếu xử lý</a>.</p>"),
            new EnterpriseBpm.Notification.Domain.Entities.NotificationTemplate(
                "TPL_WORKFLOW_APPROVED",
                EnterpriseBpm.Notification.Domain.Enums.NotificationChannel.Email,
                "[Đã duyệt] Phiếu {{ FormKey }} đã được phê duyệt",
                "<p>Kính gửi,</p><p>Phiếu trình ký <strong>{{ FormKey }}</strong> (Mã: {{ WorkflowInstanceId }}) đã được phê duyệt thành công vào lúc {{ ApprovedDate }}.</p>"),
            new EnterpriseBpm.Notification.Domain.Entities.NotificationTemplate(
                "TPL_WORKFLOW_REJECTED",
                EnterpriseBpm.Notification.Domain.Enums.NotificationChannel.Email,
                "[Từ chối] Phiếu {{ FormKey }} bị từ chối",
                "<p>Kính gửi,</p><p>Phiếu trình ký <strong>{{ FormKey }}</strong> đã bị từ chối bởi <strong>{{ RejectedByName }}</strong>.</p><p>Lý do: <em>{{ RejectionReason }}</em></p>"),
            new EnterpriseBpm.Notification.Domain.Entities.NotificationTemplate(
                "TPL_WORKFLOW_CANCELLED",
                EnterpriseBpm.Notification.Domain.Enums.NotificationChannel.Email,
                "[Hủy] Phiếu {{ FormKey }} đã bị hủy",
                "<p>Phiếu trình ký <strong>{{ FormKey }}</strong> đã bị hủy bởi <strong>{{ CancelledByName }}</strong>. Lý do: {{ CancellationReason }}</p>"),
            new EnterpriseBpm.Notification.Domain.Entities.NotificationTemplate(
                "TPL_TASK_SLA_BREACHED",
                EnterpriseBpm.Notification.Domain.Enums.NotificationChannel.Email,
                "[CẢNH BÁO KHẨN] Nhiệm vụ quá hạn SLA - {{ FormKey }}",
                "<p>Nhiệm vụ phê duyệt cho hồ sơ <strong>{{ FormKey }}</strong> đã quá hạn hoàn thành theo SLA (Hạn: {{ DueDate }}). Đề nghị xử lý ngay lập tức.</p>"),
            new EnterpriseBpm.Notification.Domain.Entities.NotificationTemplate(
                "TPL_EMPLOYEE_DEACTIVATED",
                EnterpriseBpm.Notification.Domain.Enums.NotificationChannel.Email,
                "[Thông báo Nhân sự] Vô hiệu hóa tài khoản {{ EmployeeName }}",
                "<p>Nhân sự <strong>{{ EmployeeName }}</strong> (Mã: {{ EmployeeCode }}) đã ngừng hoạt động từ {{ DeactivationDate }}. Các tác vụ liên quan đã được tự động điều chuyển.</p>")
        );
        await notifDb.SaveChangesAsync();
        logger.LogInformation("[Seed] Đã nạp thành công 6 mẫu thông báo NotificationTemplates mặc định.");
    }

    if (!await settingsDb.SystemSettings.AnyAsync())
    {
        settingsDb.SystemSettings.AddRange(
            new EnterpriseBpm.SystemSettings.Domain.Entities.SystemSetting("Brand:AppName", "Enterprise BPM Platform", "Tên hiển thị của hệ thống BPM"),
            new EnterpriseBpm.SystemSettings.Domain.Entities.SystemSetting("Brand:CorporationName", "Tập đoàn Công nghệ & Sản xuất Quốc gia", "Tên tập đoàn/doanh nghiệp sở hữu"),
            new EnterpriseBpm.SystemSettings.Domain.Entities.SystemSetting("Brand:LogoUrl", "https://adminlte.io/themes/v3/dist/img/AdminLTELogo.png", "Đường dẫn Logo thương hiệu"),
            new EnterpriseBpm.SystemSettings.Domain.Entities.SystemSetting("Brand:FaviconUrl", "/favicon.ico", "Đường dẫn Favicon"),
            new EnterpriseBpm.SystemSettings.Domain.Entities.SystemSetting("Brand:PrimaryColor", "#007bff", "Mã màu chủ đạo (Primary Hex Color)"),
            new EnterpriseBpm.SystemSettings.Domain.Entities.SystemSetting("Brand:AccentColor", "#17a2b8", "Mã màu nhấn (Accent Hex Color)"),
            new EnterpriseBpm.SystemSettings.Domain.Entities.SystemSetting("Security:MaxUploadSizeMb", "25", "Dung lượng upload tối đa cho phép (MB)", EnterpriseBpm.SystemSettings.Domain.Enums.SettingDataType.Number),
            new EnterpriseBpm.SystemSettings.Domain.Entities.SystemSetting("Security:SessionTimeoutMinutes", "60", "Thời gian hết hạn phiên đăng nhập (phút)", EnterpriseBpm.SystemSettings.Domain.Enums.SettingDataType.Number)
        );
        await settingsDb.SaveChangesAsync();
        logger.LogInformation("[Seed] Đã nạp thành công bộ cấu hình SystemSettings và White-labeling mặc định.");
    }
}

static async Task SeedDocumentTemplatesAsync(DocumentDbContext documentDb, ILogger logger)
{
    if (!await documentDb.DocumentTemplates.AnyAsync())
    {
        documentDb.DocumentTemplates.AddRange(
            EnterpriseBpm.Document.Domain.Entities.DocumentTemplate.Create(
                templateKey: "tpl_procurement_request",
                title: "Quyết định Phê duyệt Mua sắm Thiết bị & Dịch vụ",
                format: EnterpriseBpm.Document.Domain.Enums.TemplateFormat.Pdf,
                oneDriveTemplateItemId: "item-template-procurement-001",
                placeholderSchema: "{\"FormKey\":\"string\",\"WorkflowInstanceId\":\"string\",\"ApprovedByUserId\":\"string\",\"ApprovedAt\":\"string\"}",
                description: "Mẫu quyết định phê duyệt mua sắm tự động sinh khi hồ sơ đạt trạng thái Approved."
            ),
            EnterpriseBpm.Document.Domain.Entities.DocumentTemplate.Create(
                templateKey: "tpl_default",
                title: "Biên Bản Nghiệm Thu & Phê Duyệt Hồ Sơ Quy Trình",
                format: EnterpriseBpm.Document.Domain.Enums.TemplateFormat.Pdf,
                oneDriveTemplateItemId: "item-template-default-002",
                placeholderSchema: "{\"FormKey\":\"string\",\"WorkflowInstanceId\":\"string\",\"ApprovedAt\":\"string\"}",
                description: "Mẫu văn bản quyết định phê duyệt mặc định cho toàn bộ quy trình doanh nghiệp."
            )
        );
        await documentDb.SaveChangesAsync();
        logger.LogInformation("[Seed] Đã nạp thành công 2 mẫu DocumentTemplates mặc định (tpl_procurement_request, tpl_default).");
    }
}

static async Task SeedAiConfigurationsAsync(AiDbContext aiDb, ILogger logger)
{
    if (!await aiDb.AiProviderConfigurations.AnyAsync())
    {
        aiDb.AiProviderConfigurations.AddRange(
            EnterpriseBpm.AiAssistant.Domain.Entities.AiProviderConfiguration.Create(
                EnterpriseBpm.AiAssistant.Domain.Enums.AiProviderType.Gemini,
                "Google Gemini",
                "gemini-1.5-flash",
                "https://generativelanguage.googleapis.com",
                null,
                0.1,
                2048,
                isDefault: true
            ),
            EnterpriseBpm.AiAssistant.Domain.Entities.AiProviderConfiguration.Create(
                EnterpriseBpm.AiAssistant.Domain.Enums.AiProviderType.OpenAi,
                "OpenAI (ChatGPT)",
                "gpt-4o-mini",
                "https://api.openai.com/v1",
                null,
                0.1,
                2048,
                isDefault: false
            ),
            EnterpriseBpm.AiAssistant.Domain.Entities.AiProviderConfiguration.Create(
                EnterpriseBpm.AiAssistant.Domain.Enums.AiProviderType.Claude,
                "Anthropic Claude",
                "claude-3-5-haiku-20241022",
                "https://api.anthropic.com/v1",
                null,
                0.1,
                2048,
                isDefault: false
            ),
            EnterpriseBpm.AiAssistant.Domain.Entities.AiProviderConfiguration.Create(
                EnterpriseBpm.AiAssistant.Domain.Enums.AiProviderType.Ollama,
                "Local LLM / MCP (Ollama)",
                "llama3:latest",
                "http://localhost:11434",
                null,
                0.1,
                2048,
                isDefault: false
            )
        );
        await aiDb.SaveChangesAsync();
        logger.LogInformation("[Seed] Đã nạp thành công 4 cấu hình AI Provider mặc định (Gemini, OpenAI, Claude, Ollama/MCP).");
    }

    if (!await aiDb.KnowledgeDocuments.AnyAsync())
    {
        aiDb.KnowledgeDocuments.AddRange(
            EnterpriseBpm.AiAssistant.Domain.Entities.KnowledgeDocument.Create(
                "Quy chế Chi tiêu & Mua sắm Nội bộ 2026",
                EnterpriseBpm.AiAssistant.Domain.Enums.KnowledgeDocumentType.Policy,
                "App_Data/OneDriveStorage/QuyCheChiTieu2026.pdf",
                "company_policies",
                "nomic-embed-text"
            ),
            EnterpriseBpm.AiAssistant.Domain.Entities.KnowledgeDocument.Create(
                "Nội quy Lao động & Chế độ Công tác phí",
                EnterpriseBpm.AiAssistant.Domain.Enums.KnowledgeDocumentType.Regulation,
                "App_Data/OneDriveStorage/NoiQuyCongTacPhi.pdf",
                "company_policies",
                "nomic-embed-text"
            )
        );
        await aiDb.SaveChangesAsync();
        logger.LogInformation("[Seed] Đã nạp thành công 2 tài liệu tri thức quy chế nội bộ mặc định vào Knowledge Base.");
    }
}

static async Task SeedAnalyticsWorkingCalendarAsync(AnalyticsDbContext analyticsDb, ILogger logger)
{
    if (!await analyticsDb.WorkingCalendars.AnyAsync())
    {
        var defaultCalendar = WorkingCalendar.CreateDefaultVietnamCalendar();
        analyticsDb.WorkingCalendars.Add(defaultCalendar);
        await analyticsDb.SaveChangesAsync();
        logger.LogInformation("[Seed] Đã nạp thành công Lịch làm việc tiêu chuẩn Việt Nam (CAL-VN-STANDARD).");
    }
}

// Handler xác thực DevToken để phục vụ Swagger / Postman thử nghiệm
public class DevAuthenticationHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    public DevAuthenticationHandler(
        Microsoft.Extensions.Options.IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        System.Text.Encodings.Web.UrlEncoder encoder) : base(options, logger, encoder)
    {
    }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var authHeader = Request.Headers.Authorization.FirstOrDefault();
        if (string.IsNullOrWhiteSpace(authHeader))
        {
            return Task.FromResult(AuthenticateResult.NoResult());
        }

        var userId = Guid.Parse("20000000-0000-0000-0000-000000000001");
        var claims = new[]
        {
            new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.NameIdentifier, userId.ToString()),
            new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.Name, "superadmin"),
            new System.Security.Claims.Claim("sub", userId.ToString()),
            new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.Role, "SUPER_ADMIN")
        };

        var identity = new System.Security.Claims.ClaimsIdentity(claims, Scheme.Name);
        var principal = new System.Security.Claims.ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, Scheme.Name);

        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}
