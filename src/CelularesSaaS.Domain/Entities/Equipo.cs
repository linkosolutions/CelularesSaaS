using CelularesSaaS.Domain.Common;
using CelularesSaaS.Domain.Enums;

namespace CelularesSaaS.Domain.Entities;

public class Equipo : BaseEntity, ITenantEntity, IAuditableEntity
{
    public Guid TenantId { get; set; }

    public string Marca { get; set; } = null!;
    public string Modelo { get; set; } = null!;
    public string Capacidad { get; set; } = null!;
    public string Color { get; set; } = null!;
    public string Imei { get; set; } = null!;
    public string? Imei2 { get; set; }
    public string? NumeroSerie { get; set; }

    public CondicionEquipo Condicion { get; set; }
    public EstadoEquipo Estado { get; set; } = EstadoEquipo.EnStock;
    public UbicacionEquipo Ubicacion { get; set; } = UbicacionEquipo.Vitrina;
    public int? BateriaPorcentaje { get; set; }
    public string? Observaciones { get; set; }

    public decimal PrecioCompra { get; set; }
    public Moneda MonedaCompra { get; set; }
    public decimal? CotizacionDolarCompra { get; set; }

    public decimal PrecioVentaSugerido { get; set; }
    public Moneda MonedaVenta { get; set; }

    public Guid? ProveedorId { get; set; }
    public Proveedor? Proveedor { get; set; }

    public Guid? ClienteOrigenId { get; set; }
    public Cliente? ClienteOrigen { get; set; }

    public Guid? PartePagoId { get; set; }
    public PartePago? PartePago { get; set; }

    public DateTime FechaIngreso { get; set; } = DateTime.UtcNow;

    public int? GarantiaMeses { get; set; }
    public DateTime? FechaVencimientoGarantia { get; set; }

    public Guid? CreadoPorUsuarioId { get; set; }
    public Guid? ModificadoPorUsuarioId { get; set; }

    public ICollection<EquipoHistorial> Historial { get; set; } = new List<EquipoHistorial>();
    public ICollection<VentaItem> VentaItems { get; set; } = new List<VentaItem>();
    public ICollection<Reparacion> Reparaciones { get; set; } = new List<Reparacion>();
}
