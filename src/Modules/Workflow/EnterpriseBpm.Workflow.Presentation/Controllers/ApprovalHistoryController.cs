using EnterpriseBpm.Workflow.Application.Contracts;
using Microsoft.AspNetCore.Mvc;

namespace EnterpriseBpm.Workflow.Presentation.Controllers;

[ApiController]
[Route("api/workflows/history")]
public class ApprovalHistoryController : ControllerBase
{
    private readonly IApprovalHistoryService _historyService;

    public ApprovalHistoryController(IApprovalHistoryService historyService)
    {
        _historyService = historyService;
    }

    [HttpGet("{instanceId}")]
    public async Task<IActionResult> GetHistoryByInstance(string instanceId, CancellationToken cancellationToken)
    {
        var records = await _historyService.GetHistoryByInstanceAsync(instanceId, cancellationToken);
        return Ok(records);
    }
}

