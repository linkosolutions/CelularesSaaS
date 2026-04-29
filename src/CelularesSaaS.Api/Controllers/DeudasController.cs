using CelularesSaaS.Application.Common.Exceptions;
using CelularesSaaS.Application.Common.Interfaces;
using CelularesSaaS.Domain.Entities;
using CelularesSaaS.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CelularesSaaS.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class DeudasController : ControllerBase
{
    private readonly ApplicationDbContext _db;
    private readonly ICurrentUserService _user;

    public DeudasController(ApplicationDbContext db, ICurrentUserService user)
    {
        _db = db;
        _user = user;
    }

    [HttpGet]
    public async Task<IActionResult> Listar([FromQuery] string? estado, [FromQuery] Guid? clienteId)
    {
        var query = _db.Deudas
            .Include(d => d.Cliente)
            .Include(d => d.Cuotas.OrderBy(c => c.NumeroCuota))
            .Include(d => d.Venta)
            .AsQueryable();

        if (!string.IsNullOrEmpty(estado)) query = query.Where(d => d.Estado == estado);
        if (clienteId.HasValue) query = query.Where(d => d.ClienteId == clienteId);

        var deudas = await query.OrderByDescending(d => d.FechaCreacion).ToListAsync();

        return Ok(deudas.Select(d => new
        {
            id = d.Id,
            clienteNombre = d.Cliente?.NombreCompleto ?? "Cliente ocasional",
            clienteId = d.ClienteId,
            numeroVenta = d.Venta.NumeroVenta,
            ventaId = d.VentaId,
            montoOriginal = d.MontoOriginal,
            montoRestante = d.MontoRestante,
            interes = d.Interes,
            cantidadCuotas = d.CantidadCuotas,
            estado = d.Estado,
            observaciones = d.Observaciones,
            fechaCreacion = d.FechaCreacion,
            cuotas = d.Cuotas.Select(c => new
            {
                id = c.Id,
                numeroCuota = c.NumeroCuota,
                monto = c.Monto,
                montoPagado = c.MontoPagado,
                fechaVencimiento = c.FechaVencimiento,
                fechaPago = c.FechaPago,
                estado = c.Estado,
            })
        }));
    }

    [HttpPost]
    public async Task<IActionResult> Crear([FromBody] CrearDeudaRequest request)
    {
        var tenantId = _user.TenantId!.Value;

        var montoConInteres = request.MontoOriginal * (1 + request.Interes / 100);
        var montoPorCuota = Math.Round(montoConInteres / request.CantidadCuotas, 2);

        var deuda = new Deuda
        {
            TenantId = tenantId,
            ClienteId = request.ClienteId,
            VentaId = request.VentaId,
            MontoOriginal = request.MontoOriginal,
            MontoRestante = montoConInteres,
            Interes = request.Interes,
            CantidadCuotas = request.CantidadCuotas,
            Estado = "Pendiente",
            Observaciones = request.Observaciones,
        };

        for (int i = 0; i < request.CantidadCuotas; i++)
        {
            deuda.Cuotas.Add(new CuotaDeuda
            {
                TenantId = tenantId,
                NumeroCuota = i + 1,
                Monto = montoPorCuota,
                MontoPagado = 0,
                FechaVencimiento = request.FechasPorCuota != null && i < request.FechasPorCuota.Count
                    ? request.FechasPorCuota[i]
                    : DateTime.UtcNow.AddMonths(i + 1),
                Estado = "Pendiente",
            });
        }

        _db.Deudas.Add(deuda);
        await _db.SaveChangesAsync();
        return Ok(new { id = deuda.Id });
    }

    [HttpPost("{deudaId}/cuotas/{cuotaId}/pagar")]
    public async Task<IActionResult> PagarCuota(Guid deudaId, Guid cuotaId, [FromBody] PagarCuotaRequest request)
    {
        var deuda = await _db.Deudas
            .Include(d => d.Cuotas)
            .FirstOrDefaultAsync(d => d.Id == deudaId)
            ?? throw new NotFoundException("Deuda", deudaId);

        var cuota = deuda.Cuotas.FirstOrDefault(c => c.Id == cuotaId)
            ?? throw new NotFoundException("Cuota", cuotaId);

        cuota.MontoPagado += request.Monto;
        if (cuota.MontoPagado >= cuota.Monto)
        {
            cuota.MontoPagado = cuota.Monto;
            cuota.Estado = "Pagada";
            cuota.FechaPago = DateTime.UtcNow;
        }
        else
        {
            cuota.Estado = "PagoParcial";
        }

        deuda.MontoRestante -= request.Monto;
        if (deuda.MontoRestante <= 0)
        {
            deuda.MontoRestante = 0;
            deuda.Estado = "Saldada";
        }
        else if (deuda.Cuotas.Any(c => c.Estado is "Pagada" or "PagoParcial"))
        {
            deuda.Estado = "PagoParcial";
        }

        await _db.SaveChangesAsync();
        return Ok(new { montoRestante = deuda.MontoRestante, estadoDeuda = deuda.Estado });
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Eliminar(Guid id)
    {
        var deuda = await _db.Deudas.FindAsync(id)
            ?? throw new NotFoundException("Deuda", id);
        _db.Deudas.Remove(deuda);
        await _db.SaveChangesAsync();
        return NoContent();
    }
}

public record CrearDeudaRequest(
    Guid VentaId,
    Guid? ClienteId,
    decimal MontoOriginal,
    decimal Interes,
    int CantidadCuotas,
    List<DateTime>? FechasPorCuota,
    string? Observaciones
);

public record PagarCuotaRequest(decimal Monto);