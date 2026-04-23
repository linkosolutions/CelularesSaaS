using CelularesSaaS.Domain.Common;
using CelularesSaaS.Domain.Enums;

namespace CelularesSaaS.Domain.Entities;

public class Reparacion : BaseEntity, ITenantEntity, IAuditableEntity
{
    public Guid TenantId { get; set; }

    public string NumeroOrden { get; set; } = null!;

    public Guid? EquipoId { get; set; }
    public Equipo? Equipo { get; set; }

    public string? ImeiExterno { get; set; }
    public string? MarcaExterna { get; set; }
    public string? ModeloExterno { get; set; }
    public string? ColorExterno { get; set; }

    public Guid? ClienteId { get; set; }
    public Cliente? Cliente { get; set; }

    public string ProblemaReportado { get; set; } = null!;
    public string? DiagnosticoTecnico { get; set; }
    public string? TrabajoRealizado { get; set; }

    public EstadoReparacion Estado { get; set; } = EstadoReparacion.Ingresado;

    public decimal? PresupuestoMonto { get; set; }
    public decimal? CostoRepuestos { get; set; }
    public decimal? ManoDeObra { get; set; }
    public decimal? TotalCobrado { get; set; }
    public Moneda? Moneda { get; set; }

    public DateTime FechaIngreso { get; set; } = DateTime.UtcNow;
    public DateTime? FechaEntrega { get; set; }

    public Guid? TecnicoId { get; set; }
    public Usuario? Tecnico { get; set; }

    public int? GarantiaDias { get; set; }
    public DateTime? FechaVencimientoGarantia { get; set; }

    public Guid? CreadoPorUsuarioId { get; set; }
    public Guid? ModificadoPorUsuarioId { get; set; }

    public ICollection<ReparacionHistorial> Historial { get; set; } = new List<ReparacionHistorial>();
}
