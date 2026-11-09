using EnterpriseBpm.Analytics.Domain.Enums;
using EnterpriseBpm.SharedKernel;
using EnterpriseBpm.SharedKernel.Contracts;

namespace EnterpriseBpm.Analytics.Domain.Entities;

/// <summary>
/// Đăng ký lập lịch xuất & gửi báo cáo tự động (Scheduled Report Subscription - TSK-701, TSK-709)
/// </summary>
public class ScheduledReportSubscription : BaseEntity<Guid>, IAuditable
{
    public Guid TemplateId { get; private set; }
    public string Title { get; private set; } = default!;
    public string CronExpression { get; private set; } = "0 8 * * 1"; // Mặc định: 08:00 sáng Thứ Hai hàng tuần
    public ReportFrequency Frequency { get; private set; } = ReportFrequency.Weekly;
    public ReportExportFormat ExportFormat { get; private set; } = ReportExportFormat.Excel;

    // Danh sách email nhận (JSON mảng chuỗi)
    public string RecipientsEmailsJson { get; private set; } = "[]";

    public bool IsActive { get; private set; } = true;
    public DateTime? LastRunAt { get; private set; }
    public DateTime? NextRunAt { get; private set; }
    public string? LastRunStatus { get; private set; }
    public Guid CreatedByUserId { get; private set; }

    // IAuditable
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public Guid? CreatedBy { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public Guid? UpdatedBy { get; set; }

    protected ScheduledReportSubscription() { }

    public static ScheduledReportSubscription Create(
        Guid templateId,
        string title,
        string cronExpression,
        ReportFrequency frequency,
        ReportExportFormat exportFormat,
        string recipientsEmailsJson,
        Guid createdByUserId)
    {
        return new ScheduledReportSubscription
        {
            Id = Guid.NewGuid(),
            TemplateId = templateId,
            Title = title.Trim(),
            CronExpression = string.IsNullOrWhiteSpace(cronExpression) ? "0 8 * * 1" : cronExpression.Trim(),
            Frequency = frequency,
            ExportFormat = exportFormat,
            RecipientsEmailsJson = string.IsNullOrWhiteSpace(recipientsEmailsJson) ? "[]" : recipientsEmailsJson,
            IsActive = true,
            NextRunAt = DateTime.UtcNow.AddMinutes(5), // Lên lịch lần chạy kế tiếp
            CreatedByUserId = createdByUserId,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = createdByUserId
        };
    }

    public void MarkExecuted(bool success, string? message, DateTime? nextRun)
    {
        LastRunAt = DateTime.UtcNow;
        LastRunStatus = success ? "Success" : $"Failed: {message}";
        NextRunAt = nextRun ?? DateTime.UtcNow.AddDays(7);
        UpdatedAt = DateTime.UtcNow;
    }
}

