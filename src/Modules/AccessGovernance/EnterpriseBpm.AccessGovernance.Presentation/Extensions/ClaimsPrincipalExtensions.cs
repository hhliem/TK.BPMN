using System.Security.Claims;

namespace EnterpriseBpm.AccessGovernance.Presentation.Extensions;

public static class ClaimsPrincipalExtensions
{
    public static Guid GetUserId(this ClaimsPrincipal principal)
    {
        var id = principal.FindFirst(ClaimTypes.NameIdentifier)?.Value
            ?? principal.FindFirst("sub")?.Value;

        if (Guid.TryParse(id, out var userId))
        {
            return userId;
        }

        // Mặc định hoặc Anonymous Dev test
        return Guid.Empty;
    }
}

