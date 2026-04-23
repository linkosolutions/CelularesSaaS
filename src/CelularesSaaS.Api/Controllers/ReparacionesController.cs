using CelularesSaaS.Application.Common.Exceptions;
using CelularesSaaS.Application.Common.Interfaces;
using CelularesSaaS.Application.Reparaciones.DTOs;
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
public class ReparacionesController : ControllerBase
{
    private readonly ApplicationDbContext _db;
    private readonly ICurrentUserService _user;

    public ReparacionesController(ApplicationDbContext db, ICurrentUserService user)
    {
        _db = db;
        _user = user;
    }

    [HttpGet]
    public async Task<ActionResult<List<ReparacionDto>>> Listar([FromQuery] EstadoReparacion? estado)
    {
        var query = _db.Reparaciones
            .Include(r => r.Cliente)
            .Include(r => r.Equipo)
            .Include(r => r.Tecnico)
            .AsQueryable();

        if (estado.HasValue) query = query.Where(r => r.Estado == estado);

        var reparaciones = await query.OrderByDescending(r => r.FechaCreacion)
            .Select(r => new ReparacionDto(
                r.Id, r.NumeroOrden,
                r.Equipo != null ? r.Equipo.Imei : r.ImeiExterno,
                r.Equipo != null ? $"{r.Equipo.Marca} {r.Equipo.Modelo}" : $"{r.MarcaExterna} {r.ModeloExterno}",
                r.Cliente != null ? r.Cliente.NombreCompleto : null,
                r.Tecnico != null ? r.Tecnico.NombreCompleto : null,
                r.ProblemaReportado, r.DiagnosticoTecnico,
                r.Estado, r.PresupuestoMonto, r.TotalCobrado,
                r.FechaIngreso, r.FechaEntrega))
            .ToListAsync();

        return Ok(reparaciones);
    }

    [HttpPost]
    public async Task<ActionResult<ReparacionDto>> Crear([FromBody] CrearReparacionRequest request)
    {
        var tenantId = _user.TenantId!.Value;

        // Generar número de orden correlativo
        var ultimoNumero = await _db.Reparaciones
            .IgnoreQueryFilters()
            .Where(r => r.TenantId == tenantId)
            .CountAsync();
        var numeroOrden = $"R-{(ultimoNumero + 1):D6}";

        var reparacion = new Reparacion
        {
            TenantId = tenantId,
            NumeroOrden = numeroOrden,
            EquipoId = request.EquipoId,
            ImeiExterno = request.ImeiExterno,
            MarcaExterna = request.MarcaExterna,
            ModeloExterno = request.ModeloExterno,
            ColorExterno = request.ColorExterno,
            ClienteId = request.ClienteId,
            ProblemaReportado = request.ProblemaReportado,
            TecnicoId = request.TecnicoId,
            Estado = EstadoReparacion.Ingresado,
        };

        _db.Reparaciones.Add(reparacion);

        // Si el equipo es nuestro, cambiarle el estado
        if (request.EquipoId.HasValue)
        {
            var equipo = await _db.Equipos.FindAsync(request.EquipoId.Value);
            if (equipo != null)
            {
                equipo.Estado = EstadoEquipo.EnReparacion;
                _db.EquipoHistoriales.Add(new EquipoHistorial
                {
                    TenantId = tenantId,
                    EquipoId = equipo.Id,
                    EstadoAnterior = EstadoEquipo.EnStock,
                    EstadoNuevo = EstadoEquipo.EnReparacion,
                    Motivo = "Ingreso a reparación",
                    Detalle = numeroOrden,
                    UsuarioId = _user.UserId
                });
            }
        }

        await _db.SaveChangesAsync();

        return CreatedAtAction(nameof(Listar), new { id = reparacion.Id },
            new ReparacionDto(reparacion.Id, reparacion.NumeroOrden,
                null, null, null, null, reparacion.ProblemaReportado,
                null, reparacion.Estado, null, null, reparacion.FechaIngreso, null));
    }

    [HttpPatch("{id}")]
    public async Task<IActionResult> Actualizar(Guid id, [FromBody] ActualizarReparacionRequest request)
    {
        var reparacion = await _db.Reparaciones.FindAsync(id)
            ?? throw new NotFoundException("Reparacion", id);

        var estadoAnterior = reparacion.Estado;

        reparacion.Estado = request.Estado;
        if (request.DiagnosticoTecnico != null) reparacion.DiagnosticoTecnico = request.DiagnosticoTecnico;
        if (request.TrabajoRealizado != null) reparacion.TrabajoRealizado = request.TrabajoRealizado;
        if (request.PresupuestoMonto.HasValue) reparacion.PresupuestoMonto = request.PresupuestoMonto;
        if (request.CostoRepuestos.HasValue) reparacion.CostoRepuestos = request.CostoRepuestos;
        if (request.ManoDeObra.HasValue) reparacion.ManoDeObra = request.ManoDeObra;
        if (request.TotalCobrado.HasValue) reparacion.TotalCobrado = request.TotalCobrado;
        if (request.Moneda.HasValue) reparacion.Moneda = request.Moneda;
        if (request.FechaEntrega.HasValue) reparacion.FechaEntrega = request.FechaEntrega;
        if (request.GarantiaDias.HasValue) reparacion.GarantiaDias = request.GarantiaDias;

        if (request.Estado != estadoAnterior)
        {
            _db.ReparacionHistoriales.Add(new ReparacionHistorial
            {
                TenantId = reparacion.TenantId,
                ReparacionId = reparacion.Id,
                EstadoAnterior = estadoAnterior,
                EstadoNuevo = request.Estado,
                Comentario = request.Comentario,
                UsuarioId = _user.UserId
            });

            // Si se entrega el equipo, vuelve al stock
            if (request.Estado == EstadoReparacion.Entregado && reparacion.EquipoId.HasValue)
            {
                var equipo = await _db.Equipos.FindAsync(reparacion.EquipoId.Value);
                if (equipo != null)
                {
                    equipo.Estado = EstadoEquipo.EnStock;
                    _db.EquipoHistoriales.Add(new EquipoHistorial
                    {
                        TenantId = equipo.TenantId,
                        EquipoId = equipo.Id,
                        EstadoAnterior = EstadoEquipo.EnReparacion,
                        EstadoNuevo = EstadoEquipo.EnStock,
                        Motivo = "Reparación finalizada",
                        UsuarioId = _user.UserId
                    });
                }
            }
        }

        await _db.SaveChangesAsync();
        return NoContent();
    }
}
