using CelularesSaaS.Domain.Common;
using CelularesSaaS.Domain.Enums;

namespace CelularesSaaS.Domain.Entities;

public class Venta : BaseEntity, ITenantEntity, IAuditableEntity
{
    public Guid TenantId { get; set; }

    public string NumeroVenta { get; set; } = null!;
    public DateTime Fecha { get; set; } = DateTime.UtcNow;

    public Guid? ClienteId { get; set; }
    public Cliente? Cliente { get; set; }

    public decimal Subtotal { get; set; }
    public decimal Descuento { get; set; }
    public decimal Total { get; set; }
    public Moneda MonedaTotal { get; set; }
    public decimal CotizacionDolar { get; set; }

    public decimal CostoTotal { get; set; }
    public decimal Ganancia { get; set; }

    public Guid? PartePagoId { get; set; }
    public PartePago? PartePago { get; set; }

    public string? Observaciones { get; set; }
    public bool Anulada { get; set; }
    public DateTime? FechaAnulacion { get; set; }
    public string? MotivoAnulacion { get; set; }

    public Guid? CreadoPorUsuarioId { get; set; }
    public Guid? ModificadoPorUsuarioId { get; set; }

    public ICollection<VentaItem> Items { get; set; } = new List<VentaItem>();
    public ICollection<Pago> Pagos { get; set; } = new List<Pago>();
    public EstadoVenta EstadoVenta { get; set; } = EstadoVenta.Completada;
}
