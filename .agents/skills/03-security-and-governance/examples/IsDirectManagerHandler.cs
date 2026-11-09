using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;

namespace EnterpriseBpm.Workflow.Application.Authorization;

public class IsDirectManagerRequirement : IAuthorizationRequirement { }

public class IsDirectManagerHandler : AuthorizationHandler<IsDirectManagerRequirement>
{
    private readonly IWorkflowContextService _contextService;

    public IsDirectManagerHandler(IWorkflowContextService contextService)
    {
        _contextService = contextService;
    }

    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        IsDirectManagerRequirement requirement)
    {
        var userIdClaim = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!Guid.TryParse(userIdClaim, out var currentUserId))
        {
            context.Fail();
            return;
        }

        var workflowId = _contextService.GetWorkflowInstanceIdFromRoute();
        var isManager = await _contextService.IsUserDirectManagerOfWorkflowCreatorAsync(currentUserId, workflowId);

        if (isManager)
            context.Succeed(requirement);
        else
            context.Fail();
    }
}
