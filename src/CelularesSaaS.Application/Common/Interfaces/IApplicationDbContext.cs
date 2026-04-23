using CelularesSaaS.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace CelularesSaaS.Application.Common.Interfaces;

public interface IApplicationDbContext
{
    DbSet<Tenant> Tenants { get; }
    DbSet<Usuario> Usuarios { get; }
    DbSet<Equipo> Equipos { get; }
    DbSet<EquipoHistorial> EquipoHistoriales { get; }
    DbSet<Accesorio> Accesorios { get; }
    DbSet<MovimientoStockAccesorio> MovimientosStockAccesorio { get; }
    DbSet<Cliente> Clientes { get; }
    DbSet<Proveedor> Proveedores { get; }
    DbSet<Venta> Ventas { get; }
    DbSet<VentaItem> VentaItems { get; }
    DbSet<PartePago> PartePagos { get; }
    DbSet<Pago> Pagos { get; }
    DbSet<Reparacion> Reparaciones { get; }
    DbSet<ReparacionHistorial> ReparacionHistoriales { get; }
    DbSet<CotizacionDolar> CotizacionesDolar { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
    DbSet<Producto> Productos { get; }
    DbSet<MovimientoStockProducto> MovimientosStockProducto { get; }
    DbSet<Cita> Citas { get; }
}
