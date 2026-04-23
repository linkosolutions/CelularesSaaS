using CelularesSaaS.Domain.Common;
using CelularesSaaS.Domain.Enums;

namespace CelularesSaaS.Domain.Entities;

public class Pago : BaseEntity, ITenantEntity
{
    public Guid TenantId { get; set; }

    public Guid VentaId { get; set; }
    public Venta Venta { get; set; } = null!;

    public FormaPago FormaPago { get; set; }
    public decimal Monto { get; set; }
    public Moneda Moneda { get; set; }
    public decimal CotizacionDolar { get; set; }
    public decimal MontoConvertido { get; set; }

    public string? Referencia { get; set; }
    public string? Observaciones { get; set; }
}
