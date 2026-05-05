using CelularesSaaS.Domain.Common;
using CelularesSaaS.Domain.Enums;

namespace CelularesSaaS.Domain.Entities;

public class CompraItem : BaseEntity
{
    public Guid CompraId { get; set; }
    public Compra Compra { get; set; } = null!;
    public Guid? EquipoId { get; set; }
    public Equipo? Equipo { get; set; }
    public Guid? ProductoId { get; set; }
    public Producto? Producto { get; set; }
    public int Cantidad { get; set; }
    public decimal PrecioUnitario { get; set; }
    public Moneda Moneda { get; set; }
}