using EnterpriseBpm.Workflow.Domain.Entities;
using EnterpriseBpm.Workflow.Domain.Enums;
using EnterpriseBpm.Workflow.Domain.Exceptions;
using EnterpriseBpm.Workflow.Domain.Services;
using Xunit;

namespace EnterpriseBpm.SharedKernel.UnitTests;

public class WorkflowDomainTests
{
    private readonly IDigitalSignatureService _signatureService = new DigitalSignatureService();
    private const string TestSalt = "EnterpriseBpmSecretWorkflowSalt2026!#";

    [Fact]
    public void DigitalSignatureService_ShouldComputeValidHash_AndVerifySuccessfully()
    {
        // Arrange
        var instanceId = "wf_123456789";
        var stepOrder = 1;
        var actorUserId = Guid.NewGuid();
        var decision = ApprovalDecision.Approved;
        var timestamp = DateTime.UtcNow;

        // Act
        var hash = _signatureService.ComputeHash(instanceId, stepOrder, actorUserId, decision, timestamp, TestSalt);
        var isValid = _signatureService.VerifyHash(instanceId, stepOrder, actorUserId, decision, timestamp, TestSalt, hash);

        // Assert
        Assert.NotNull(hash);
        Assert.NotEmpty(hash);
        Assert.Equal(64, hash.Length); // SHA-256 hex string = 64 ký tự
        Assert.True(isValid);
    }

    [Fact]
    public void DigitalSignatureService_ShouldFailVerification_WhenTampered()
    {
        // Arrange
        var instanceId = "wf_123456789";
        var stepOrder = 1;
        var actorUserId = Guid.NewGuid();
        var decision = ApprovalDecision.Approved;
        var timestamp = DateTime.UtcNow;

        var originalHash = _signatureService.ComputeHash(instanceId, stepOrder, actorUserId, decision, timestamp, TestSalt);

        // Act - Kẻ xấu cố gắng đổi quyết định thành Rejected nhưng giữ nguyên hash
        var isTamperedValid = _signatureService.VerifyHash(
            instanceId, stepOrder, actorUserId, ApprovalDecision.Rejected, timestamp, TestSalt, originalHash);

        // Assert
        Assert.False(isTamperedValid);
    }

    [Fact]
    public void WorkflowSubmission_StateTransitions_ShouldWorkCorrectly()
    {
        // Arrange
        var submission = WorkflowSubmission.Create(
            workflowInstanceId: "wf_test_001",
            workflowDefinitionId: "def_purchase_request",
            formKey: "purchase_order_form",
            submittedByUserId: Guid.NewGuid(),
            requesterOrgUnitId: Guid.NewGuid(),
            formDataJson: "{\"amount\": 50000000}",
            priority: WorkflowPriority.High);

        // Assert initial
        Assert.Equal(WorkflowStatus.InProgress, submission.CurrentStatus);
        Assert.Equal(WorkflowPriority.High, submission.Priority);
        Assert.Null(submission.CompletedAt);

        // Act - Approve
        submission.MarkApproved();

        // Assert approved
        Assert.Equal(WorkflowStatus.Approved, submission.CurrentStatus);
        Assert.NotNull(submission.CompletedAt);

        // Assert cannot approve when cancelled
        var cancelledSubmission = WorkflowSubmission.Create(
            "wf_test_002", "def_leave", "leave_form", Guid.NewGuid(), Guid.NewGuid(), "{}");
        cancelledSubmission.Cancel(Guid.NewGuid());
        Assert.Throws<InvalidWorkflowOperationException>(() => cancelledSubmission.MarkApproved());
    }

    [Fact]
    public void ApprovalTask_ShouldPreventDuplicateCompletion_WhenAlreadyProcessed()
    {
        // Arrange
        var task = ApprovalTask.Create(
            workflowInstanceId: "wf_test_003",
            activityId: "ManagerApprovalNode",
            assignedToUserId: Guid.NewGuid(),
            targetRole: "LineManager",
            dueDate: DateTime.UtcNow.AddHours(24));

        Assert.Equal(ApprovalTaskStatus.Pending, task.Status);

        // Act 1 - Complete lần đầu
        task.Complete(ApprovalDecision.Approved, "Đồng ý phê duyệt đề xuất.");
        Assert.Equal(ApprovalTaskStatus.Completed, task.Status);
        Assert.Equal(ApprovalDecision.Approved, task.Decision);
        Assert.NotNull(task.CompletedAt);

        // Act 2 - Click đúp / Thao tác lặp lại -> Phải ném ngoại lệ TaskAlreadyProcessedException
        Assert.Throws<TaskAlreadyProcessedException>(() =>
            task.Complete(ApprovalDecision.Approved, "Cố tình phê duyệt lần hai."));
    }
}

