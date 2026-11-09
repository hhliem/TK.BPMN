using EnterpriseBpm.EventBus.Outbox;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace EnterpriseBpm.Workflow.Infrastructure.Persistence;

/// <summary>
/// Triển khai IOutboxStore sử dụng WorkflowDbContext (TSK-801, TSK-802)
/// </summary>
public class WorkflowOutboxStore : IOutboxStore
{
    private readonly WorkflowDbContext _dbContext;
    private readonly ILogger<WorkflowOutboxStore> _logger;

    public WorkflowOutboxStore(WorkflowDbContext dbContext, ILogger<WorkflowOutboxStore> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<IReadOnlyList<OutboxMessage>> GetPendingMessagesAsync(int batchSize = 50, CancellationToken cancellationToken = default)
    {
        return await _dbContext.OutboxMessages
            .Where(m => m.ProcessedAt == null && m.RetryCount < 5)
            .OrderBy(m => m.CreatedAt)
            .Take(batchSize)
            .ToListAsync(cancellationToken);
    }

    public async Task MarkProcessedAsync(Guid messageId, CancellationToken cancellationToken = default)
    {
        var msg = await _dbContext.OutboxMessages.FirstOrDefaultAsync(m => m.Id == messageId, cancellationToken);
        if (msg != null)
        {
            msg.MarkProcessed();
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
    }

    public async Task RecordFailureAsync(Guid messageId, string error, CancellationToken cancellationToken = default)
    {
        var msg = await _dbContext.OutboxMessages.FirstOrDefaultAsync(m => m.Id == messageId, cancellationToken);
        if (msg != null)
        {
            msg.RecordFailure(error);
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
    }
}

