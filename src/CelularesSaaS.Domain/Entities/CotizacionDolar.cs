using CelularesSaaS.Domain.Common;

namespace CelularesSaaS.Domain.Entities;

public class CotizacionDolar : BaseEntity, ITenantEntity
{
    public Guid TenantId { get; set; }

    public DateTime Fecha { get; set; }
    public decimal ValorCompra { get; set; }
    public decimal ValorVenta { get; set; }
    public string? Fuente { get; set; }
}
