using EnterpriseBpm.Org.Application.Contracts;
using EnterpriseBpm.Org.Application.DTOs;
using EnterpriseBpm.Org.Domain.Entities;
using EnterpriseBpm.Org.Domain.Services;
using EnterpriseBpm.Org.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace EnterpriseBpm.Org.Infrastructure.Services;

public class DelegationResolver : IDelegationResolver, IDelegationService
{
    private readonly OrgDbContext _dbContext;
    private readonly ILogger<DelegationResolver> _logger;

    public DelegationResolver(OrgDbContext dbContext, ILogger<DelegationResolver> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<Guid> ResolveActualApproverAsync(
        Guid configuredApproverId, 
        string formKey, 
        CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;

        // Tìm kiếm bản ghi ủy quyền đang có hiệu lực tại thời điểm hiện tại
        var activeDelegation = await _dbContext.WorkflowDelegations
            .AsNoTracking()
            .Where(d => d.DelegatorId == configuredApproverId 
                     && d.IsActive 
                     && d.FromDate <= now 
                     && d.ToDate >= now
                     && (d.ScopeFormKey == null || d.ScopeFormKey == formKey || d.ScopeFormKey == "*"))
            .OrderByDescending(d => d.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);

        if (activeDelegation != null)
        {
            _logger.LogInformation(
                "Phát hiện Ủy quyền hợp lệ: Phê duyệt viên cấu hình '{DelegatorId}' được ủy quyền cho '{DelegateeId}' đối với Form '{FormKey}'",
                configuredApproverId, activeDelegation.DelegateeId, formKey);

            return activeDelegation.DelegateeId;
        }

        return configuredApproverId;
    }

    public async Task<Guid> AssignDelegationAsync(AssignDelegationCommand command, CancellationToken cancellationToken = default)
    {
        // 1. Kiểm tra hai nhân viên tồn tại
        var delegatorExists = await _dbContext.Employees.AnyAsync(e => e.Id == command.DelegatorId && e.IsActive, cancellationToken);
        var delegateeExists = await _dbContext.Employees.AnyAsync(e => e.Id == command.DelegateeId && e.IsActive, cancellationToken);

        if (!delegatorExists || !delegateeExists)
        {
            throw new KeyNotFoundException("Người ủy quyền hoặc người được ủy quyền không tồn tại trong hệ thống.");
        }

        // 2. Lấy các bản ghi ủy quyền đang active của Delegatee để kiểm tra chống ủy quyền ngược/vòng tròn
        var activeDelegations = await _dbContext.WorkflowDelegations
            .AsNoTracking()
            .Where(d => d.IsActive && d.DelegatorId == command.DelegateeId)
            .Select(d => new { d.DelegatorId, d.DelegateeId, d.FromDate, d.ToDate, d.IsActive })
            .ToListAsync(cancellationToken);

        var tuples = activeDelegations.Select(d => (d.DelegatorId, d.DelegateeId, d.FromDate, d.ToDate, d.IsActive));

        // Anti-Circular Delegation Validation
        AntiCircularDelegationValidator.Validate(
            command.DelegatorId, 
            command.DelegateeId, 
            command.FromDate, 
            command.ToDate, 
            tuples);

        var delegation = new WorkflowDelegation
        {
            Id = Guid.NewGuid(),
            DelegatorId = command.DelegatorId,
            DelegateeId = command.DelegateeId,
            FromDate = command.FromDate,
            ToDate = command.ToDate,
            ScopeFormKey = command.ScopeFormKey,
            Reason = command.Reason,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        _dbContext.WorkflowDelegations.Add(delegation);
        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Thiết lập thành công ủy quyền {DelegationId} từ {DelegatorId} sang {DelegateeId}", delegation.Id, command.DelegatorId, command.DelegateeId);

        return delegation.Id;
    }

    public async Task RevokeDelegationAsync(Guid delegationId, CancellationToken cancellationToken = default)
    {
        var delegation = await _dbContext.WorkflowDelegations
            .FirstOrDefaultAsync(d => d.Id == delegationId, cancellationToken)
            ?? throw new KeyNotFoundException($"Không tìm thấy bản ghi ủy quyền: {delegationId}");

        delegation.IsActive = false;
        delegation.UpdatedAt = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("Hủy kích hoạt ủy quyền: {DelegationId}", delegationId);
    }

    public async Task<IReadOnlyList<DelegationDto>> GetMyDelegationsAsync(Guid employeeId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.WorkflowDelegations
            .AsNoTracking()
            .Where(d => d.DelegatorId == employeeId || d.DelegateeId == employeeId)
            .OrderByDescending(d => d.CreatedAt)
            .Select(d => new DelegationDto(
                d.Id,
                d.DelegatorId,
                d.Delegator.FullName,
                d.DelegateeId,
                d.Delegatee.FullName,
                d.FromDate,
                d.ToDate,
                d.ScopeFormKey,
                d.Reason,
                d.IsActive))
            .ToListAsync(cancellationToken);
    }
}

