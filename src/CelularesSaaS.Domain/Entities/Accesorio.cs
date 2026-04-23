using CelularesSaaS.Domain.Common;
using CelularesSaaS.Domain.Enums;

namespace CelularesSaaS.Domain.Entities;

public class Accesorio : BaseEntity, ITenantEntity
{
    public Guid TenantId { get; set; }

    public string Codigo { get; set; } = null!;
    public string? CodigoBarras { get; set; }
    public string Nombre { get; set; } = null!;
    public string? Descripcion { get; set; }
    public string? Categoria { get; set; }
    public string? Marca { get; set; }
    public string? Compatibilidad { get; set; }

    public decimal PrecioCompra { get; set; }
    public Moneda MonedaCompra { get; set; }
    public decimal PrecioVenta { get; set; }
    public Moneda MonedaVenta { get; set; }

    public int Stock { get; set; }
    public int StockMinimo { get; set; } = 0;

    public Guid? ProveedorId { get; set; }
    public Proveedor? Proveedor { get; set; }

    public ICollection<MovimientoStockAccesorio> Movimientos { get; set; } = new List<MovimientoStockAccesorio>();
}
