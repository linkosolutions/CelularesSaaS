using CelularesSaaS.Domain.Common;
using CelularesSaaS.Domain.Enums;

namespace CelularesSaaS.Domain.Entities;

public class EquipoHistorial : BaseEntity, ITenantEntity
{
    public Guid TenantId { get; set; }

    public Guid EquipoId { get; set; }
    public Equipo Equipo { get; set; } = null!;

    public EstadoEquipo EstadoAnterior { get; set; }
    public EstadoEquipo EstadoNuevo { get; set; }
    public string? Motivo { get; set; }
    public string? Detalle { get; set; }

    public Guid? UsuarioId { get; set; }
    public Usuario? Usuario { get; set; }
}
