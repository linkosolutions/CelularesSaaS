using System.Security.Claims;
using CelularesSaaS.Application.Common.Interfaces;
using Microsoft.AspNetCore.Http;

namespace CelularesSaaS.Infrastructure.Services;

public class CurrentUserService : ICurrentUserService
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CurrentUserService(IHttpContextAccessor httpContextAccessor)
        => _httpContextAccessor = httpContextAccessor;

    private ClaimsPrincipal? User => _httpContextAccessor.HttpContext?.User;

    public Guid? UserId
    {
        get
        {
            var val = User?.FindFirstValue(ClaimTypes.NameIdentifier);
            return val != null ? Guid.Parse(val) : null;
        }
    }

    public Guid? TenantId
    {
        get
        {
            var val = User?.FindFirstValue("tenantId");
            return val != null ? Guid.Parse(val) : null;
        }
    }

    public string? Email => User?.FindFirstValue(ClaimTypes.Email);
    public string? Rol => User?.FindFirstValue(ClaimTypes.Role);
    public bool IsAuthenticated => User?.Identity?.IsAuthenticated ?? false;
}
