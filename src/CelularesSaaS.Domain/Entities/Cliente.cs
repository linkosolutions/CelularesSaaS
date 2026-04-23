using CelularesSaaS.Domain.Common;

namespace CelularesSaaS.Domain.Entities;

public class Cliente : BaseEntity, ITenantEntity
{
    public Guid TenantId { get; set; }

    public string NombreCompleto { get; set; } = null!;
    public string? Dni { get; set; }
    public string? Telefono { get; set; }
    public string? Email { get; set; }
    public string? Direccion { get; set; }
    public string? Notas { get; set; }

    public ICollection<Venta> Ventas { get; set; } = new List<Venta>();
    public ICollection<Reparacion> Reparaciones { get; set; } = new List<Reparacion>();
    public ICollection<Equipo> EquiposVendidos { get; set; } = new List<Equipo>();
}
