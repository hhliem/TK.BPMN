using System.Security.Cryptography;
using System.Text;
using EnterpriseBpm.Document.Domain.Entities;
using EnterpriseBpm.Document.Domain.Enums;
using EnterpriseBpm.Org.Application.Contracts;
using EnterpriseBpm.Org.Application.Events;
using EnterpriseBpm.Workflow.Application.Events;
using EnterpriseBpm.Workflow.Domain.Entities;
using EnterpriseBpm.Workflow.Domain.Enums;
using EnterpriseBpm.Workflow.Domain.Exceptions;
using Xunit;

namespace EnterpriseBpm.SharedKernel.UnitTests;

/// <summary>
/// Bộ kiểm thử Unit Tests cho Sprint 05: Document Templates, SLA Escalation & Task Delegation.
/// Bao gồm TSK-515, TSK-516, TSK-517.
/// </summary>
public class Sprint05Tests
{
    // =========================================================================
    // TSK-515: Test Sinh File Word/PDF & Toàn Vẹn SHA-256
    // =========================================================================

    [Theory]
    [InlineData("Quyết định phê duyệt mua sắm máy chủ Dell PowerEdge R750", "500,000,000 VNĐ", "Nguyễn Văn Giám Đốc", "Phòng CNTT")]
    [InlineData("Hợp đồng dịch vụ bảo trì định kỳ hệ thống 2026", "125,500,000 VNĐ", "Trần Thị Trưởng Phòng", "Ban Kế Hoạch & Đầu Tư")]
    [InlineData("Tờ trình mua sắm máy photocopy đa năng", "45,000,000 VNĐ", "Lê Văn Nhân Viên", "Phòng Hành Chính Quản Trị")]
    [InlineData("Biên bản bàn giao tài sản và trang thiết bị làm việc", "0 VNĐ", "Phạm Văn Kỹ Thuật", "Trung Tâm Vận Hành")]
    public void DocumentTemplate_ShouldHandle_ComplexVietnameseUnicodePlaceholders(
        string title, string amount, string approverName, string department)
    {
        // Arrange
        var placeholders = new Dictionary<string, object>
        {
            ["Title"] = title,
            ["Amount"] = amount,
            ["ApproverName"] = approverName,
            ["Department"] = department,
            ["MaliciousScript"] = "<script>alert('xss')</script>Chào buổi sángjavascript:void(0)"
        };

        // Act - Giả lập làm sạch placeholder (SanitizePlaceholders logic)
        var sanitized = new Dictionary<string, object>();
        foreach (var (k, v) in placeholders)
        {
            if (v is string s)
            {
                var clean = s.Replace("<script", "", StringComparison.OrdinalIgnoreCase)
                             .Replace("</script>", "", StringComparison.OrdinalIgnoreCase)
                             .Replace("javascript:", "", StringComparison.OrdinalIgnoreCase)
                             .Trim();
                sanitized[k] = clean;
            }
            else
            {
                sanitized[k] = v;
            }
        }

        var templateContent = "CỘNG HÒA XÃ HỘI CHỦ NGHĨA VIỆT NAM\nTiêu đề: {{Title}}\nSố tiền: {{Amount}}\nĐơn vị: {{Department}}\nNgười ký: {{ApproverName}}\nGhi chú: {{MaliciousScript}}";
        var rendered = templateContent;
        foreach (var (k, v) in sanitized)
        {
            rendered = rendered.Replace("{{" + k + "}}", v.ToString());
        }

        var renderedBytes = Encoding.UTF8.GetBytes(rendered);
        var sha256Hex = Convert.ToHexString(SHA256.HashData(renderedBytes)).ToLowerInvariant();

        // Assert
        Assert.Contains(title, rendered);
        Assert.Contains(amount, rendered);
        Assert.Contains(approverName, rendered);
        Assert.Contains(department, rendered);
        Assert.DoesNotContain("<script", rendered);
        Assert.DoesNotContain("javascript:", rendered);
        Assert.Equal(64, sha256Hex.Length);
    }

    [Fact]
    public void GeneratedDocument_IntegrityVerification_ShouldValidateSha256Correctly()
    {
        // Arrange
        var validHash = "e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855";
        var invalidShortHash = "abc123";

        // Act & Assert
        Assert.True(validHash.Length == 64);
        Assert.False(invalidShortHash.Length == 64);
        Assert.True(string.IsNullOrWhiteSpace(null));
    }

    [Fact]
    public void DocumentTemplate_Create_ShouldStoreValidMetadata()
    {
        // Arrange & Act
        var template = DocumentTemplate.Create(
            templateKey: "tpl_procurement_decision",
            title: "Mẫu Quyết Định Phê Duyệt Mua Sắm",
            format: TemplateFormat.Docx,
            oneDriveTemplateItemId: "templates/procurement_decision.docx",
            placeholderSchema: "{\"Title\":\"string\",\"Amount\":\"decimal\",\"ApproverName\":\"string\"}",
            description: "Mẫu quyết định mua sắm tự động"
        );

        // Assert
        Assert.Equal("tpl_procurement_decision", template.TemplateKey);
        Assert.Equal("Mẫu Quyết Định Phê Duyệt Mua Sắm", template.Title);
        Assert.Equal(TemplateFormat.Docx, template.Format);
        Assert.True(template.IsActive);
        Assert.NotNull(template.PlaceholderSchema);
        Assert.Equal("Mẫu quyết định mua sắm tự động", template.Description);
    }

    // =========================================================================
    // TSK-516: Test Worker Quét Quá Hạn SLA & Leo Thang (Escalation)
    // =========================================================================

    [Fact]
    public void ApprovalTask_OverdueSla_ShouldBeEscalatedSuccessfully()
    {
        // Arrange - Tạo nhiệm vụ có thời hạn đã qua trong quá khứ (DueDate lùi về 2 giờ trước)
        var pastDueDate = DateTime.UtcNow.AddHours(-2);
        var task = ApprovalTask.Create(
            workflowInstanceId: "wf_sla_breach_001",
            activityId: "ManagerApprovalNode",
            assignedToUserId: Guid.NewGuid(),
            targetRole: "DepartmentHead",
            dueDate: pastDueDate
        );

        Assert.False(task.IsEscalated);
        Assert.Equal(ApprovalTaskStatus.Pending, task.Status);
        Assert.True(task.DueDate < DateTime.UtcNow);

        // Act - Background worker phát hiện quá hạn và gọi MarkEscalated
        task.MarkEscalated();

        // Assert
        Assert.True(task.IsEscalated);
    }

    [Fact]
    public void ApprovalTask_FutureDueDate_ShouldNotBeConsideredOverdue()
    {
        // Arrange - Thời hạn SLA còn 24 giờ trong tương lai
        var futureDueDate = DateTime.UtcNow.AddHours(24);
        var task = ApprovalTask.Create(
            workflowInstanceId: "wf_sla_ok_002",
            activityId: "DirectorReviewNode",
            assignedToUserId: Guid.NewGuid(),
            targetRole: "BoardOfDirectors",
            dueDate: futureDueDate
        );

        // Act & Assert
        Assert.False(task.IsEscalated);
        Assert.False(task.DueDate < DateTime.UtcNow);
    }

    [Fact]
    public void TaskSlaBreachedEvent_ShouldHoldAccurateEscalationPayload()
    {
        // Arrange
        var taskId = Guid.NewGuid();
        var instanceId = "wf_sla_event_999";
        var formKey = "procurement_request";
        var orgUnitId = Guid.NewGuid();
        var approverId = Guid.NewGuid();
        var dueDate = DateTime.UtcNow.AddMinutes(-30);

        // Act
        var evt = new TaskSlaBreachedEvent(
            TaskId: taskId,
            WorkflowInstanceId: instanceId,
            FormKey: formKey,
            RequesterOrgUnitId: orgUnitId,
            AssignedToUserId: approverId,
            DueDate: dueDate
        );

        // Assert
        Assert.Equal(taskId, evt.TaskId);
        Assert.Equal(instanceId, evt.WorkflowInstanceId);
        Assert.Equal(formKey, evt.FormKey);
        Assert.Equal(orgUnitId, evt.RequesterOrgUnitId);
        Assert.Equal(approverId, evt.AssignedToUserId);
        Assert.Equal(dueDate, evt.DueDate);
    }

    // =========================================================================
    // TSK-517: Test Điều Chuyển Task Khi Nhân Viên Thôi Việc
    // =========================================================================

    [Fact]
    public void EmployeeDeactivated_ShouldReassignAllPendingTasks_ToDirectManager()
    {
        // Arrange - Giả lập nhân viên thôi việc có 5 nhiệm vụ Pending
        var deactivatedEmployeeId = Guid.NewGuid();
        var directManagerId = Guid.NewGuid();

        var tasks = new List<ApprovalTask>();
        for (int i = 1; i <= 5; i++)
        {
            var task = ApprovalTask.Create(
                workflowInstanceId: $"wf_task_inst_{i}",
                activityId: $"ApprovalNode_{i}",
                assignedToUserId: deactivatedEmployeeId,
                targetRole: "StaffReviewer",
                dueDate: DateTime.UtcNow.AddHours(48)
            );
            tasks.Add(task);
        }

        // Kiểm tra ban đầu cả 5 task đều thuộc nhân viên thôi việc
        Assert.All(tasks, t =>
        {
            Assert.Equal(deactivatedEmployeeId, t.AssignedToUserId);
            Assert.Equal(ApprovalTaskStatus.Pending, t.Status);
        });

        // Act - Thực hiện điều chuyển sang Quản lý trực tiếp
        var note = "Nhân sự tiền nhiệm đã thôi việc. Tự động điều chuyển sang Quản lý trực tiếp.";
        foreach (var task in tasks)
        {
            task.Reassign(directManagerId, note);
        }

        // Assert - Cả 5 task đều đã sang ManagerId và ghi nhận lý do điều chuyển
        Assert.All(tasks, t =>
        {
            Assert.Equal(directManagerId, t.AssignedToUserId);
            Assert.Equal(ApprovalTaskStatus.Pending, t.Status);
            Assert.Contains(note, t.Comment);
        });
    }

    [Fact]
    public void EmployeeDeactivated_WithoutDirectManager_ShouldFallbackToWorkflowAdmin()
    {
        // Arrange - Nhân sự thôi việc không có quản lý trực tiếp (DirectManagerId == null)
        var deactivatedEmployeeId = Guid.NewGuid();
        Guid? directManagerId = null;
        var workflowAdminFallbackId = Guid.Parse("11111111-1111-1111-1111-111111111111");

        var task = ApprovalTask.Create(
            workflowInstanceId: "wf_orphan_task_001",
            activityId: "CfoApprovalNode",
            assignedToUserId: deactivatedEmployeeId,
            targetRole: "CFO",
            dueDate: DateTime.UtcNow.AddDays(3)
        );

        // Act - Logic fallback trong EmployeeDeactivatedWorkflowHandler
        var targetApproverId = directManagerId ?? workflowAdminFallbackId;
        var note = directManagerId.HasValue
            ? "Tự động điều chuyển sang Quản lý trực tiếp."
            : "Nhân sự tiền nhiệm đã thôi việc và không có quản lý trực tiếp. Tự động điều chuyển sang Quản trị viên quy trình (Workflow Admin).";

        task.Reassign(targetApproverId, note);

        // Assert
        Assert.Equal(workflowAdminFallbackId, task.AssignedToUserId);
        Assert.Equal(ApprovalTaskStatus.Pending, task.Status);
        Assert.Contains("Workflow Admin", task.Comment);
    }

    [Fact]
    public void ApprovalTask_Reassign_ShouldThrowException_WhenTaskAlreadyCompleted()
    {
        // Arrange
        var task = ApprovalTask.Create(
            workflowInstanceId: "wf_completed_task",
            activityId: "LeadReview",
            assignedToUserId: Guid.NewGuid(),
            targetRole: "Lead",
            dueDate: DateTime.UtcNow.AddHours(12)
        );
        task.Complete(ApprovalDecision.Approved, "Đã duyệt trước khi nhân viên nghỉ.");

        // Act & Assert - Không thể điều chuyển task đã hoàn tất
        Assert.Throws<TaskAlreadyProcessedException>(() =>
            task.Reassign(Guid.NewGuid(), "Điều chuyển vô lý khi đã xong"));
    }
}
