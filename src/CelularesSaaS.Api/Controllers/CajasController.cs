using CelularesSaaS.Application.Common.Interfaces;
using CelularesSaaS.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CelularesSaaS.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class CajasController : ControllerBase
{
    private readonly ApplicationDbContext _db;
    private readonly ICurrentUserService _user;

    public CajasController(ApplicationDbContext db, ICurrentUserService user)
    {
        _db = db;
        _user = user;
    }

    [HttpGet("saldos")]
    public async Task<ActionResult> ObtenerSaldos()
    {
        var movimientos = await _db.MovimientosCaja.ToListAsync();

        var saldos = movimientos
            .GroupBy(m => new { m.FormaPago, m.Moneda })
            .Select(g => new
            {
                formaPago = g.Key.FormaPago,
                moneda = g.Key.Moneda,
                saldo = g.Sum(m => m.Monto),
            })
            .ToList();

        return Ok(saldos);
    }

    [HttpGet("movimientos")]
    public async Task<ActionResult> ObtenerMovimientos(
        [FromQuery] DateTime? desde,
        [FromQuery] DateTime? hasta,
        [FromQuery] int? formaPago)
    {
        var query = _db.MovimientosCaja.AsQueryable();

        if (desde.HasValue) query = query.Where(m => m.Fecha >= desde.Value);
        if (hasta.HasValue) query = query.Where(m => m.Fecha <= hasta.Value);
        if (formaPago.HasValue) query = query.Where(m => m.FormaPago == formaPago.Value);

        var movimientos = await query
            .OrderByDescending(m => m.Fecha)
            .Select(m => new
            {
                id = m.Id,
                fecha = m.Fecha,
                formaPago = m.FormaPago,
                moneda = m.Moneda,
                monto = m.Monto,
                concepto = m.Concepto,
                ventaId = m.VentaId,
                compraId = m.CompraId,
            })
            .ToListAsync();

        return Ok(movimientos);
    }
}