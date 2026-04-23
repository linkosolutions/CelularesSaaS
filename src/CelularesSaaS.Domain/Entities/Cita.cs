using CelularesSaaS.Domain.Common;
using CelularesSaaS.Domain.Enums;

namespace CelularesSaaS.Domain.Entities;

public class Cita : BaseEntity, ITenantEntity
{
    public Guid TenantId { get; set; }

    public DateTime FechaHora { get; set; }
    public string Motivo { get; set; } = null!;
    public EstadoCita Estado { get; set; } = EstadoCita.Pendiente;
    public string? Notas { get; set; }

    public Guid? ClienteId { get; set; }
    public Cliente? Cliente { get; set; }

    // Si no tiene cliente registrado
    public string? NombreContacto { get; set; }
    public string? TelefonoContacto { get; set; }

    public Guid? UsuarioAsignadoId { get; set; }
    public Usuario? UsuarioAsignado { get; set; }
}
