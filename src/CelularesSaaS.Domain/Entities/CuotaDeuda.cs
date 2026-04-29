using CelularesSaaS.Domain.Common;
namespace CelularesSaaS.Domain.Entities;

public class CuotaDeuda : BaseEntity, ITenantEntity
{
    public Guid TenantId { get; set; }
    public Guid DeudaId { get; set; }
    public Deuda Deuda { get; set; } = null!;
    public int NumeroCuota { get; set; }
    public decimal Monto { get; set; }
    public decimal MontoPagado { get; set; } = 0;
    public DateTime FechaVencimiento { get; set; }
    public DateTime? FechaPago { get; set; }
    public string Estado { get; set; } = "Pendiente"; // Pendiente, PagoParcial, Pagada
}