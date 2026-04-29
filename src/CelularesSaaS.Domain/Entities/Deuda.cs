using CelularesSaaS.Domain.Common;
namespace CelularesSaaS.Domain.Entities;

public class Deuda : BaseEntity, ITenantEntity
{
    public Guid TenantId { get; set; }
    public Guid? ClienteId { get; set; }
    public Cliente? Cliente { get; set; }
    public Guid VentaId { get; set; }
    public Venta Venta { get; set; } = null!;
    public decimal MontoOriginal { get; set; }
    public decimal MontoRestante { get; set; }
    public decimal Interes { get; set; } = 0;
    public int CantidadCuotas { get; set; }
    public string Estado { get; set; } = "Pendiente"; // Pendiente, PagoParcial, Saldada
    public string? Observaciones { get; set; }
    public ICollection<CuotaDeuda> Cuotas { get; set; } = new List<CuotaDeuda>();
}