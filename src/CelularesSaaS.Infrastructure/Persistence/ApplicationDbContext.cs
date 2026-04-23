using CelularesSaaS.Application.Common.Interfaces;
using CelularesSaaS.Domain.Common;
using CelularesSaaS.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace CelularesSaaS.Infrastructure.Persistence;

public class ApplicationDbContext : DbContext, IApplicationDbContext
{
    private readonly ICurrentUserService _currentUser;

    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options,
        ICurrentUserService currentUser) : base(options)
    {
        _currentUser = currentUser;
    }

    public DbSet<Tenant> Tenants => Set<Tenant>();
    public DbSet<Usuario> Usuarios => Set<Usuario>();
    public DbSet<Equipo> Equipos => Set<Equipo>();
    public DbSet<EquipoHistorial> EquipoHistoriales => Set<EquipoHistorial>();
    public DbSet<Accesorio> Accesorios => Set<Accesorio>();
    public DbSet<MovimientoStockAccesorio> MovimientosStockAccesorio => Set<MovimientoStockAccesorio>();
    public DbSet<Cliente> Clientes => Set<Cliente>();
    public DbSet<Proveedor> Proveedores => Set<Proveedor>();
    public DbSet<Venta> Ventas => Set<Venta>();
    public DbSet<VentaItem> VentaItems => Set<VentaItem>();
    public DbSet<PartePago> PartePagos => Set<PartePago>();
    public DbSet<Pago> Pagos => Set<Pago>();
    public DbSet<Reparacion> Reparaciones => Set<Reparacion>();
    public DbSet<ReparacionHistorial> ReparacionHistoriales => Set<ReparacionHistorial>();
    public DbSet<CotizacionDolar> CotizacionesDolar => Set<CotizacionDolar>();
    public DbSet<Cita> Citas => Set<Cita>();


    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);

        // ── Query filters multi-tenant (aplican automáticamente a todas las queries) ──
        var tenantId = _currentUser.TenantId;

        modelBuilder.Entity<Usuario>().HasQueryFilter(e => e.TenantId == tenantId && e.Activo);
        modelBuilder.Entity<Equipo>().HasQueryFilter(e => e.TenantId == tenantId && e.Activo);
        modelBuilder.Entity<EquipoHistorial>().HasQueryFilter(e => e.TenantId == tenantId);
        modelBuilder.Entity<Accesorio>().HasQueryFilter(e => e.TenantId == tenantId && e.Activo);
        modelBuilder.Entity<MovimientoStockAccesorio>().HasQueryFilter(e => e.TenantId == tenantId);
        modelBuilder.Entity<Cliente>().HasQueryFilter(e => e.TenantId == tenantId && e.Activo);
        modelBuilder.Entity<Proveedor>().HasQueryFilter(e => e.TenantId == tenantId && e.Activo);
        modelBuilder.Entity<Venta>().HasQueryFilter(e => e.TenantId == tenantId);
        modelBuilder.Entity<VentaItem>().HasQueryFilter(e => e.TenantId == tenantId);
        modelBuilder.Entity<PartePago>().HasQueryFilter(e => e.TenantId == tenantId);
        modelBuilder.Entity<Pago>().HasQueryFilter(e => e.TenantId == tenantId);
        modelBuilder.Entity<Reparacion>().HasQueryFilter(e => e.TenantId == tenantId);
        modelBuilder.Entity<ReparacionHistorial>().HasQueryFilter(e => e.TenantId == tenantId);
        modelBuilder.Entity<CotizacionDolar>().HasQueryFilter(e => e.TenantId == tenantId);
        modelBuilder.Entity<Producto>().HasQueryFilter(e => e.TenantId == tenantId && e.Activo);
        modelBuilder.Entity<MovimientoStockProducto>().HasQueryFilter(e => e.TenantId == tenantId);
        modelBuilder.Entity<Cita>().HasQueryFilter(e => e.TenantId == tenantId && e.Activo);
    }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        var userId = _currentUser.UserId;
        var tenantId = _currentUser.TenantId;

        foreach (var entry in ChangeTracker.Entries<BaseEntity>())
        {
            if (entry.State == EntityState.Added)
                entry.Entity.FechaCreacion = now;

            if (entry.State is EntityState.Added or EntityState.Modified)
                entry.Entity.FechaModificacion = now;
        }

        // Auto-set TenantId
        foreach (var entry in ChangeTracker.Entries<BaseEntity>())
        {
            if (entry.Entity is ITenantEntity tenantEntity && entry.State == EntityState.Added)
                if (tenantEntity.TenantId == Guid.Empty && tenantId.HasValue)
                    tenantEntity.TenantId = tenantId.Value;
        }

        // Auto-set auditoría
        foreach (var entry in ChangeTracker.Entries<IAuditableEntity>())
        {
            if (entry.State == EntityState.Added)
                entry.Entity.CreadoPorUsuarioId = userId;
            if (entry.State is EntityState.Added or EntityState.Modified)
                entry.Entity.ModificadoPorUsuarioId = userId;
        }

        return await base.SaveChangesAsync(cancellationToken);
    }
    public DbSet<Producto> Productos => Set<Producto>();
    public DbSet<MovimientoStockProducto> MovimientosStockProducto => Set<MovimientoStockProducto>();

}
