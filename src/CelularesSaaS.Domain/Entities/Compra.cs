using CelularesSaaS.Domain.Common;
using CelularesSaaS.Domain.Enums;

namespace CelularesSaaS.Domain.Entities;

public class Compra : BaseEntity, ITenantEntity
{
    public Guid TenantId { get; set; }
    public Guid? ProveedorId { get; set; }
    public Proveedor? Proveedor { get; set; }
    public DateTime Fecha { get; set; } = DateTime.UtcNow;
    public decimal MontoTotal { get; set; }
    public Moneda Moneda { get; set; }
    public decimal CotizacionDolar { get; set; }
    public decimal MontoPagado { get; set; }
    public decimal MontoPendiente => MontoTotal - MontoPagado;
    public string? Observaciones { get; set; }
    public ICollection<CompraItem> Items { get; set; } = new List<CompraItem>();
    public ICollection<MovimientoCaja> Movimientos { get; set; } = new List<MovimientoCaja>();
}