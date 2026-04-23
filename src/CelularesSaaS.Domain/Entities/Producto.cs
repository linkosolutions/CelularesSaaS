using CelularesSaaS.Domain.Common;
using CelularesSaaS.Domain.Enums;

namespace CelularesSaaS.Domain.Entities;

public class Producto : BaseEntity, ITenantEntity
{
    public Guid TenantId { get; set; }

    // Identificación
    public string Codigo { get; set; } = null!;
    public string? CodigoBarras { get; set; }
    public string Nombre { get; set; } = null!;
    public string? Descripcion { get; set; }
    public string? Marca { get; set; }
    public string? Modelo { get; set; }
    public string? Compatibilidad { get; set; }

    // Tipo
    public TipoProducto TipoProducto { get; set; }

    // Precios duales ARS/USD
    public decimal PrecioCompraARS { get; set; }
    public decimal PrecioCompraUSD { get; set; }
    public decimal PrecioVentaARS { get; set; }
    public decimal PrecioVentaUSD { get; set; }

    // Stock
    public int Stock { get; set; }
    public int StockMinimo { get; set; } = 0;

    // Imagen
    public string? ImagenUrl { get; set; }
    public string? ImagenPublicId { get; set; } // para borrar de Cloudinary

    // Proveedor
    public Guid? ProveedorId { get; set; }
    public Proveedor? Proveedor { get; set; }

    public ICollection<MovimientoStockProducto> Movimientos { get; set; } = new List<MovimientoStockProducto>();
}
