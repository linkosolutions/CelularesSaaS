using CelularesSaaS.Application.Common.Exceptions;
using CelularesSaaS.Application.Common.Interfaces;
using CelularesSaaS.Application.Ventas.DTOs;
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
public class VentasController : ControllerBase
{
    private readonly ApplicationDbContext _db;
    private readonly ICurrentUserService _user;

    public VentasController(ApplicationDbContext db, ICurrentUserService user)
    {
        _db = db;
        _user = user;
    }

    [HttpGet]
    public async Task<ActionResult<List<VentaDto>>> Listar()
    {
        var ventas = await _db.Ventas
            .Include(v => v.Cliente)
            .Include(v => v.Items).ThenInclude(i => i.Equipo)
            .Include(v => v.Pagos)
            .Include(v => v.PartePago)
            .OrderByDescending(v => v.Fecha)
            .Select(v => new VentaDto(
                v.Id, v.NumeroVenta, v.Fecha,
                v.Cliente != null ? v.Cliente.NombreCompleto : null,
                v.Subtotal, v.Descuento, v.Total, v.MonedaTotal,
                v.CotizacionDolar, v.Ganancia, v.Anulada,
                v.EstadoVenta,
                v.Items.Select(i => new VentaItemDto(
                    i.Id, i.Tipo, i.Descripcion, i.Cantidad,
                    i.PrecioUnitario, i.CostoUnitario, i.Subtotal, i.Moneda,
                    i.Equipo != null ? i.Equipo.Imei : null)).ToList(),
                v.Pagos.Select(p => new PagoDto(
                    p.Id, p.FormaPago, p.Monto, p.Moneda,
                    p.MontoConvertido, p.Referencia)).ToList()))
            .ToListAsync();

        return Ok(ventas);
    }

    [HttpPost]
    public async Task<ActionResult> Crear([FromBody] CrearVentaRequest request)
    {
        var tenantId   = _user.TenantId!.Value;
        var cotizacion = request.CotizacionDolar;

        var ultimoNumero = await _db.Ventas
            .IgnoreQueryFilters()
            .Where(v => v.TenantId == tenantId)
            .CountAsync();
        var numeroVenta = $"V-{(ultimoNumero + 1):D6}";

        var venta = new Venta
        {
            TenantId        = tenantId,
            NumeroVenta     = numeroVenta,
            ClienteId       = request.ClienteId,
            MonedaTotal     = request.MonedaTotal,
            CotizacionDolar = cotizacion,
            Descuento       = request.Descuento,
            Observaciones   = request.Observaciones,
            Fecha           = DateTime.UtcNow,
            EstadoVenta     = EstadoVenta.Completada,
        };

        decimal costoTotal  = 0;
        decimal subtotalARS = 0;

        foreach (var itemReq in request.Items)
        {
            decimal precioUnitarioARS = 0;
            decimal costoUnitarioARS  = 0;
            string  descripcion       = "";

            if (itemReq.Tipo == TipoItemVenta.Equipo && itemReq.EquipoId.HasValue)
            {
                var equipo = await _db.Equipos.FindAsync(itemReq.EquipoId.Value)
                    ?? throw new NotFoundException("Equipo", itemReq.EquipoId.Value);

                if (equipo.Estado != EstadoEquipo.EnStock)
                    throw new AppException($"El equipo {equipo.Imei} no está en stock.");

                precioUnitarioARS = equipo.MonedaVenta == Moneda.USD
                    ? equipo.PrecioVentaSugerido * cotizacion
                    : equipo.PrecioVentaSugerido;

                costoUnitarioARS = equipo.MonedaCompra == Moneda.USD
                    ? equipo.PrecioCompra * (equipo.CotizacionDolarCompra ?? cotizacion)
                    : equipo.PrecioCompra;

                if (itemReq.PrecioUnitarioARS.HasValue)
                    precioUnitarioARS = itemReq.PrecioUnitarioARS.Value;

                descripcion = $"{equipo.Marca} {equipo.Modelo} {equipo.Capacidad} - IMEI: {equipo.Imei}";

                // Calcular garantía y estado final
                var estadoFinal = EstadoEquipo.Vendido;
                if (equipo.GarantiaMeses.HasValue && equipo.GarantiaMeses > 0)
                {
                    equipo.FechaVencimientoGarantia = DateTime.UtcNow.AddMonths(equipo.GarantiaMeses.Value);
                    estadoFinal = EstadoEquipo.EnGarantia;
                }
                equipo.Estado = estadoFinal;

                _db.EquipoHistoriales.Add(new EquipoHistorial
                {
                    TenantId = tenantId,
                    EquipoId = equipo.Id,
                    EstadoAnterior = EstadoEquipo.EnStock,
                    EstadoNuevo = estadoFinal,
                    Motivo = "Venta",
                    Detalle = numeroVenta,
                    UsuarioId = _user.UserId,
                });
            }
            else if (itemReq.Tipo == TipoItemVenta.Accesorio && itemReq.ProductoId.HasValue)
            {
                var prod = await _db.Productos.FindAsync(itemReq.ProductoId.Value)
                    ?? throw new NotFoundException("Producto", itemReq.ProductoId.Value);

                if (prod.Stock < itemReq.Cantidad)
                    throw new AppException($"Stock insuficiente de '{prod.Nombre}'. Disponible: {prod.Stock}.");

                precioUnitarioARS = itemReq.PrecioUnitarioARS ?? prod.PrecioVentaARS;
                costoUnitarioARS  = prod.PrecioCompraARS;
                descripcion       = prod.Nombre;
                prod.Stock       -= itemReq.Cantidad;

                _db.Set<MovimientoStockProducto>().Add(new MovimientoStockProducto
                {
                    TenantId      = tenantId,
                    ProductoId    = prod.Id,
                    Cantidad      = -itemReq.Cantidad,
                    StockAnterior = prod.Stock + itemReq.Cantidad,
                    StockNuevo    = prod.Stock,
                    Motivo        = "Venta",
                    VentaId       = venta.Id,
                    UsuarioId     = _user.UserId,
                });
            }

            var subtotalItem  = precioUnitarioARS * itemReq.Cantidad;
            subtotalARS      += subtotalItem;
            costoTotal       += costoUnitarioARS * itemReq.Cantidad;

            venta.Items.Add(new VentaItem
            {
                TenantId       = tenantId,
                Tipo           = itemReq.Tipo,
                EquipoId       = itemReq.EquipoId,
                ProductoId     = itemReq.ProductoId,
                AccesorioId    = null,
                Descripcion    = descripcion,
                Cantidad       = itemReq.Cantidad,
                PrecioUnitario = precioUnitarioARS,
                CostoUnitario  = costoUnitarioARS,
                Subtotal       = subtotalItem,
                Moneda         = Moneda.ARS,
            });
        }

        // ── Parte de pago ──
        decimal valorPartePagoARS = 0;
        if (request.PartePago != null)
        {
            var pp = request.PartePago;

            // Verificar que el IMEI no esté ya en stock
            var imeiExiste = await _db.Equipos.AnyAsync(e => e.Imei == pp.Imei);
            if (imeiExiste)
                throw new AppException($"El IMEI {pp.Imei} ya existe en el sistema.");

            valorPartePagoARS = pp.MonedaValor == Moneda.USD
                ? pp.ValorTomado * cotizacion
                : pp.ValorTomado;

            // Crear el equipo en stock automáticamente
            var equipoNuevo = new Equipo
            {
                TenantId              = tenantId,
                Marca                 = pp.Marca,
                Modelo                = pp.Modelo,
                Capacidad             = pp.Capacidad,
                Color                 = pp.Color,
                Imei                  = pp.Imei,
                Condicion             = CondicionEquipo.Usado,
                Estado                = EstadoEquipo.EnStock,
                Ubicacion             = UbicacionEquipo.Deposito,
                BateriaPorcentaje     = pp.BateriaPorcentaje,
                Observaciones         = pp.Observaciones,
                PrecioCompra          = valorPartePagoARS,
                MonedaCompra          = Moneda.ARS,
                CotizacionDolarCompra = cotizacion,
                PrecioVentaSugerido   = valorPartePagoARS,
                MonedaVenta           = Moneda.ARS,
                ClienteOrigenId       = request.ClienteId,
                FechaIngreso          = DateTime.UtcNow,
            };

            _db.Equipos.Add(equipoNuevo);

            _db.EquipoHistoriales.Add(new EquipoHistorial
            {
                TenantId       = tenantId,
                EquipoId       = equipoNuevo.Id,
                EstadoAnterior = EstadoEquipo.EnStock,
                EstadoNuevo    = EstadoEquipo.EnStock,
                Motivo         = "Ingreso por parte de pago",
                Detalle        = $"Parte de pago en venta {numeroVenta}",
                UsuarioId      = _user.UserId,
            });

            var partePago = new PartePago
            {
                TenantId         = tenantId,
                Marca            = pp.Marca,
                Modelo           = pp.Modelo,
                Capacidad        = pp.Capacidad,
                Color            = pp.Color,
                Imei             = pp.Imei,
                BateriaPorcentaje = pp.BateriaPorcentaje,
                Observaciones    = pp.Observaciones,
                ValorTomado      = pp.ValorTomado,
                MonedaValor      = pp.MonedaValor,
                CotizacionDolar  = cotizacion,
                EquipoGeneradoId = equipoNuevo.Id,
                ClienteId        = request.ClienteId,
            };

            _db.PartePagos.Add(partePago);
            venta.PartePago = partePago;
        }

        venta.Subtotal   = subtotalARS;
        venta.Total      = subtotalARS - request.Descuento - valorPartePagoARS;
        venta.CostoTotal = costoTotal;
        venta.Ganancia   = venta.Total - costoTotal + valorPartePagoARS;

        foreach (var pagoReq in request.Pagos)
        {
            var montoARS = pagoReq.Moneda == Moneda.USD
                ? pagoReq.Monto * cotizacion
                : pagoReq.Monto;

            venta.Pagos.Add(new Pago
            {
                TenantId        = tenantId,
                FormaPago       = pagoReq.FormaPago,
                Monto           = pagoReq.Monto,
                Moneda          = pagoReq.Moneda,
                CotizacionDolar = cotizacion,
                MontoConvertido = montoARS,
                Referencia      = pagoReq.Referencia,
            });
        }

        _db.Ventas.Add(venta);
        await _db.SaveChangesAsync();

        return Ok(new
        {
            id              = venta.Id,
            numeroVenta     = venta.NumeroVenta,
            totalARS        = venta.Total,
            totalUSD        = Math.Round(venta.Total / cotizacion, 2),
            ganancia        = venta.Ganancia,
            valorPartePago  = valorPartePagoARS,
        });
    }

    [HttpPost("{id}/anular")]
    public async Task<IActionResult> Anular(Guid id, [FromBody] string motivo)
    {
        var venta = await _db.Ventas
            .Include(v => v.Items).ThenInclude(i => i.Equipo)
            .FirstOrDefaultAsync(v => v.Id == id)
            ?? throw new NotFoundException("Venta", id);

        if (venta.Anulada)
            throw new AppException("La venta ya está anulada.");

        venta.Anulada         = true;
        venta.FechaAnulacion  = DateTime.UtcNow;
        venta.MotivoAnulacion = motivo;

        foreach (var item in venta.Items.Where(i => i.EquipoId.HasValue))
        {
            var equipo = item.Equipo;
            if (equipo != null)
            {
                equipo.Estado = EstadoEquipo.EnStock;
                _db.EquipoHistoriales.Add(new EquipoHistorial
                {
                    TenantId       = venta.TenantId,
                    EquipoId       = equipo.Id,
                    EstadoAnterior = EstadoEquipo.Vendido,
                    EstadoNuevo    = EstadoEquipo.EnStock,
                    Motivo         = "Anulación de venta",
                    Detalle        = motivo,
                    UsuarioId      = _user.UserId,
                });
            }
        }

        await _db.SaveChangesAsync();
        return NoContent();
    }

    [HttpPatch("{id}/estado")]
    public async Task<IActionResult> CambiarEstado(Guid id, [FromBody] EstadoVenta nuevoEstado)
    {
        var venta = await _db.Ventas.FindAsync(id)
            ?? throw new NotFoundException("Venta", id);

        if (venta.Anulada)
            throw new AppException("La venta está anulada y no se puede modificar.");

        venta.EstadoVenta = nuevoEstado;
        await _db.SaveChangesAsync();
        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Eliminar(Guid id)
    {
        var venta = await _db.Ventas
            .Include(v => v.Items).ThenInclude(i => i.Equipo)
            .FirstOrDefaultAsync(v => v.Id == id)
            ?? throw new NotFoundException("Venta", id);

        if (!venta.Anulada)
            throw new AppException("Primero anulá la venta antes de eliminarla.");

        _db.Ventas.Remove(venta);
        await _db.SaveChangesAsync();
        return NoContent();
    }
}
