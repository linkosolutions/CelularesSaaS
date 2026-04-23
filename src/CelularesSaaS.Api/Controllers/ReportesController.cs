using CelularesSaaS.Application.Common.Interfaces;
using CelularesSaaS.Application.Reportes;
using CelularesSaaS.Domain.Enums;
using CelularesSaaS.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CelularesSaaS.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class ReportesController : ControllerBase
{
    private readonly ApplicationDbContext _db;
    private readonly ICurrentUserService _user;

    public ReportesController(ApplicationDbContext db, ICurrentUserService user)
    {
        _db = db;
        _user = user;
    }

    private async Task<bool> TienePlanPro()
    {
        var tenant = await _db.Tenants.FindAsync(_user.TenantId);
        if (tenant == null) return false;

        // Durante la prueba tienen acceso Pro completo
        if (tenant.Plan == "Prueba" && tenant.FechaVencimientoPlan.HasValue
            && tenant.FechaVencimientoPlan > DateTime.UtcNow)
            return true;

        return tenant.Plan is "Pro" or "Enterprise";
    }

    // ═══════════════════════════════════════════
    // BÁSICO
    // ═══════════════════════════════════════════

    [HttpGet("dashboard")]
    public async Task<ActionResult<ResumenDashboardDto>> Dashboard()
    {
        var offset = TimeSpan.FromHours(-3);
        var ahoraLocal = DateTimeOffset.UtcNow.ToOffset(offset);
        var hoyLocal = ahoraLocal.Date;
        var hoyUtc = new DateTimeOffset(hoyLocal, offset).UtcDateTime;
        var inicioMesUtc = new DateTimeOffset(hoyLocal.Year, hoyLocal.Month, 1, 0, 0, 0, offset).UtcDateTime;

        var ventasHoy = await _db.Ventas.Where(v => !v.Anulada && v.EstadoVenta == EstadoVenta.Completada && v.Fecha >= hoyUtc).SumAsync(v => (decimal?)v.Total) ?? 0;
        var ventasMes = await _db.Ventas.Where(v => !v.Anulada && v.EstadoVenta == EstadoVenta.Completada && v.Fecha >= inicioMesUtc).SumAsync(v => (decimal?)v.Total) ?? 0;
        var gananciaHoy = await _db.Ventas.Where(v => !v.Anulada && v.EstadoVenta == EstadoVenta.Completada && v.Fecha >= hoyUtc).SumAsync(v => (decimal?)v.Ganancia) ?? 0;
        var gananciaMes = await _db.Ventas.Where(v => !v.Anulada && v.EstadoVenta == EstadoVenta.Completada && v.Fecha >= inicioMesUtc).SumAsync(v => (decimal?)v.Ganancia) ?? 0;

        var equiposEnStock = await _db.Equipos.CountAsync(e => e.Estado == EstadoEquipo.EnStock);
        var reparacionesPendientes = await _db.Reparaciones.CountAsync(r => r.Estado != EstadoReparacion.Entregado && r.Estado != EstadoReparacion.NoReparable && r.Estado != EstadoReparacion.Rechazado);
        var accesoriosStockBajo = await _db.Productos.CountAsync(a => a.Stock <= a.StockMinimo);
        var capitalEquipos = await _db.Equipos.Where(e => e.Estado == EstadoEquipo.EnStock).SumAsync(e => (decimal?)e.PrecioCompra) ?? 0;

        return Ok(new ResumenDashboardDto(ventasHoy, ventasMes, gananciaHoy, gananciaMes, equiposEnStock, reparacionesPendientes, accesoriosStockBajo, capitalEquipos));
    }

    [HttpGet("ventas-por-dia")]
    public async Task<ActionResult<List<VentaPorDiaDto>>> VentasPorDia(
        [FromQuery] DateTime desde, [FromQuery] DateTime hasta)
    {
        var datos = await _db.Ventas
            .Where(v => !v.Anulada && v.Fecha >= desde && v.Fecha <= hasta)
            .GroupBy(v => v.Fecha.Date)
            .Select(g => new VentaPorDiaDto(g.Key, g.Sum(v => v.Total), g.Sum(v => v.Ganancia), g.Count()))
            .OrderBy(x => x.Fecha)
            .ToListAsync();
        return Ok(datos);
    }

    [HttpGet("ventas-pendientes")]
    public async Task<ActionResult> VentasPendientes()
    {
        var ventas = await _db.Ventas
            .Include(v => v.Cliente)
            .Include(v => v.Items)
            .Where(v => !v.Anulada && (v.EstadoVenta == EstadoVenta.Pendiente || v.EstadoVenta == EstadoVenta.Reservada))
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

    [HttpGet("stock-valorizado")]
    public async Task<ActionResult> StockValorizado()
    {
        var cotizacion = await _db.CotizacionesDolar
            .OrderByDescending(c => c.Fecha)
            .Select(c => c.ValorVenta)
            .FirstOrDefaultAsync();
        if (cotizacion == 0) cotizacion = 1200;

        var equipos = await _db.Equipos
            .Where(e => e.Estado == EstadoEquipo.EnStock)
            .Select(e => new
            {
                marca = e.Marca,
                modelo = e.Modelo,
                costoARS = e.MonedaCompra == Moneda.USD ? e.PrecioCompra * (e.CotizacionDolarCompra ?? cotizacion) : e.PrecioCompra,
                ventaARS = e.MonedaVenta == Moneda.USD ? e.PrecioVentaSugerido * cotizacion : e.PrecioVentaSugerido,
            })
            .ToListAsync();

        var totalCostoARS = equipos.Sum(e => e.costoARS);
        var totalVentaARS = equipos.Sum(e => e.ventaARS);
        var totalCostoUSD = totalCostoARS / cotizacion;
        var totalVentaUSD = totalVentaARS / cotizacion;

        return Ok(new
        {
            cantidadEquipos = equipos.Count,
            totalCostoARS = Math.Round(totalCostoARS, 2),
            totalVentaARS = Math.Round(totalVentaARS, 2),
            totalCostoUSD = Math.Round(totalCostoUSD, 2),
            totalVentaUSD = Math.Round(totalVentaUSD, 2),
            gananciaEstimadaARS = Math.Round(totalVentaARS - totalCostoARS, 2),
            cotizacionUsada = cotizacion,
        });
    }

    [HttpGet("stock-minimo")]
    public async Task<ActionResult> StockMinimo()
    {
        var productos = await _db.Productos
            .Where(p => p.Stock <= p.StockMinimo)
            .OrderBy(p => p.Stock)
            .Select(p => new
            {
                id = p.Id,
                nombre = p.Nombre,
                codigo = p.Codigo,
                stock = p.Stock,
                stockMinimo = p.StockMinimo,
                faltantes = p.StockMinimo - p.Stock,
            })
            .ToListAsync();
        return Ok(productos);
    }

    [HttpGet("formas-de-pago")]
    public async Task<ActionResult> FormasDePago([FromQuery] int dias = 30)
    {
        var offset = TimeSpan.FromHours(-3);
        var desde = DateTimeOffset.UtcNow.ToOffset(offset).Date.AddDays(-dias);
        var desdeUtc = new DateTimeOffset(desde, offset).UtcDateTime;

        var datos = await _db.Pagos
            .Include(p => p.Venta)
            .Where(p => !p.Venta.Anulada && p.Venta.Fecha >= desdeUtc)
            .GroupBy(p => p.FormaPago)
            .Select(g => new
            {
                formaPago = g.Key.ToString(),
                totalARS = g.Sum(p => p.MontoConvertido),
                cantidadUsos = g.Count(),
            })
            .OrderByDescending(x => x.totalARS)
            .ToListAsync();
        return Ok(datos);
    }

    [HttpGet("garantias-activas")]
    public async Task<ActionResult> GarantiasActivas()
    {
        var hoy = DateTime.UtcNow;
        var equipos = await _db.Equipos
            .IgnoreQueryFilters()
            .Include(e => e.ClienteOrigen)
            .Where(e => e.TenantId == _user.TenantId
                && e.FechaVencimientoGarantia.HasValue
                && e.FechaVencimientoGarantia > hoy)
            .OrderBy(e => e.FechaVencimientoGarantia)
            .Select(e => new
            {
                id = e.Id,
                marca = e.Marca,
                modelo = e.Modelo,
                imei = e.Imei,
                garantiaMeses = e.GarantiaMeses,
                fechaVencimiento = e.FechaVencimientoGarantia,
                diasRestantes = (int)(e.FechaVencimientoGarantia!.Value - hoy).TotalDays,
                clienteNombre = e.ClienteOrigen != null ? e.ClienteOrigen.NombreCompleto : null,
            })
            .ToListAsync();
        return Ok(equipos);
    }

    [HttpGet("top-productos")]
    public async Task<ActionResult> TopProductos([FromQuery] int dias = 30)
    {
        var offset = TimeSpan.FromHours(-3);
        var desde = DateTimeOffset.UtcNow.ToOffset(offset).Date.AddDays(-dias);
        var desdeUtc = new DateTimeOffset(desde, offset).UtcDateTime;

        var resultado = await _db.VentaItems
            .Include(i => i.Venta)
            .Where(i => !i.Venta.Anulada && i.Venta.Fecha >= desdeUtc)
            .GroupBy(i => i.Descripcion)
            .Select(g => new
            {
                nombre = g.Key,
                cantidadVendida = g.Sum(i => i.Cantidad),
                totalVendido = g.Sum(i => i.PrecioUnitario * i.Cantidad),
            })
            .OrderByDescending(x => x.cantidadVendida)
            .Take(5)
            .ToListAsync();
        return Ok(resultado);
    }

    // ═══════════════════════════════════════════
    // PRO
    // ═══════════════════════════════════════════

    [HttpGet("productos-mas-vendidos")]
    public async Task<ActionResult> ProductosMasVendidos([FromQuery] int dias = 30)
    {
        if (!await TienePlanPro())
            return StatusCode(402, new { message = "Requiere plan Pro.", codigo = "PLAN_REQUERIDO" });

        var offset = TimeSpan.FromHours(-3);
        var desde = DateTimeOffset.UtcNow.ToOffset(offset).Date.AddDays(-dias);
        var desdeUtc = new DateTimeOffset(desde, offset).UtcDateTime;

        var equipos = await _db.VentaItems
            .Include(i => i.Venta).Include(i => i.Equipo)
            .Where(i => i.Tipo == TipoItemVenta.Equipo && i.Equipo != null && !i.Venta.Anulada && i.Venta.Fecha >= desdeUtc)
            .GroupBy(i => new { i.Equipo!.Marca, i.Equipo.Modelo })
            .Select(g => new
            {
                nombre = g.Key.Marca + " " + g.Key.Modelo,
                tipo = "Equipo",
                cantidadVendida = g.Sum(i => i.Cantidad),
                gananciaTotal = g.Sum(i => (i.PrecioUnitario - i.CostoUnitario) * i.Cantidad),
                totalVendido = g.Sum(i => i.PrecioUnitario * i.Cantidad),
                margenPromedio = g.Average(i => i.CostoUnitario > 0 ? ((i.PrecioUnitario - i.CostoUnitario) / i.PrecioUnitario) * 100 : 0),
            })
            .OrderByDescending(x => x.cantidadVendida)
            .Take(10)
            .ToListAsync();

        var productos = await _db.VentaItems
            .Include(i => i.Venta)
            .Where(i => i.Tipo == TipoItemVenta.Accesorio && !i.Venta.Anulada && i.Venta.Fecha >= desdeUtc)
            .GroupBy(i => i.Descripcion)
            .Select(g => new
            {
                nombre = g.Key,
                tipo = "Producto",
                cantidadVendida = g.Sum(i => i.Cantidad),
                gananciaTotal = g.Sum(i => (i.PrecioUnitario - i.CostoUnitario) * i.Cantidad),
                totalVendido = g.Sum(i => i.PrecioUnitario * i.Cantidad),
                margenPromedio = g.Average(i => i.CostoUnitario > 0 ? ((i.PrecioUnitario - i.CostoUnitario) / i.PrecioUnitario) * 100 : 0),
            })
            .OrderByDescending(x => x.cantidadVendida)
            .Take(10)
            .ToListAsync();

        return Ok(equipos.Concat(productos).OrderByDescending(x => x.cantidadVendida).ToList());
    }

    [HttpGet("ganancia-por-tipo")]
    public async Task<ActionResult> GananciaPorTipo([FromQuery] int dias = 30)
    {
        if (!await TienePlanPro())
            return StatusCode(402, new { message = "Requiere plan Pro.", codigo = "PLAN_REQUERIDO" });

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

    [HttpGet("ventas-por-vendedor")]
    public async Task<ActionResult> VentasPorVendedor([FromQuery] int dias = 30)
    {
        if (!await TienePlanPro())
            return StatusCode(402, new { message = "Requiere plan Pro.", codigo = "PLAN_REQUERIDO" });

        var offset = TimeSpan.FromHours(-3);
        var desde = DateTimeOffset.UtcNow.ToOffset(offset).Date.AddDays(-dias);
        var desdeUtc = new DateTimeOffset(desde, offset).UtcDateTime;

        var ventas = await _db.Ventas
            .Where(v => !v.Anulada && v.EstadoVenta == EstadoVenta.Completada && v.Fecha >= desdeUtc)
            .Select(v => new
            {
                v.CreadoPorUsuarioId,
                v.Total,
                v.Ganancia,
            })
            .ToListAsync();

        var usuarioIds = ventas
            .Where(v => v.CreadoPorUsuarioId.HasValue)
            .Select(v => v.CreadoPorUsuarioId!.Value)
            .Distinct()
            .ToList();

        var usuarios = await _db.Usuarios
            .IgnoreQueryFilters()
            .Where(u => usuarioIds.Contains(u.Id))
            .ToDictionaryAsync(u => u.Id, u => u.NombreCompleto);

        var datos = ventas
            .GroupBy(v => v.CreadoPorUsuarioId)
            .Select(g => new
            {
                vendedor = g.Key.HasValue && usuarios.ContainsKey(g.Key.Value)
                                 ? usuarios[g.Key.Value] : "Sin asignar",
                cantidadVentas = g.Count(),
                totalVendido = g.Sum(v => v.Total),
                gananciaTotal = g.Sum(v => v.Ganancia),
                ticketPromedio = g.Average(v => v.Total),
            })
            .OrderByDescending(x => x.totalVendido)
            .ToList();

        return Ok(datos);
    }

    [HttpGet("equipos-parados")]
    public async Task<ActionResult> EquiposParados([FromQuery] int dias = 60)
    {
        if (!await TienePlanPro())
            return StatusCode(402, new { message = "Requiere plan Pro.", codigo = "PLAN_REQUERIDO" });

        var hoy = DateTime.UtcNow;
        var limite = hoy.AddDays(-dias);

        var equipos = await _db.Equipos
            .Where(e => e.Estado == EstadoEquipo.EnStock && e.FechaIngreso <= limite)
            .OrderBy(e => e.FechaIngreso)
            .Select(e => new
            {
                id = e.Id,
                marca = e.Marca,
                modelo = e.Modelo,
                capacidad = e.Capacidad,
                color = e.Color,
                imei = e.Imei,
                diasEnStock = (int)(hoy - e.FechaIngreso).TotalDays,
                precioCompra = e.PrecioCompra,
                monedaCompra = e.MonedaCompra.ToString(),
                precioVenta = e.PrecioVentaSugerido,
                monedaVenta = e.MonedaVenta.ToString(),
            })
            .ToListAsync();
        return Ok(equipos);
    }

    [HttpGet("ranking-clientes")]
    public async Task<ActionResult> RankingClientes([FromQuery] int dias = 90)
    {
        if (!await TienePlanPro())
            return StatusCode(402, new { message = "Requiere plan Pro.", codigo = "PLAN_REQUERIDO" });

        var offset = TimeSpan.FromHours(-3);
        var desde = DateTimeOffset.UtcNow.ToOffset(offset).Date.AddDays(-dias);
        var desdeUtc = new DateTimeOffset(desde, offset).UtcDateTime;

        var datos = await _db.Ventas
            .Include(v => v.Cliente)
            .Where(v => !v.Anulada && v.EstadoVenta == EstadoVenta.Completada && v.ClienteId != null && v.Fecha >= desdeUtc)
            .GroupBy(v => new { v.ClienteId, Nombre = v.Cliente!.NombreCompleto, Telefono = v.Cliente.Telefono })
            .Select(g => new
            {
                clienteId = g.Key.ClienteId,
                nombre = g.Key.Nombre,
                telefono = g.Key.Telefono,
                cantidadCompras = g.Count(),
                totalGastado = g.Sum(v => v.Total),
                ticketPromedio = g.Average(v => v.Total),
                ultimaCompra = g.Max(v => v.Fecha),
            })
            .OrderByDescending(x => x.totalGastado)
            .Take(20)
            .ToListAsync();
        return Ok(datos);
    }

    [HttpGet("clientes-inactivos")]
    public async Task<ActionResult> ClientesInactivos([FromQuery] int dias = 60)
    {
        if (!await TienePlanPro())
            return StatusCode(402, new { message = "Requiere plan Pro.", codigo = "PLAN_REQUERIDO" });

        var offset = TimeSpan.FromHours(-3);
        var limite = DateTimeOffset.UtcNow.ToOffset(offset).UtcDateTime.AddDays(-dias);

        var datos = await _db.Clientes
            .Where(c => c.Activo)
            .Select(c => new
            {
                id = c.Id,
                nombre = c.NombreCompleto,
                telefono = c.Telefono,
                email = c.Email,
                ultimaCompra = _db.Ventas
                    .Where(v => v.ClienteId == c.Id && !v.Anulada)
                    .OrderByDescending(v => v.Fecha)
                    .Select(v => (DateTime?)v.Fecha)
                    .FirstOrDefault(),
                totalCompras = _db.Ventas
                    .Count(v => v.ClienteId == c.Id && !v.Anulada),
            })
            .ToListAsync();

        var inactivos = datos
            .Where(c => c.ultimaCompra == null || c.ultimaCompra < limite)
            .OrderBy(c => c.ultimaCompra)
            .ToList();

        return Ok(inactivos);
    }

    [HttpGet("permutas")]
    public async Task<ActionResult> Permutas([FromQuery] int dias = 90)
    {
        if (!await TienePlanPro())
            return StatusCode(402, new { message = "Requiere plan Pro.", codigo = "PLAN_REQUERIDO" });

        var offset = TimeSpan.FromHours(-3);
        var desde = DateTimeOffset.UtcNow.ToOffset(offset).Date.AddDays(-dias);
        var desdeUtc = new DateTimeOffset(desde, offset).UtcDateTime;

        var permutas = await _db.PartePagos
            .Include(p => p.EquipoGenerado)
            .Include(p => p.Cliente)
            .Where(p => p.FechaCreacion >= desdeUtc)
            .Select(p => new
            {
                id = p.Id,
                marca = p.Marca,
                modelo = p.Modelo,
                imei = p.Imei,
                valorTomado = p.ValorTomado,
                moneda = p.MonedaValor.ToString(),
                clienteNombre = p.Cliente != null ? p.Cliente.NombreCompleto : null,
                fecha = p.FechaCreacion,
                estadoEquipo = p.EquipoGenerado != null ? p.EquipoGenerado.Estado.ToString() : null,
                precioVentaActual = p.EquipoGenerado != null ? p.EquipoGenerado.PrecioVentaSugerido : (decimal?)null,
                vendido = p.EquipoGenerado != null && p.EquipoGenerado.Estado == EstadoEquipo.Vendido,
            })
            .OrderByDescending(p => p.fecha)
            .ToListAsync();
        return Ok(permutas);
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
}