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
public class PartePagosController : ControllerBase
{
    private readonly ApplicationDbContext _db;
    private readonly ICurrentUserService _user;

    public PartePagosController(ApplicationDbContext db, ICurrentUserService user)
    {
        _db = db;
        _user = user;
    }

    [HttpGet]
    public async Task<ActionResult> Listar()
    {
        var partes = await _db.PartePagos
            .Include(p => p.Cliente)
            .Include(p => p.EquipoGenerado)
            .OrderByDescending(p => p.FechaCreacion)
            .Select(p => new
            {
                id             = p.Id,
                marca          = p.Marca,
                modelo         = p.Modelo,
                capacidad      = p.Capacidad,
                color          = p.Color,
                imei           = p.Imei,
                valorTomado    = p.ValorTomado,
                monedaValor    = p.MonedaValor,
                cotizacionDolar = p.CotizacionDolar,
                clienteNombre  = p.Cliente != null ? p.Cliente.NombreCompleto : null,
                equipoGeneradoId = p.EquipoGeneradoId,
                ventaId        = p.VentaId,
                fechaCreacion  = p.FechaCreacion,
            })
            .ToListAsync();

        return Ok(partes);
    }
}
