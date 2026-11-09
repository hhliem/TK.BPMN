using EnterpriseBpm.Workflow.Application.Contracts;
using EnterpriseBpm.Workflow.Application.DTOs;
using EnterpriseBpm.Workflow.Domain.Entities;
using EnterpriseBpm.Workflow.Domain.Enums;
using EnterpriseBpm.Workflow.Domain.Services;
using EnterpriseBpm.Workflow.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace EnterpriseBpm.Workflow.Infrastructure.Services;

public class ApprovalHistoryService : IApprovalHistoryService
{
    private readonly WorkflowDbContext _dbContext;
    private readonly IDigitalSignatureService _signatureService;
    private readonly IConfiguration _configuration;
    private readonly ILogger<ApprovalHistoryService> _logger;

    public ApprovalHistoryService(
        WorkflowDbContext dbContext,
        IDigitalSignatureService signatureService,
        IConfiguration configuration,
        ILogger<ApprovalHistoryService> logger)
    {
        _dbContext = dbContext;
        _signatureService = signatureService;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task RecordHistoryAsync(
        string workflowInstanceId,
        int stepOrder,
        string stepName,
        Guid actorUserId,
        ApprovalDecision decision,
        string? comment,
        Guid? onBehalfOfUserId = null,
        Guid? delegationId = null,
        CancellationToken cancellationToken = default)
    {
        var salt = _configuration.GetValue<string>("Security:SignatureSalt")
            ?? throw new InvalidOperationException("Khóa bảo mật 'Security:SignatureSalt' chưa được cấu hình.");

        var timestamp = DateTime.UtcNow;
        var hash = _signatureService.ComputeHash(
            workflowInstanceId,
            stepOrder,
            actorUserId,
            decision,
            timestamp,
            salt);

        var historyRecord = ApprovalHistory.Create(
            workflowInstanceId: workflowInstanceId,
            stepOrder: stepOrder,
            stepName: stepName,
            actorUserId: actorUserId,
            decision: decision,
            digitalSignatureHash: hash,
            comment: comment,
            onBehalfOfUserId: onBehalfOfUserId,
            delegationId: delegationId,
            actionTimestamp: timestamp);

        await _dbContext.ApprovalHistory.AddAsync(historyRecord, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Đã ghi nhận lịch sử duyệt bất biến cho Instance {InstanceId}, Bước {StepOrder} ({StepName}), Quyết định: {Decision}, Hash: {Hash}",
            workflowInstanceId, stepOrder, stepName, decision, hash);
    }

    public async Task<IReadOnlyList<ApprovalHistoryDto>> GetHistoryByInstanceAsync(
        string workflowInstanceId,
        CancellationToken cancellationToken = default)
    {
        var salt = _configuration.GetValue<string>("Security:SignatureSalt")
            ?? throw new InvalidOperationException("Khóa bảo mật 'Security:SignatureSalt' chưa được cấu hình.");

        var records = await _dbContext.ApprovalHistory
            .AsNoTracking()
            .Where(h => h.WorkflowInstanceId == workflowInstanceId)
            .OrderBy(h => h.StepOrder)
            .ThenBy(h => h.ActionTimestamp)
            .ToListAsync(cancellationToken);

        return records.Select(r =>
        {
            var isValid = _signatureService.VerifyHash(
                r.WorkflowInstanceId,
                r.StepOrder,
                r.ActorUserId,
                r.Decision,
                r.ActionTimestamp,
                salt,
                r.DigitalSignatureHash);

            return new ApprovalHistoryDto(
                Id: r.Id,
                WorkflowInstanceId: r.WorkflowInstanceId,
                StepOrder: r.StepOrder,
                StepName: r.StepName,
                ActorUserId: r.ActorUserId,
                OnBehalfOfUserId: r.OnBehalfOfUserId,
                DelegationId: r.DelegationId,
                Decision: r.Decision.ToString(),
                Comment: r.Comment,
                ActionTimestamp: r.ActionTimestamp,
                DigitalSignatureHash: r.DigitalSignatureHash,
                IsValidSignature: isValid
            );
        }).ToList();
    }
}

