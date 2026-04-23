using CelularesSaaS.Application.Reportes;
using CelularesSaaS.Domain.Enums;
using CelularesSaaS.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using CelularesSaaS.Domain.Enums;

namespace CelularesSaaS.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class ReportesController : ControllerBase
{
    private readonly ApplicationDbContext _db;

    public ReportesController(ApplicationDbContext db) => _db = db;

    [HttpGet("dashboard")]
    public async Task<ActionResult<ResumenDashboardDto>> Dashboard()
    {
        var offset = TimeSpan.FromHours(-3);
        var ahoraLocal = DateTimeOffset.UtcNow.ToOffset(offset);
        var hoyLocal = ahoraLocal.Date;
        var hoyUtc = new DateTimeOffset(hoyLocal, offset).UtcDateTime;
        var inicioMesUtc = new DateTimeOffset(hoyLocal.Year, hoyLocal.Month, 1, 0, 0, 0, offset).UtcDateTime;

        var ventasHoy = await _db.Ventas
            .Where(v => !v.Anulada && v.EstadoVenta == EstadoVenta.Completada && v.Fecha >= hoyUtc)
            .SumAsync(v => (decimal?)v.Total) ?? 0;

        var ventasMes = await _db.Ventas
            .Where(v => !v.Anulada && v.EstadoVenta == EstadoVenta.Completada && v.Fecha >= inicioMesUtc)
            .SumAsync(v => (decimal?)v.Total) ?? 0;

        var gananciaHoy = await _db.Ventas
            .Where(v => !v.Anulada && v.EstadoVenta == EstadoVenta.Completada && v.Fecha >= hoyUtc)
            .SumAsync(v => (decimal?)v.Ganancia) ?? 0;

        var gananciaMes = await _db.Ventas
            .Where(v => !v.Anulada && v.EstadoVenta == EstadoVenta.Completada && v.Fecha >= inicioMesUtc)
            .SumAsync(v => (decimal?)v.Ganancia) ?? 0;

        var equiposEnStock = await _db.Equipos
            .CountAsync(e => e.Estado == EstadoEquipo.EnStock);

        var reparacionesPendientes = await _db.Reparaciones
            .CountAsync(r => r.Estado != EstadoReparacion.Entregado
                && r.Estado != EstadoReparacion.NoReparable
                && r.Estado != EstadoReparacion.Rechazado);

        var accesoriosStockBajo = await _db.Productos
            .CountAsync(a => a.Stock <= a.StockMinimo);

        var capitalEquipos = await _db.Equipos
            .Where(e => e.Estado == EstadoEquipo.EnStock)
            .SumAsync(e => (decimal?)e.PrecioCompra) ?? 0;

        return Ok(new ResumenDashboardDto(
            ventasHoy, ventasMes, gananciaHoy, gananciaMes,
            equiposEnStock, reparacionesPendientes,
            accesoriosStockBajo, capitalEquipos));
    }

    [HttpGet("ventas-por-dia")]
    public async Task<ActionResult<List<VentaPorDiaDto>>> VentasPorDia(
        [FromQuery] DateTime desde, [FromQuery] DateTime hasta)
    {
        var datos = await _db.Ventas
            .Where(v => !v.Anulada && v.Fecha >= desde && v.Fecha <= hasta)
            .GroupBy(v => v.Fecha.Date)
            .Select(g => new VentaPorDiaDto(
                g.Key,
                g.Sum(v => v.Total),
                g.Sum(v => v.Ganancia),
                g.Count()))
            .OrderBy(x => x.Fecha)
            .ToListAsync();

        return Ok(datos);
    }

    [HttpGet("stock-equipos")]
    public async Task<ActionResult<List<StockEquipoDto>>> StockEquipos()
    {
        var hoy = DateTime.UtcNow;
        var equipos = await _db.Equipos
            .Where(e => e.Estado == EstadoEquipo.EnStock)
            .Select(e => new StockEquipoDto(
                e.Id, e.Marca, e.Modelo, e.Capacidad, e.Color, e.Imei,
                e.Condicion.ToString(), e.Ubicacion.ToString(),
                e.PrecioVentaSugerido, e.MonedaVenta.ToString(),
                (int)(hoy - e.FechaIngreso).TotalDays))
            .OrderByDescending(e => e.DiasEnStock)
            .ToListAsync();

        return Ok(equipos);
    }
    [HttpGet("productos-mas-vendidos")]
    public async Task<ActionResult> ProductosMasVendidos([FromQuery] int dias = 30)
    {
        var offset = TimeSpan.FromHours(-3);
        var desde = DateTimeOffset.UtcNow.ToOffset(offset).Date.AddDays(-dias);
        var desdeUtc = new DateTimeOffset(desde, offset).UtcDateTime;

        var equipos = await _db.VentaItems
            .Include(i => i.Venta)
            .Include(i => i.Equipo)
            .Where(i => i.Tipo == TipoItemVenta.Equipo
                && i.Equipo != null
                && !i.Venta.Anulada
                && i.Venta.Fecha >= desdeUtc)
            .GroupBy(i => new { i.Equipo!.Marca, i.Equipo.Modelo })
            .Select(g => new
            {
                nombre = g.Key.Marca + " " + g.Key.Modelo,
                tipo = "Equipo",
                cantidadVendida = g.Sum(i => i.Cantidad),
                gananciaTotal = g.Sum(i => (i.PrecioUnitario - i.CostoUnitario) * i.Cantidad),
                totalVendido = g.Sum(i => i.PrecioUnitario * i.Cantidad),
            })
            .OrderByDescending(x => x.cantidadVendida)
            .Take(10)
            .ToListAsync();

        var productos = await _db.VentaItems
            .Include(i => i.Venta)
            .Where(i => i.Tipo == TipoItemVenta.Accesorio
                && i.AccesorioId != null
                && !i.Venta.Anulada
                && i.Venta.Fecha >= desdeUtc)
            .GroupBy(i => i.Descripcion)
            .Select(g => new
            {
                nombre = g.Key,
                tipo = "Producto",
                cantidadVendida = g.Sum(i => i.Cantidad),
                gananciaTotal = g.Sum(i => (i.PrecioUnitario - i.CostoUnitario) * i.Cantidad),
                totalVendido = g.Sum(i => i.PrecioUnitario * i.Cantidad),
            })
            .OrderByDescending(x => x.cantidadVendida)
            .Take(10)
            .ToListAsync();

        var resultado = equipos.Concat(productos)
            .OrderByDescending(x => x.cantidadVendida)
            .ToList();

        return Ok(resultado);
    }

    [HttpGet("ganancia-por-tipo")]
    public async Task<ActionResult> GananciaPorTipo([FromQuery] int dias = 30)
    {
        var offset = TimeSpan.FromHours(-3);
        var desde = DateTimeOffset.UtcNow.ToOffset(offset).Date.AddDays(-dias);
        var desdeUtc = new DateTimeOffset(desde, offset).UtcDateTime;

        var datos = await _db.VentaItems
            .Include(i => i.Venta)
            .Where(i => !i.Venta.Anulada && i.Venta.Fecha >= desdeUtc)
            .GroupBy(i => i.Tipo)
            .Select(g => new
            {
                tipo = g.Key.ToString(),
                ganancia = g.Sum(i => (i.PrecioUnitario - i.CostoUnitario) * i.Cantidad),
                totalVendido = g.Sum(i => i.PrecioUnitario * i.Cantidad),
                cantidadItems = g.Sum(i => i.Cantidad),
            })
            .ToListAsync();

        return Ok(datos);
    }

    [HttpGet("ventas-pendientes")]
    public async Task<ActionResult> VentasPendientes()
    {
        var ventas = await _db.Ventas
            .Include(v => v.Cliente)
            .Include(v => v.Items)
            .Where(v => !v.Anulada && (
                v.EstadoVenta == EstadoVenta.Pendiente ||
                v.EstadoVenta == EstadoVenta.Reservada))
            .OrderByDescending(v => v.Fecha)
            .Select(v => new
            {
                id = v.Id,
                numeroVenta = v.NumeroVenta,
                fecha = v.Fecha,
                estadoVenta = v.EstadoVenta.ToString(),
                clienteNombre = v.Cliente != null ? v.Cliente.NombreCompleto : "Cliente ocasional",
                total = v.Total,
                cantidadItems = v.Items.Count,
            })
            .ToListAsync();

        return Ok(ventas);
    }
}
