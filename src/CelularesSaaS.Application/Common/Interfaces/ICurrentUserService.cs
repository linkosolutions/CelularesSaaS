namespace CelularesSaaS.Application.Common.Interfaces;

public interface ICurrentUserService
{
    Guid? UserId { get; }
    Guid? TenantId { get; }
    string? Email { get; }
    string? Rol { get; }
    bool IsAuthenticated { get; }
}
