using CelularesSaaS.Domain.Common;
using CelularesSaaS.Domain.Enums;

namespace CelularesSaaS.Domain.Entities;

public class VentaItem : BaseEntity, ITenantEntity
{
    public Guid TenantId { get; set; }

    public Guid VentaId { get; set; }
    public Venta Venta { get; set; } = null!;

    public TipoItemVenta Tipo { get; set; }

    public Guid? EquipoId { get; set; }
    public Equipo? Equipo { get; set; }

    public Guid? AccesorioId { get; set; }
    public Accesorio? Accesorio { get; set; }

    public string Descripcion { get; set; } = null!;
    public int Cantidad { get; set; } = 1;
    public decimal PrecioUnitario { get; set; }
    public decimal CostoUnitario { get; set; }
    public decimal Subtotal { get; set; }
    public Moneda Moneda { get; set; }
    public Guid? ProductoId { get; set; }
    public Producto? Producto { get; set; }
}
