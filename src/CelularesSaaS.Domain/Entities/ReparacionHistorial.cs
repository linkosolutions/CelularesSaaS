using CelularesSaaS.Domain.Common;
using CelularesSaaS.Domain.Enums;

namespace CelularesSaaS.Domain.Entities;

public class ReparacionHistorial : BaseEntity, ITenantEntity
{
    public Guid TenantId { get; set; }

    public Guid ReparacionId { get; set; }
    public Reparacion Reparacion { get; set; } = null!;

    public EstadoReparacion EstadoAnterior { get; set; }
    public EstadoReparacion EstadoNuevo { get; set; }
    public string? Comentario { get; set; }

    public Guid? UsuarioId { get; set; }
}
