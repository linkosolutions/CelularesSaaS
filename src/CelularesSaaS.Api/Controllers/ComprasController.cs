using CelularesSaaS.Application.Common.Exceptions;
using CelularesSaaS.Application.Common.Interfaces;
using CelularesSaaS.Domain.Entities;
using CelularesSaaS.Domain.Enums;
using CelularesSaaS.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CelularesSaaS.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class ComprasController : ControllerBase
{
    private readonly ApplicationDbContext _db;
    private readonly ICurrentUserService _user;

    public ComprasController(ApplicationDbContext db, ICurrentUserService user)
    {
        _db = db;
        _user = user;
    }

    [HttpGet]
    public async Task<ActionResult> Listar()
    {
        var compras = await _db.Compras
            .Include(c => c.Proveedor)
            .Include(c => c.Items)
            .OrderByDescending(c => c.Fecha)
            .Select(c => new
            {
                id = c.Id,
                fecha = c.Fecha,
                proveedor = c.Proveedor != null ? c.Proveedor.Nombre : null,
                proveedorId = c.ProveedorId,
                montoTotal = c.MontoTotal,
                montoPagado = c.MontoPagado,
                montoPendiente = c.MontoTotal - c.MontoPagado,
                moneda = c.Moneda.ToString(),
                observaciones = c.Observaciones,
                cantItems = c.Items.Count,
            })
            .ToListAsync();

        return Ok(compras);
    }

    [HttpPost]
    public async Task<ActionResult> Crear([FromBody] CrearCompraRequest request)
    {
        var tenantId = _user.TenantId!.Value;

        var compra = new Compra
        {
            TenantId = tenantId,
            ProveedorId = request.ProveedorId,
            Fecha = request.Fecha ?? DateTime.UtcNow,
            MontoTotal = request.MontoTotal,
            Moneda = request.Moneda,
            CotizacionDolar = request.CotizacionDolar,
            MontoPagado = request.MontoPagado,
            Observaciones = request.Observaciones,
        };

        _db.Compras.Add(compra);

        // Procesar items
        foreach (var item in request.Items)
        {
            if (item.TipoItem == "equipo" && item.Equipo != null)
            {
                // Crear equipo en stock
                var eq = item.Equipo;
                var equipo = new Equipo
                {
                    TenantId = tenantId,
                    Marca = eq.Marca,
                    Modelo = eq.Modelo,
                    Capacidad = eq.Capacidad ?? "",
                    Color = eq.Color ?? "",
                    Imei = string.IsNullOrWhiteSpace(eq.Imei)
        ? $"TEMP-{Guid.NewGuid().ToString()[..8].ToUpper()}"
        : eq.Imei,
                    Imei2 = eq.Imei2,
                    Condicion = eq.Condicion,
                    BateriaPorcentaje = eq.BateriaPorcentaje,
                    PrecioCompra = item.PrecioUnitario,
                    MonedaCompra = item.Moneda,
                    CotizacionDolarCompra = request.CotizacionDolar,
                    PrecioVentaSugerido = eq.PrecioVentaSugerido ?? item.PrecioUnitario,
                    MonedaVenta = eq.MonedaVenta ?? item.Moneda,
                    ProveedorId = request.ProveedorId,
                    GarantiaMeses = eq.GarantiaMeses,
                    Observaciones = eq.Observaciones,
                };
                _db.Equipos.Add(equipo);
                _db.EquipoHistoriales.Add(new EquipoHistorial
                {
                    TenantId = tenantId,
                    EquipoId = equipo.Id,
                    EstadoAnterior = EstadoEquipo.EnStock,
                    EstadoNuevo = EstadoEquipo.EnStock,
                    Motivo = "Compra",
                    Detalle = $"Ingresado via compra a proveedor.",
                    UsuarioId = _user.UserId,
                });
                _db.CompraItems.Add(new CompraItem
                {
                    CompraId = compra.Id,
                    EquipoId = equipo.Id,
                    Cantidad = 1,
                    PrecioUnitario = item.PrecioUnitario,
                    Moneda = item.Moneda,
                });
            }
            else if (item.TipoItem == "producto" && item.ProductoId.HasValue)
            {
                // Sumar stock al producto existente
                var producto = await _db.Productos.FindAsync(item.ProductoId.Value)
                    ?? throw new NotFoundException("Producto", item.ProductoId.Value);

                var stockAnterior = producto.Stock;
                producto.Stock += item.Cantidad;

                _db.MovimientosStockProducto.Add(new MovimientoStockProducto
                {
                    TenantId = tenantId,
                    ProductoId = producto.Id,
                    Cantidad = item.Cantidad,
                    StockAnterior = stockAnterior,
                    StockNuevo = producto.Stock,
                    Motivo = "Compra a proveedor",
                    UsuarioId = _user.UserId,
                });

                _db.CompraItems.Add(new CompraItem
                {
                    CompraId = compra.Id,
                    ProductoId = producto.Id,
                    Cantidad = item.Cantidad,
                    PrecioUnitario = item.PrecioUnitario,
                    Moneda = item.Moneda,
                });
            }
        }

        // Registrar movimiento de caja si se pagó algo
        if (request.MontoPagado > 0 && request.FormaPago.HasValue)
        {
            _db.MovimientosCaja.Add(new MovimientoCaja
            {
                TenantId = tenantId,
                FormaPago = request.FormaPago.Value,
                Moneda = (int)request.Moneda,
                Monto = -request.MontoPagado, // negativo = egreso
                Concepto = $"Compra a {(request.ProveedorId.HasValue ? "proveedor" : "sin proveedor")}",
                CompraId = compra.Id,
                Fecha = compra.Fecha,
            });
        }

        await _db.SaveChangesAsync();
        return Ok(new { id = compra.Id });
    }

    // Registrar pago parcial a una compra
    [HttpPost("{id}/pagar")]
    public async Task<ActionResult> PagarCompra(Guid id, [FromBody] PagarCompraRequest request)
    {
        var compra = await _db.Compras.FindAsync(id)
            ?? throw new NotFoundException("Compra", id);

        var pendiente = compra.MontoTotal - compra.MontoPagado;
        if (request.Monto > pendiente)
            throw new AppException($"El monto supera el pendiente ({pendiente}).");

        compra.MontoPagado += request.Monto;

        _db.MovimientosCaja.Add(new MovimientoCaja
        {
            TenantId = compra.TenantId,
            FormaPago = request.FormaPago,
            Moneda = (int)compra.Moneda,
            Monto = -request.Monto,
            Concepto = $"Pago parcial compra",
            CompraId = compra.Id,
            Fecha = DateTime.UtcNow,
        });

        await _db.SaveChangesAsync();
        return Ok(new { montoPagado = compra.MontoPagado, montoPendiente = compra.MontoTotal - compra.MontoPagado });
    }

    [HttpGet("{id}")]
    public async Task<ActionResult> ObtenerPorId(Guid id)
    {
        var compra = await _db.Compras
            .Include(c => c.Proveedor)
            .Include(c => c.Items)
                .ThenInclude(i => i.Equipo)
            .Include(c => c.Items)
                .ThenInclude(i => i.Producto)
            .FirstOrDefaultAsync(c => c.Id == id)
            ?? throw new NotFoundException("Compra", id);

        return Ok(new
        {
            id = compra.Id,
            fecha = compra.Fecha,
            proveedor = compra.Proveedor?.Nombre,
            montoTotal = compra.MontoTotal,
            montoPagado = compra.MontoPagado,
            montoPendiente = compra.MontoTotal - compra.MontoPagado,
            moneda = compra.Moneda.ToString(),
            observaciones = compra.Observaciones,
            items = compra.Items.Select(i => new
            {
                tipoItem = i.EquipoId.HasValue ? "equipo" : "producto",
                descripcion = i.EquipoId.HasValue
                    ? $"{i.Equipo!.Marca} {i.Equipo.Modelo} {i.Equipo.Capacidad} — IMEI: {i.Equipo.Imei}"
                    : i.Producto?.Nombre,
                cantidad = i.Cantidad,
                precioUnitario = i.PrecioUnitario,
                moneda = i.Moneda.ToString(),
            }).ToList()
        });
    }
}

public record CrearCompraRequest(
    Guid? ProveedorId,
    DateTime? Fecha,
    decimal MontoTotal,
    Moneda Moneda,
    decimal CotizacionDolar,
    decimal MontoPagado,
    int? FormaPago,
    string? Observaciones,
    List<CompraItemRequest> Items
);

public record CompraItemRequest(
    string TipoItem,           // "equipo" o "producto"
    Guid? ProductoId,          // si es producto existente
    int Cantidad,
    decimal PrecioUnitario,
    Moneda Moneda,
    EquipoCompraRequest? Equipo // si es equipo nuevo
);

public record EquipoCompraRequest(
    string Marca,
    string Modelo,
    string? Capacidad,
    string? Color,
    string Imei,
    string? Imei2,
    CondicionEquipo Condicion,
    int? BateriaPorcentaje,
    decimal? PrecioVentaSugerido,
    Moneda? MonedaVenta,
    int? GarantiaMeses,
    string? Observaciones
);

public record PagarCompraRequest(decimal Monto, int FormaPago);