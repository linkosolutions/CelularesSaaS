using CelularesSaaS.Domain.Common;
using CelularesSaaS.Domain.Enums;

namespace CelularesSaaS.Domain.Entities;

public class MovimientoCaja : BaseEntity, ITenantEntity
{
    public Guid TenantId { get; set; }
    public int FormaPago { get; set; }       // mismo enum que ventas
    public int Moneda { get; set; }          // moneda del movimiento
    public decimal Monto { get; set; }       // positivo = ingreso, negativo = egreso
    public string Concepto { get; set; } = null!;  // "Venta #123", "Compra #456"
    public Guid? VentaId { get; set; }
    public Venta? Venta { get; set; }
    public Guid? CompraId { get; set; }
    public Compra? Compra { get; set; }
    public DateTime Fecha { get; set; } = DateTime.UtcNow;
}