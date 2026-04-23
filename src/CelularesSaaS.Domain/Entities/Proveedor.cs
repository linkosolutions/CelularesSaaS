using CelularesSaaS.Domain.Common;

namespace CelularesSaaS.Domain.Entities;

public class Proveedor : BaseEntity, ITenantEntity
{
    public Guid TenantId { get; set; }

    public string Nombre { get; set; } = null!;
    public string? Cuit { get; set; }
    public string? Telefono { get; set; }
    public string? Email { get; set; }
    public string? Direccion { get; set; }
    public string? Notas { get; set; }

    public ICollection<Equipo> Equipos { get; set; } = new List<Equipo>();
    public ICollection<Accesorio> Accesorios { get; set; } = new List<Accesorio>();
}
