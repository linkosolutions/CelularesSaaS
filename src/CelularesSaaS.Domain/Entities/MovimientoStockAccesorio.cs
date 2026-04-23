using CelularesSaaS.Domain.Common;

namespace CelularesSaaS.Domain.Entities;

public class MovimientoStockAccesorio : BaseEntity, ITenantEntity
{
    public Guid TenantId { get; set; }

    public Guid AccesorioId { get; set; }
    public Accesorio Accesorio { get; set; } = null!;

    public int Cantidad { get; set; }
    public int StockAnterior { get; set; }
    public int StockNuevo { get; set; }
    public string Motivo { get; set; } = null!;
    public Guid? VentaId { get; set; }
    public Guid? UsuarioId { get; set; }
}
