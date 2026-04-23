using CelularesSaaS.Domain.Common;
using CelularesSaaS.Domain.Enums;

namespace CelularesSaaS.Domain.Entities;

public class PartePago : BaseEntity, ITenantEntity
{
    public Guid TenantId { get; set; }

    public string Marca { get; set; } = null!;
    public string Modelo { get; set; } = null!;
    public string Capacidad { get; set; } = null!;
    public string Color { get; set; } = null!;
    public string Imei { get; set; } = null!;
    public int? BateriaPorcentaje { get; set; }
    public string? Observaciones { get; set; }

    public decimal ValorTomado { get; set; }
    public Moneda MonedaValor { get; set; }
    public decimal CotizacionDolar { get; set; }

    public Guid? EquipoGeneradoId { get; set; }
    public Equipo? EquipoGenerado { get; set; }

    public Guid? ClienteId { get; set; }
    public Cliente? Cliente { get; set; }

    public Guid? VentaId { get; set; }
    public Venta? Venta { get; set; }
}
