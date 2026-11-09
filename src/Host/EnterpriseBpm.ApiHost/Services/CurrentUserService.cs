using System.Security.Claims;
using EnterpriseBpm.SharedKernel.Contracts;

namespace EnterpriseBpm.ApiHost.Services;

public class CurrentUserService : ICurrentUserService
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CurrentUserService(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public Guid? UserId
    {
        get
        {
            var user = _httpContextAccessor.HttpContext?.User;
            var idClaim = user?.FindFirst(ClaimTypes.NameIdentifier)?.Value
                ?? user?.FindFirst("sub")?.Value;

            if (Guid.TryParse(idClaim, out var userId))
            {
                return userId;
            }

            // Fallback: Kiểm tra header X-User-Id cho môi trường dev/staging
            var headerUserId = _httpContextAccessor.HttpContext?.Request.Headers["X-User-Id"].FirstOrDefault();
            if (Guid.TryParse(headerUserId, out var parsedHeaderId))
            {
                return parsedHeaderId;
            }

            return null;
        }
    }

    public string? UserName
    {
        get
        {
            var user = _httpContextAccessor.HttpContext?.User;
            return user?.FindFirst(ClaimTypes.Name)?.Value 
                ?? user?.FindFirst("preferred_username")?.Value 
                ?? _httpContextAccessor.HttpContext?.Request.Headers["X-User-Name"].FirstOrDefault();
        }
    }

    public string? UserPrincipalName
    {
        get
        {
            var user = _httpContextAccessor.HttpContext?.User;
            return user?.FindFirst(ClaimTypes.Upn)?.Value
                ?? user?.FindFirst(ClaimTypes.Email)?.Value;
        }
    }

    public Guid? OrgUnitId
    {
        get
        {
            var user = _httpContextAccessor.HttpContext?.User;
            var orgClaim = user?.FindFirst("org_unit_id")?.Value
                ?? _httpContextAccessor.HttpContext?.Request.Headers["X-Org-Unit-Id"].FirstOrDefault();

            if (Guid.TryParse(orgClaim, out var orgId))
            {
                return orgId;
            }

            return null;
        }
    }

    public IReadOnlyList<string> Roles
    {
        get
        {
            var user = _httpContextAccessor.HttpContext?.User;
            if (user == null)
            {
                return Array.Empty<string>();
            }

            var roleClaims = user.FindAll(ClaimTypes.Role)
                .Concat(user.FindAll("role"))
                .Select(c => c.Value)
                .Distinct()
                .ToList();

            return roleClaims;
        }
    }

    public string? IpAddress => _httpContextAccessor.HttpContext?.Connection.RemoteIpAddress?.ToString();

    public bool IsAuthenticated => _httpContextAccessor.HttpContext?.User?.Identity?.IsAuthenticated ?? false;
}
