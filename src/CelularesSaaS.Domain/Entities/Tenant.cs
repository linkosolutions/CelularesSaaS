using CelularesSaaS.Domain.Common;

namespace CelularesSaaS.Domain.Entities;

public class Tenant : BaseEntity
{
    public string Nombre { get; set; } = null!;
    public string NombreComercial { get; set; } = null!;
    public string? Cuit { get; set; }
    public string? Telefono { get; set; }
    public string? Email { get; set; }
    public string? Direccion { get; set; }
    public string? LogoUrl { get; set; }
    public string Slug { get; set; } = null!;
    public DateTime? FechaVencimientoPlan { get; set; }
    public string Plan { get; set; } = "Basico";

    public ICollection<Usuario> Usuarios { get; set; } = new List<Usuario>();
}
