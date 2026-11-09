using EnterpriseBpm.Notification.Application.Contracts;
using EnterpriseBpm.Notification.Domain.Entities;
using EnterpriseBpm.Notification.Domain.Enums;
using EnterpriseBpm.Notification.Domain.Services;
using EnterpriseBpm.Notification.Infrastructure.Services;
using Xunit;

namespace EnterpriseBpm.SharedKernel.UnitTests;

public class NotificationTests
{
    [Fact]
    public void TemplateRenderer_ShouldReplaceTokensCorrectly()
    {
        // Arrange
        var template = "Kính gửi {{ FullName }}, bạn có phiếu {{ FormKey }} cần duyệt tại {{ Url }}.";
        var parameters = new Dictionary<string, object?>
        {
            { "FullName", "Nguyễn Văn A" },
            { "FormKey", "PR-2026-001" },
            { "Url", "https://bpm.enterprise.com" }
        };

        // Act
        var result = TemplateRenderer.Render(template, parameters, sanitizeHtml: false);

        // Assert
        Assert.Equal("Kính gửi Nguyễn Văn A, bạn có phiếu PR-2026-001 cần duyệt tại https://bpm.enterprise.com.", result);
    }

    [Fact]
    public void TemplateRenderer_ShouldSanitizeHtml_ToPreventXss()
    {
        // Arrange
        var template = "<p>Lý do từ chối: {{ Reason }}</p>";
        var parameters = new Dictionary<string, object?>
        {
            { "Reason", "<script>alert('hack')</script>" }
        };

        // Act
        var result = TemplateRenderer.Render(template, parameters, sanitizeHtml: true);

        // Assert
        Assert.DoesNotContain("<script>", result);
        Assert.Contains("&lt;script&gt;", result);
    }

    [Fact]
    public void NotificationLog_RecordFailure_ShouldTransitionToFailed_AfterThreeAttempts()
    {
        // Arrange
        var log = new NotificationLog("TPL_TASK_ASSIGNED", NotificationChannel.Email, "test@enterprise.com", null, Guid.NewGuid());
        Assert.Equal(NotificationStatus.Pending, log.Status);
        Assert.Equal(0, log.RetryCount);

        // Act 1
        log.RecordFailure("Network timeout 1");
        Assert.Equal(1, log.RetryCount);
        Assert.Equal(NotificationStatus.Pending, log.Status);

        // Act 2
        log.RecordFailure("Network timeout 2");
        Assert.Equal(2, log.RetryCount);
        Assert.Equal(NotificationStatus.Pending, log.Status);

        // Act 3
        log.RecordFailure("Network timeout 3");
        Assert.Equal(3, log.RetryCount);
        Assert.Equal(NotificationStatus.Failed, log.Status);
    }

    [Fact]
    public void NotificationLog_MarkSent_ShouldUpdateStatusAndSentAt()
    {
        // Arrange
        var log = new NotificationLog("TPL_WORKFLOW_APPROVED", NotificationChannel.Email, "approver@enterprise.com", null, Guid.NewGuid());

        // Act
        log.MarkSent();

        // Assert
        Assert.Equal(NotificationStatus.Sent, log.Status);
        Assert.NotNull(log.SentAt);
        Assert.Null(log.LastError);
    }

    [Fact]
    public async Task NotificationQueue_ShouldEnqueueAndDequeueSuccessfully()
    {
        // Arrange
        var queue = new NotificationQueue(capacity: 10);
        var item = new NotificationDispatchItem(
            TemplateCode: "TPL_TEST",
            Channel: NotificationChannel.Email,
            RecipientEmail: "user@enterprise.com",
            PayloadParameters: new Dictionary<string, object?> { { "Key", "Val" } }
        );

        // Act
        await queue.EnqueueAsync(item);
        var dequeued = await queue.DequeueAsync();

        // Assert
        Assert.NotNull(dequeued);
        Assert.Equal("TPL_TEST", dequeued.TemplateCode);
        Assert.Equal("user@enterprise.com", dequeued.RecipientEmail);
    }
}

