using CelularesSaaS.Domain.Common;
using CelularesSaaS.Domain.Enums;

namespace CelularesSaaS.Domain.Entities;

public class Usuario : BaseEntity, ITenantEntity
{
    public Guid TenantId { get; set; }
    public Tenant Tenant { get; set; } = null!;

    public string NombreCompleto { get; set; } = null!;
    public string Email { get; set; } = null!;
    public string PasswordHash { get; set; } = null!;
    public RolUsuario Rol { get; set; }
    public DateTime? UltimoLogin { get; set; }
    public string? RefreshToken { get; set; }
    public DateTime? RefreshTokenExpira { get; set; }
}
