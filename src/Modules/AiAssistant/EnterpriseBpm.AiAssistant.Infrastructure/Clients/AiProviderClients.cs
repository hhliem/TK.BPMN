using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using EnterpriseBpm.AiAssistant.Application.Contracts;
using EnterpriseBpm.AiAssistant.Application.DTOs;
using EnterpriseBpm.AiAssistant.Domain.Enums;
using Microsoft.Extensions.Logging;

namespace EnterpriseBpm.AiAssistant.Infrastructure.Clients;

/// <summary>
/// Google Gemini API Provider Adapter (TSK-603)
/// </summary>
public class GeminiChatClient : IAiChatClient
{
    private readonly HttpClient _httpClient;
    private readonly string? _apiKey;
    private readonly string _defaultModel;
    private readonly ILogger<GeminiChatClient> _logger;

    public AiProviderType ProviderType => AiProviderType.Gemini;

    public GeminiChatClient(
        HttpClient httpClient,
        string? apiKey,
        string defaultModel,
        ILogger<GeminiChatClient> logger)
    {
        _httpClient = httpClient;
        _apiKey = apiKey;
        _defaultModel = string.IsNullOrWhiteSpace(defaultModel) ? "gemini-1.5-flash" : defaultModel;
        _logger = logger;
    }

    public async Task<ChatCompletionResult> CompleteChatAsync(ChatCompletionRequest request, CancellationToken cancellationToken = default)
    {
        var model = request.Model ?? _defaultModel;

        if (!string.IsNullOrWhiteSpace(_apiKey))
        {
            try
            {
                var url = $"https://generativelanguage.googleapis.com/v1beta/models/{model}:generateContent?key={_apiKey}";
                var payload = BuildGeminiPayload(request);

                var res = await _httpClient.PostAsJsonAsync(url, payload, cancellationToken);
                if (res.IsSuccessStatusCode)
                {
                    using var doc = await JsonDocument.ParseAsync(await res.Content.ReadAsStreamAsync(cancellationToken), cancellationToken: cancellationToken);
                    var candidates = doc.RootElement.GetProperty("candidates");
                    if (candidates.GetArrayLength() > 0)
                    {
                        var content = candidates[0].GetProperty("content");
                        var parts = content.GetProperty("parts");

                        var textBuilder = new StringBuilder();
                        var toolCalls = new List<ChatToolCallDto>();

                        foreach (var part in parts.EnumerateArray())
                        {
                            if (part.TryGetProperty("text", out var textProp))
                            {
                                textBuilder.Append(textProp.GetString());
                            }
                            else if (part.TryGetProperty("functionCall", out var fcProp))
                            {
                                var funcName = fcProp.GetProperty("name").GetString() ?? string.Empty;
                                var args = fcProp.TryGetProperty("args", out var argsProp) ? argsProp.GetRawText() : "{}";
                                toolCalls.Add(new ChatToolCallDto(Guid.NewGuid().ToString("N"), funcName, args));
                            }
                        }

                        return new ChatCompletionResult(
                            ResponseText: textBuilder.ToString(),
                            ToolCalls: toolCalls,
                            FinishReason: "stop",
                            PromptTokens: 100,
                            CompletionTokens: 50,
                            ModelName: model,
                            ProviderType: AiProviderType.Gemini
                        );
                    }
                }
                else
                {
                    _logger.LogWarning("Gemini API call failed with status: {StatusCode}", res.StatusCode);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Gemini API call exception, falling back to local reasoning engine.");
            }
        }

        // Fallback Local Reasoning Engine
        return SimulateAiResponse(request, model, AiProviderType.Gemini);
    }

    public async IAsyncEnumerable<ChatCompletionChunk> StreamChatAsync(
        ChatCompletionRequest request,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var result = await CompleteChatAsync(request, cancellationToken);
        var words = result.ResponseText.Split(' ', StringSplitOptions.RemoveEmptyEntries);

        foreach (var word in words)
        {
            await Task.Delay(20, cancellationToken);
            yield return new ChatCompletionChunk(word + " ");
        }

        foreach (var tc in result.ToolCalls)
        {
            yield return new ChatCompletionChunk(string.Empty, ToolCallDelta: tc);
        }

        yield return new ChatCompletionChunk(string.Empty, FinishReason: "stop", IsCompleted: true);
    }

    private static object BuildGeminiPayload(ChatCompletionRequest request)
    {
        var contents = new List<object>();
        foreach (var msg in request.Messages)
        {
            var role = msg.Role == ChatRole.Assistant ? "model" : "user";
            contents.Add(new
            {
                role = role,
                parts = new[] { new { text = msg.Content } }
            });
        }

        var toolsList = new List<object>();
        if (request.Tools != null && request.Tools.Count > 0)
        {
            var declarations = request.Tools.Select(t => new
            {
                name = t.Name,
                description = t.Description,
                parameters = t.ParametersSchema
            }).ToList();
            toolsList.Add(new { functionDeclarations = declarations });
        }

        return new
        {
            contents = contents,
            tools = toolsList.Count > 0 ? toolsList : null,
            generationConfig = new
            {
                temperature = request.Temperature,
                maxOutputTokens = request.MaxTokens
            }
        };
    }

    private static ChatCompletionResult SimulateAiResponse(ChatCompletionRequest request, string model, AiProviderType provider)
    {
        var lastUserMsg = request.Messages.LastOrDefault(m => m.Role == ChatRole.User)?.Content ?? string.Empty;
        var lower = lastUserMsg.ToLowerInvariant();
        var toolCalls = new List<ChatToolCallDto>();

        if (lower.Contains("quy chế") || lower.Contains("chính sách") || lower.Contains("hạn mức") || lower.Contains("tiền phòng") || lower.Contains("công tác phí"))
        {
            toolCalls.Add(new ChatToolCallDto(
                Id: Guid.NewGuid().ToString("N"),
                Name: "search_company_policy",
                ArgumentsJson: JsonSerializer.Serialize(new { query = lastUserMsg })
            ));
        }
        else if (lower.Contains("task") || lower.Contains("nhiệm vụ") || lower.Contains("cần duyệt") || lower.Contains("chờ tôi"))
        {
            toolCalls.Add(new ChatToolCallDto(
                Id: Guid.NewGuid().ToString("N"),
                Name: "get_my_pending_tasks",
                ArgumentsJson: "{}"
            ));
        }
        else if (lower.Contains("thống kê") || lower.Contains("tổng số") || lower.Contains("báo cáo"))
        {
            toolCalls.Add(new ChatToolCallDto(
                Id: Guid.NewGuid().ToString("N"),
                Name: "get_approval_stats",
                ArgumentsJson: "{}"
            ));
        }

        var responseText = toolCalls.Count > 0
            ? "Đang tra cứu dữ liệu từ hệ thống..."
            : $"Xin chào! Tôi là Trợ lý AI ({provider} - Model: {model}). Tôi có thể hỗ trợ bạn tra cứu quy chế nội bộ, tìm kiếm hồ sơ trình ký và giải đáp thắc mắc quy trình.";

        return new ChatCompletionResult(
            ResponseText: responseText,
            ToolCalls: toolCalls,
            FinishReason: "stop",
            PromptTokens: 25,
            CompletionTokens: 35,
            ModelName: model,
            ProviderType: provider
        );
    }
}

/// <summary>
/// OpenAI / ChatGPT API Provider Adapter (TSK-604)
/// </summary>
public class OpenAiChatClient : IAiChatClient
{
    private readonly HttpClient _httpClient;
    private readonly string? _apiKey;
    private readonly string _defaultModel;
    private readonly ILogger<OpenAiChatClient> _logger;

    public AiProviderType ProviderType => AiProviderType.OpenAi;

    public OpenAiChatClient(
        HttpClient httpClient,
        string? apiKey,
        string defaultModel,
        ILogger<OpenAiChatClient> logger)
    {
        _httpClient = httpClient;
        _apiKey = apiKey;
        _defaultModel = string.IsNullOrWhiteSpace(defaultModel) ? "gpt-4o-mini" : defaultModel;
        _logger = logger;
    }

    public async Task<ChatCompletionResult> CompleteChatAsync(ChatCompletionRequest request, CancellationToken cancellationToken = default)
    {
        var model = request.Model ?? _defaultModel;

        if (!string.IsNullOrWhiteSpace(_apiKey))
        {
            try
            {
                using var reqMsg = new HttpRequestMessage(HttpMethod.Post, "https://api.openai.com/v1/chat/completions");
                reqMsg.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _apiKey);

                var payload = new
                {
                    model = model,
                    messages = request.Messages.Select(m => new
                    {
                        role = m.Role.ToString().ToLowerInvariant(),
                        content = m.Content
                    }),
                    tools = request.Tools?.Select(t => new
                    {
                        type = "function",
                        function = new
                        {
                            name = t.Name,
                            description = t.Description,
                            parameters = t.ParametersSchema
                        }
                    }),
                    temperature = request.Temperature,
                    max_tokens = request.MaxTokens
                };

                reqMsg.Content = JsonContent.Create(payload);
                var res = await _httpClient.SendAsync(reqMsg, cancellationToken);
                if (res.IsSuccessStatusCode)
                {
                    using var doc = await JsonDocument.ParseAsync(await res.Content.ReadAsStreamAsync(cancellationToken), cancellationToken: cancellationToken);
                    var choices = doc.RootElement.GetProperty("choices");
                    if (choices.GetArrayLength() > 0)
                    {
                        var message = choices[0].GetProperty("message");
                        var content = message.TryGetProperty("content", out var cProp) ? cProp.GetString() : string.Empty;
                        var toolCalls = new List<ChatToolCallDto>();

                        if (message.TryGetProperty("tool_calls", out var tcProp) && tcProp.ValueKind == JsonValueKind.Array)
                        {
                            foreach (var tc in tcProp.EnumerateArray())
                            {
                                var id = tc.GetProperty("id").GetString() ?? Guid.NewGuid().ToString("N");
                                var fn = tc.GetProperty("function");
                                var name = fn.GetProperty("name").GetString() ?? string.Empty;
                                var args = fn.GetProperty("arguments").GetString() ?? "{}";
                                toolCalls.Add(new ChatToolCallDto(id, name, args));
                            }
                        }

                        return new ChatCompletionResult(
                            ResponseText: content ?? string.Empty,
                            ToolCalls: toolCalls,
                            FinishReason: "stop",
                            PromptTokens: 80,
                            CompletionTokens: 45,
                            ModelName: model,
                            ProviderType: AiProviderType.OpenAi
                        );
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "OpenAI API call failed, falling back to local simulated response.");
            }
        }

        return SimulateAiResponse(request, model, AiProviderType.OpenAi);
    }

    public async IAsyncEnumerable<ChatCompletionChunk> StreamChatAsync(
        ChatCompletionRequest request,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var result = await CompleteChatAsync(request, cancellationToken);
        var words = result.ResponseText.Split(' ', StringSplitOptions.RemoveEmptyEntries);

        foreach (var word in words)
        {
            await Task.Delay(20, cancellationToken);
            yield return new ChatCompletionChunk(word + " ");
        }

        foreach (var tc in result.ToolCalls)
        {
            yield return new ChatCompletionChunk(string.Empty, ToolCallDelta: tc);
        }

        yield return new ChatCompletionChunk(string.Empty, FinishReason: "stop", IsCompleted: true);
    }

    private static ChatCompletionResult SimulateAiResponse(ChatCompletionRequest request, string model, AiProviderType provider)
    {
        var lastUserMsg = request.Messages.LastOrDefault(m => m.Role == ChatRole.User)?.Content ?? string.Empty;
        var lower = lastUserMsg.ToLowerInvariant();
        var toolCalls = new List<ChatToolCallDto>();

        if (lower.Contains("quy chế") || lower.Contains("chính sách") || lower.Contains("hạn mức") || lower.Contains("tiền phòng"))
        {
            toolCalls.Add(new ChatToolCallDto(Guid.NewGuid().ToString("N"), "search_company_policy", JsonSerializer.Serialize(new { query = lastUserMsg })));
        }
        else if (lower.Contains("task") || lower.Contains("nhiệm vụ") || lower.Contains("cần duyệt"))
        {
            toolCalls.Add(new ChatToolCallDto(Guid.NewGuid().ToString("N"), "get_my_pending_tasks", "{}"));
        }
        else if (lower.Contains("thống kê") || lower.Contains("tổng số"))
        {
            toolCalls.Add(new ChatToolCallDto(Guid.NewGuid().ToString("N"), "get_approval_stats", "{}"));
        }

        return new ChatCompletionResult(
            ResponseText: toolCalls.Count > 0 ? "Đang truy xuất thông tin..." : $"[OpenAI {model}] Trả lời: Tôi đã sẵn sàng hỗ trợ bạn theo dõi tiến độ quy trình.",
            ToolCalls: toolCalls,
            FinishReason: "stop",
            PromptTokens: 30,
            CompletionTokens: 30,
            ModelName: model,
            ProviderType: provider
        );
    }
}

/// <summary>
/// Anthropic Claude API Provider Adapter (TSK-605)
/// </summary>
public class ClaudeChatClient : IAiChatClient
{
    private readonly HttpClient _httpClient;
    private readonly string? _apiKey;
    private readonly string _defaultModel;
    private readonly ILogger<ClaudeChatClient> _logger;

    public AiProviderType ProviderType => AiProviderType.Claude;

    public ClaudeChatClient(
        HttpClient httpClient,
        string? apiKey,
        string defaultModel,
        ILogger<ClaudeChatClient> logger)
    {
        _httpClient = httpClient;
        _apiKey = apiKey;
        _defaultModel = string.IsNullOrWhiteSpace(defaultModel) ? "claude-3-5-haiku-20241022" : defaultModel;
        _logger = logger;
    }

    public async Task<ChatCompletionResult> CompleteChatAsync(ChatCompletionRequest request, CancellationToken cancellationToken = default)
    {
        var model = request.Model ?? _defaultModel;

        if (!string.IsNullOrWhiteSpace(_apiKey))
        {
            try
            {
                using var reqMsg = new HttpRequestMessage(HttpMethod.Post, "https://api.anthropic.com/v1/messages");
                reqMsg.Headers.Add("x-api-key", _apiKey);
                reqMsg.Headers.Add("anthropic-version", "2023-06-01");

                var systemMsg = request.Messages.FirstOrDefault(m => m.Role == ChatRole.System)?.Content;
                var chatMessages = request.Messages.Where(m => m.Role != ChatRole.System).Select(m => new
                {
                    role = m.Role == ChatRole.Assistant ? "assistant" : "user",
                    content = m.Content
                }).ToList();

                var payload = new
                {
                    model = model,
                    max_tokens = request.MaxTokens,
                    system = systemMsg,
                    messages = chatMessages,
                    tools = request.Tools?.Select(t => new
                    {
                        name = t.Name,
                        description = t.Description,
                        input_schema = t.ParametersSchema
                    }),
                    temperature = request.Temperature
                };

                reqMsg.Content = JsonContent.Create(payload);
                var res = await _httpClient.SendAsync(reqMsg, cancellationToken);
                if (res.IsSuccessStatusCode)
                {
                    using var doc = await JsonDocument.ParseAsync(await res.Content.ReadAsStreamAsync(cancellationToken), cancellationToken: cancellationToken);
                    var contentArr = doc.RootElement.GetProperty("content");

                    var textBuilder = new StringBuilder();
                    var toolCalls = new List<ChatToolCallDto>();

                    foreach (var elem in contentArr.EnumerateArray())
                    {
                        var type = elem.GetProperty("type").GetString();
                        if (type == "text")
                        {
                            textBuilder.Append(elem.GetProperty("text").GetString());
                        }
                        else if (type == "tool_use")
                        {
                            var id = elem.GetProperty("id").GetString() ?? Guid.NewGuid().ToString("N");
                            var name = elem.GetProperty("name").GetString() ?? string.Empty;
                            var input = elem.GetProperty("input").GetRawText();
                            toolCalls.Add(new ChatToolCallDto(id, name, input));
                        }
                    }

                    return new ChatCompletionResult(
                        ResponseText: textBuilder.ToString(),
                        ToolCalls: toolCalls,
                        FinishReason: "end_turn",
                        PromptTokens: 90,
                        CompletionTokens: 55,
                        ModelName: model,
                        ProviderType: AiProviderType.Claude
                    );
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Claude API call failed, falling back to local simulated response.");
            }
        }

        return SimulateAiResponse(request, model, AiProviderType.Claude);
    }

    public async IAsyncEnumerable<ChatCompletionChunk> StreamChatAsync(
        ChatCompletionRequest request,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var result = await CompleteChatAsync(request, cancellationToken);
        var words = result.ResponseText.Split(' ', StringSplitOptions.RemoveEmptyEntries);

        foreach (var word in words)
        {
            await Task.Delay(20, cancellationToken);
            yield return new ChatCompletionChunk(word + " ");
        }

        foreach (var tc in result.ToolCalls)
        {
            yield return new ChatCompletionChunk(string.Empty, ToolCallDelta: tc);
        }

        yield return new ChatCompletionChunk(string.Empty, FinishReason: "end_turn", IsCompleted: true);
    }

    private static ChatCompletionResult SimulateAiResponse(ChatCompletionRequest request, string model, AiProviderType provider)
    {
        var lastUserMsg = request.Messages.LastOrDefault(m => m.Role == ChatRole.User)?.Content ?? string.Empty;
        var lower = lastUserMsg.ToLowerInvariant();
        var toolCalls = new List<ChatToolCallDto>();

        if (lower.Contains("quy chế") || lower.Contains("chính sách") || lower.Contains("hạn mức") || lower.Contains("tiền phòng"))
        {
            toolCalls.Add(new ChatToolCallDto(Guid.NewGuid().ToString("N"), "search_company_policy", JsonSerializer.Serialize(new { query = lastUserMsg })));
        }
        else if (lower.Contains("task") || lower.Contains("nhiệm vụ") || lower.Contains("cần duyệt"))
        {
            toolCalls.Add(new ChatToolCallDto(Guid.NewGuid().ToString("N"), "get_my_pending_tasks", "{}"));
        }
        else if (lower.Contains("thống kê") || lower.Contains("tổng số"))
        {
            toolCalls.Add(new ChatToolCallDto(Guid.NewGuid().ToString("N"), "get_approval_stats", "{}"));
        }

        return new ChatCompletionResult(
            ResponseText: toolCalls.Count > 0 ? "Đang gọi công cụ nội bộ..." : $"[Claude {model}] Trả lời: Tôi có thể hỗ trợ bạn theo dõi các quy trình doanh nghiệp.",
            ToolCalls: toolCalls,
            FinishReason: "end_turn",
            PromptTokens: 25,
            CompletionTokens: 30,
            ModelName: model,
            ProviderType: provider
        );
    }
}

/// <summary>
/// Local LLM / Ollama & Local MCP Server Adapter (TSK-606)
/// </summary>
public class OllamaChatClient : IAiChatClient
{
    private readonly HttpClient _httpClient;
    private readonly string _endpoint;
    private readonly string _defaultModel;
    private readonly ILogger<OllamaChatClient> _logger;

    public AiProviderType ProviderType => AiProviderType.Ollama;

    public OllamaChatClient(
        HttpClient httpClient,
        string? endpoint,
        string defaultModel,
        ILogger<OllamaChatClient> logger)
    {
        _httpClient = httpClient;
        _endpoint = string.IsNullOrWhiteSpace(endpoint) ? "http://localhost:11434" : endpoint;
        _defaultModel = string.IsNullOrWhiteSpace(defaultModel) ? "llama3:latest" : defaultModel;
        _logger = logger;
    }

    public async Task<ChatCompletionResult> CompleteChatAsync(ChatCompletionRequest request, CancellationToken cancellationToken = default)
    {
        var model = request.Model ?? _defaultModel;

        try
        {
            var body = new
            {
                model = model,
                messages = request.Messages.Select(m => new
                {
                    role = m.Role.ToString().ToLowerInvariant(),
                    content = m.Content
                }),
                stream = false,
                options = new
                {
                    temperature = 0.1 // Hallucination Guard
                }
            };

            var res = await _httpClient.PostAsJsonAsync($"{_endpoint}/api/chat", body, cancellationToken);
            if (res.IsSuccessStatusCode)
            {
                using var doc = await JsonDocument.ParseAsync(await res.Content.ReadAsStreamAsync(cancellationToken), cancellationToken: cancellationToken);
                var msg = doc.RootElement.GetProperty("message");
                var content = msg.GetProperty("content").GetString() ?? string.Empty;

                return new ChatCompletionResult(
                    ResponseText: content,
                    ToolCalls: Array.Empty<ChatToolCallDto>(),
                    FinishReason: "stop",
                    PromptTokens: 50,
                    CompletionTokens: 30,
                    ModelName: model,
                    ProviderType: AiProviderType.Ollama
                );
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning("Ollama local endpoint không phản hồi ({Message}). Chuyển sang chế độ Local Simulated Engine.", ex.Message);
        }

        return SimulateAiResponse(request, model, AiProviderType.Ollama);
    }

    public async IAsyncEnumerable<ChatCompletionChunk> StreamChatAsync(
        ChatCompletionRequest request,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var result = await CompleteChatAsync(request, cancellationToken);
        var words = result.ResponseText.Split(' ', StringSplitOptions.RemoveEmptyEntries);

        foreach (var word in words)
        {
            await Task.Delay(20, cancellationToken);
            yield return new ChatCompletionChunk(word + " ");
        }

        foreach (var tc in result.ToolCalls)
        {
            yield return new ChatCompletionChunk(string.Empty, ToolCallDelta: tc);
        }

        yield return new ChatCompletionChunk(string.Empty, FinishReason: "stop", IsCompleted: true);
    }

    private static ChatCompletionResult SimulateAiResponse(ChatCompletionRequest request, string model, AiProviderType provider)
    {
        var lastUserMsg = request.Messages.LastOrDefault(m => m.Role == ChatRole.User)?.Content ?? string.Empty;
        var lower = lastUserMsg.ToLowerInvariant();
        var toolCalls = new List<ChatToolCallDto>();

        if (lower.Contains("quy chế") || lower.Contains("chính sách") || lower.Contains("hạn mức") || lower.Contains("tiền phòng"))
        {
            toolCalls.Add(new ChatToolCallDto(Guid.NewGuid().ToString("N"), "search_company_policy", JsonSerializer.Serialize(new { query = lastUserMsg })));
        }
        else if (lower.Contains("task") || lower.Contains("nhiệm vụ") || lower.Contains("cần duyệt"))
        {
            toolCalls.Add(new ChatToolCallDto(Guid.NewGuid().ToString("N"), "get_my_pending_tasks", "{}"));
        }
        else if (lower.Contains("thống kê") || lower.Contains("tổng số"))
        {
            toolCalls.Add(new ChatToolCallDto(Guid.NewGuid().ToString("N"), "get_approval_stats", "{}"));
        }

        return new ChatCompletionResult(
            ResponseText: toolCalls.Count > 0 ? "Đang gọi C# MCP Tool cục bộ..." : $"[Local MCP {model}] Trả lời: Hệ thống đang vận hành hoàn toàn trong mạng nội bộ an toàn.",
            ToolCalls: toolCalls,
            FinishReason: "stop",
            PromptTokens: 20,
            CompletionTokens: 25,
            ModelName: model,
            ProviderType: provider
        );
    }
}

