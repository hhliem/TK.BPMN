using System.Net.Http.Json;
using EnterpriseBpm.Notification.Application.Contracts;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace EnterpriseBpm.Notification.Infrastructure.Services;

public class O365GraphMailProvider : IMailProvider
{
    private readonly ILogger<O365GraphMailProvider> _logger;
    private readonly string _serviceMailbox;

    public O365GraphMailProvider(
        IConfiguration configuration,
        ILogger<O365GraphMailProvider> logger)
    {
        _logger = logger;
        _serviceMailbox = configuration["Notification:Mail:ServiceMailbox"] ?? "workflow-notification@enterprise.com";
    }

    public async Task SendEmailAsync(string recipientEmail, string subject, string htmlBody, CancellationToken cancellationToken = default)
    {
        // Giả lập gửi mail qua Microsoft Graph API (chế độ an toàn cho môi trường test/dev)
        _logger.LogInformation("[O365 Graph Mail Provider] Đang gửi email từ {ServiceMailbox} đến {RecipientEmail} | Tiêu đề: {Subject}",
            _serviceMailbox, recipientEmail, subject);

        // Mô phỏng network delay
        await Task.Delay(50, cancellationToken);

        _logger.LogInformation("[O365 Graph Mail Provider] Gửi email thành công tới {RecipientEmail}", recipientEmail);
    }
}

public class TeamsWebhookProvider : ITeamsWebhookProvider
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<TeamsWebhookProvider> _logger;

    public TeamsWebhookProvider(
        HttpClient httpClient,
        ILogger<TeamsWebhookProvider> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task SendTeamsAdaptiveCardAsync(
        string webhookUrl,
        string title,
        string textBody,
        IDictionary<string, object?>? facts = null,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("[Teams Webhook Provider] Gửi thông báo Teams Adaptive Card: {Title}", title);

        var factList = facts?.Select(f => new { title = f.Key, value = f.Value?.ToString() ?? string.Empty }).ToList();

        var cardPayload = new
        {
            type = "message",
            attachments = new[]
            {
                new
                {
                    contentType = "application/vnd.microsoft.card.adaptive",
                    content = new
                    {
                        type = "AdaptiveCard",
                        version = "1.4",
                        body = new object[]
                        {
                            new { type = "TextBlock", size = "Medium", weight = "Bolder", text = title },
                            new { type = "TextBlock", text = textBody, wrap = true },
                            new { type = "FactSet", facts = factList }
                        }
                    }
                }
            }
        };

        if (!string.IsNullOrWhiteSpace(webhookUrl) && Uri.TryCreate(webhookUrl, UriKind.Absolute, out _))
        {
            try
            {
                var response = await _httpClient.PostAsJsonAsync(webhookUrl, cardPayload, cancellationToken);
                response.EnsureSuccessStatusCode();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "[Teams Webhook Provider] Không thể gửi tới webhook thực tế, fallback sang ghi log.");
            }
        }
        else
        {
            _logger.LogInformation("[Teams Webhook Provider] Webhook Url chưa cấu hình, hoàn tất giả lập card.");
        }
    }
}

