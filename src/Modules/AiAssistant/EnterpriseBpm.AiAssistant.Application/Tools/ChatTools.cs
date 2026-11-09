using System.Text.Json;
using EnterpriseBpm.AiAssistant.Application.Contracts;
using EnterpriseBpm.AiAssistant.Application.DTOs;
using EnterpriseBpm.SharedKernel.Contracts;
using EnterpriseBpm.Workflow.Application.Contracts;
using EnterpriseBpm.Workflow.Domain.Enums;
using Microsoft.Extensions.Logging;

namespace EnterpriseBpm.AiAssistant.Application.Tools;

/// <summary>
/// Tool tra cứu quy chế nội bộ (RAG) qua Qdrant Vector Database (TSK-611)
/// </summary>
public class PolicyRagTool : IChatTool
{
    private readonly IQdrantVectorService _qdrantService;
    private readonly IEmbeddingGeneratorService _embeddingService;
    private readonly ILogger<PolicyRagTool> _logger;

    public string Name => "search_company_policy";
    public string Description => "Tìm kiếm quy chế, quy trình, chính sách nội bộ của công ty/tập đoàn với trích dẫn số trang và nội dung điều khoản.";

    public ToolDefinitionDto Definition => new(
        Name: Name,
        Description: Description,
        ParametersSchema: new Dictionary<string, object>
        {
            ["type"] = "object",
            ["properties"] = new Dictionary<string, object>
            {
                ["query"] = new Dictionary<string, string>
                {
                    ["type"] = "string",
                    ["description"] = "Nội dung câu hỏi hoặc từ khóa quy chế cần tra cứu (ví dụ: hạn mức công tác phí, quy trình mua sắm, ngày nghỉ phép)"
                },
                ["documentType"] = new Dictionary<string, string>
                {
                    ["type"] = "string",
                    ["description"] = "Loại tài liệu cần lọc nếu có (Policy, Procedure, Circular, Regulation)"
                }
            },
            ["required"] = new[] { "query" }
        }
    );

    public PolicyRagTool(
        IQdrantVectorService qdrantService,
        IEmbeddingGeneratorService embeddingService,
        ILogger<PolicyRagTool> logger)
    {
        _qdrantService = qdrantService;
        _embeddingService = embeddingService;
        _logger = logger;
    }

    public async Task<string> ExecuteAsync(string argumentsJson, CancellationToken cancellationToken = default)
    {
        try
        {
            using var doc = JsonDocument.Parse(string.IsNullOrWhiteSpace(argumentsJson) ? "{}" : argumentsJson);
            var query = doc.RootElement.TryGetProperty("query", out var qProp) ? qProp.GetString() : null;

            if (string.IsNullOrWhiteSpace(query))
            {
                return JsonSerializer.Serialize(new { error = "Tham số 'query' không được để trống." });
            }

            _logger.LogInformation("PolicyRagTool: Đang tìm kiếm ngữ nghĩa cho câu hỏi: '{Query}'", query);

            // 1. Sinh vector nhúng cho câu hỏi
            var queryVector = await _embeddingService.GenerateEmbeddingAsync(query, null, cancellationToken);

            // 2. Tra cứu vector trong Qdrant với ngưỡng tương đồng Cosine >= 0.7 (Hallucination Guard)
            var scoredPoints = await _qdrantService.SearchAsync(
                collectionName: "company_policies",
                queryVector: queryVector,
                topK: 4,
                scoreThreshold: 0.7f,
                cancellationToken: cancellationToken);

            if (scoredPoints.Count == 0)
            {
                return JsonSerializer.Serialize(new
                {
                    found = false,
                    message = "Không tìm thấy nội dung quy chế nào phù hợp trong kho tài liệu nội bộ với ngưỡng tương đồng tin cậy (Cosine Similarity < 0.7)."
                });
            }

            var chunks = scoredPoints.Select(p => new
            {
                documentTitle = p.Payload.TryGetValue("documentTitle", out var dt) ? dt.ToString() : "Quy chế nội bộ",
                page = p.Payload.TryGetValue("page", out var pg) ? pg.ToString() : "1",
                section = p.Payload.TryGetValue("section", out var sec) ? sec.ToString() : string.Empty,
                text = p.Payload.TryGetValue("text", out var txt) ? txt.ToString() : string.Empty,
                score = Math.Round(p.Score, 3)
            }).ToList();

            return JsonSerializer.Serialize(new
            {
                found = true,
                count = chunks.Count,
                results = chunks
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi thực thi PolicyRagTool");
            return JsonSerializer.Serialize(new { error = $"Lỗi tra cứu tài liệu: {ex.Message}" });
        }
    }
}

/// <summary>
/// Tool tra cứu nhiệm vụ phê duyệt cá nhân Scoped RBAC/ABAC (TSK-612)
/// </summary>
public class MyPendingTasksTool : IChatTool
{
    private readonly IApprovalTaskService _taskService;
    private readonly ICurrentUserService _currentUserService;
    private readonly ILogger<MyPendingTasksTool> _logger;

    public string Name => "get_my_pending_tasks";
    public string Description => "Tra cứu danh sách các nhiệm vụ, hồ sơ trình ký đang chờ phê duyệt của chính người dùng hiện tại (bảo đảm an toàn phân quyền RBAC/ABAC).";

    public ToolDefinitionDto Definition => new(
        Name: Name,
        Description: Description,
        ParametersSchema: new Dictionary<string, object>
        {
            ["type"] = "object",
            ["properties"] = new Dictionary<string, object>
            {
                ["status"] = new Dictionary<string, string>
                {
                    ["type"] = "string",
                    ["description"] = "Trạng thái nhiệm vụ (Pending, Completed, Rejected)"
                }
            }
        }
    );

    public MyPendingTasksTool(
        IApprovalTaskService taskService,
        ICurrentUserService currentUserService,
        ILogger<MyPendingTasksTool> logger)
    {
        _taskService = taskService;
        _currentUserService = currentUserService;
        _logger = logger;
    }

    public async Task<string> ExecuteAsync(string argumentsJson, CancellationToken cancellationToken = default)
    {
        var currentUserId = _currentUserService.UserId;
        if (!currentUserId.HasValue)
        {
            return JsonSerializer.Serialize(new { error = "Không xác định được danh tính người dùng trong phiên làm việc để tra cứu nhiệm vụ." });
        }

        try
        {
            // Truy vấn task pending chỉ thuộc về người dùng đang đăng nhập
            var pagedTasks = await _taskService.GetTasksForUserAsync(
                userId: currentUserId.Value,
                filter: new EnterpriseBpm.Workflow.Application.DTOs.PendingTaskFilterDto(Status: "Pending", Page: 1, PageSize: 20),
                cancellationToken: cancellationToken);

            var tasks = pagedTasks.Items.Select(t => new
            {
                taskId = t.Id,
                workflowInstanceId = t.WorkflowInstanceId,
                activityName = t.ActivityId,
                dueDate = t.DueDate?.ToString("dd/MM/yyyy HH:mm"),
                isEscalated = t.IsEscalated,
                status = t.Status.ToString(),
                comment = t.Comment
            }).ToList();

            return JsonSerializer.Serialize(new
            {
                currentUserId = currentUserId.Value,
                totalPendingTasks = pagedTasks.TotalCount,
                tasks = tasks
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi lấy danh sách pending tasks cho user {UserId}", currentUserId);
            return JsonSerializer.Serialize(new { error = $"Lỗi tra cứu tác vụ: {ex.Message}" });
        }
    }
}

/// <summary>
/// Tool tra cứu thống kê phê duyệt theo đơn vị Scoped RBAC/ABAC (TSK-612)
/// </summary>
public class WorkflowStatsTool : IChatTool
{
    private readonly IApprovalTaskService _taskService;
    private readonly ICurrentUserService _currentUserService;
    private readonly ILogger<WorkflowStatsTool> _logger;

    public string Name => "get_approval_stats";
    public string Description => "Lấy thống kê số lượng hồ sơ phê duyệt trong phạm vi đơn vị/phòng ban của người dùng.";

    public ToolDefinitionDto Definition => new(
        Name: Name,
        Description: Description,
        ParametersSchema: new Dictionary<string, object>
        {
            ["type"] = "object",
            ["properties"] = new Dictionary<string, object>()
        }
    );

    public WorkflowStatsTool(
        IApprovalTaskService taskService,
        ICurrentUserService currentUserService,
        ILogger<WorkflowStatsTool> logger)
    {
        _taskService = taskService;
        _currentUserService = currentUserService;
        _logger = logger;
    }

    public async Task<string> ExecuteAsync(string argumentsJson, CancellationToken cancellationToken = default)
    {
        var currentUserId = _currentUserService.UserId;
        if (!currentUserId.HasValue)
        {
            return JsonSerializer.Serialize(new { error = "Yêu cầu đăng nhập để xem số liệu thống kê." });
        }

        try
        {
            var pendingTasks = await _taskService.GetTasksForUserAsync(
                userId: currentUserId.Value,
                filter: new EnterpriseBpm.Workflow.Application.DTOs.PendingTaskFilterDto(Status: "Pending", Page: 1, PageSize: 50),
                cancellationToken: cancellationToken);

            var overdueCount = pendingTasks.Items.Count(t => t.IsEscalated || (t.DueDate.HasValue && t.DueDate.Value < DateTime.UtcNow));

            return JsonSerializer.Serialize(new
            {
                userId = currentUserId.Value,
                pendingCount = pendingTasks.TotalCount,
                overdueCount = overdueCount,
                normalPendingCount = Math.Max(0, pendingTasks.TotalCount - overdueCount)
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi thống kê số liệu cho user {UserId}", currentUserId);
            return JsonSerializer.Serialize(new { error = $"Lỗi thống kê: {ex.Message}" });
        }
    }
}

/// <summary>
/// Bộ đăng ký và quản lý công cụ C# tập trung (Unified Tool Registry - TSK-607)
/// </summary>
public class UnifiedToolRegistry : IUnifiedToolRegistry
{
    private readonly Dictionary<string, IChatTool> _tools = new(StringComparer.OrdinalIgnoreCase);

    public UnifiedToolRegistry(IEnumerable<IChatTool> tools)
    {
        foreach (var t in tools)
        {
            RegisterTool(t);
        }
    }

    public void RegisterTool(IChatTool tool)
    {
        _tools[tool.Name] = tool;
    }

    public IReadOnlyCollection<ToolDefinitionDto> GetToolDefinitions()
    {
        return _tools.Values.Select(t => t.Definition).ToList();
    }

    public IChatTool? GetTool(string toolName)
    {
        return _tools.TryGetValue(toolName, out var tool) ? tool : null;
    }

    public async Task<string> ExecuteToolAsync(string toolName, string argumentsJson, CancellationToken cancellationToken = default)
    {
        if (!_tools.TryGetValue(toolName, out var tool))
        {
            return JsonSerializer.Serialize(new { error = $"Không tìm thấy công cụ: {toolName}" });
        }

        return await tool.ExecuteAsync(argumentsJson, cancellationToken);
    }
}
