using CelularesSaaS.Domain.Common;

namespace CelularesSaaS.Domain.Entities;

public class MovimientoStockProducto : BaseEntity, ITenantEntity
{
    public Guid TenantId { get; set; }

    public Guid ProductoId { get; set; }
    public Producto Producto { get; set; } = null!;

    public int Cantidad { get; set; }       // positivo = ingreso, negativo = egreso
    public int StockAnterior { get; set; }
    public int StockNuevo { get; set; }
    public string Motivo { get; set; } = null!;
    public Guid? VentaId { get; set; }
    public Guid? ReparacionId { get; set; }
    public Guid? UsuarioId { get; set; }
}
