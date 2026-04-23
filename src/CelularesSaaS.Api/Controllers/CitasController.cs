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
public class CitasController : ControllerBase
{
    private readonly ApplicationDbContext _db;
    private readonly ICurrentUserService _user;

    public CitasController(ApplicationDbContext db, ICurrentUserService user)
    {
        _db = db;
        _user = user;
    }

    [HttpGet]
    public async Task<ActionResult> Listar(
        [FromQuery] DateTime? desde,
        [FromQuery] DateTime? hasta,
        [FromQuery] EstadoCita? estado)
    {
        var query = _db.Citas
            .Include(c => c.Cliente)
            .Include(c => c.UsuarioAsignado)
            .AsQueryable();

        if (desde.HasValue) query = query.Where(c => c.FechaHora >= desde.Value);
        if (hasta.HasValue) query = query.Where(c => c.FechaHora <= hasta.Value);
        if (estado.HasValue) query = query.Where(c => c.Estado == estado.Value);

        var citas = await query
            .OrderBy(c => c.FechaHora)
            .Select(c => new
            {
                id = c.Id,
                fechaHora = c.FechaHora,
                motivo = c.Motivo,
                estado = c.Estado,
                notas = c.Notas,
                clienteId = c.ClienteId,
                clienteNombre = c.Cliente != null ? c.Cliente.NombreCompleto : c.NombreContacto,
                telefonoContacto = c.Cliente != null ? c.Cliente.Telefono : c.TelefonoContacto,
                nombreContacto = c.NombreContacto,
                usuarioAsignado = c.UsuarioAsignado != null ? c.UsuarioAsignado.NombreCompleto : null,
            })
            .ToListAsync();

        return Ok(citas);
    }

    [HttpPost]
    public async Task<ActionResult> Crear([FromBody] CrearCitaRequest request)
    {
        var cita = new Cita
        {
            TenantId = _user.TenantId!.Value,
            FechaHora = request.FechaHora,
            Motivo = request.Motivo,
            Estado = EstadoCita.Pendiente,
            Notas = request.Notas,
            ClienteId = request.ClienteId,
            NombreContacto = request.NombreContacto,
            TelefonoContacto = request.TelefonoContacto,
            UsuarioAsignadoId = request.UsuarioAsignadoId,
        };

        _db.Citas.Add(cita);
        await _db.SaveChangesAsync();
        return Ok(new { id = cita.Id });
    }

    [HttpPatch("{id}/estado")]
    public async Task<IActionResult> CambiarEstado(Guid id, [FromBody] EstadoCita nuevoEstado)
    {
        var cita = await _db.Citas.FindAsync(id)
            ?? throw new NotFoundException("Cita", id);

        cita.Estado = nuevoEstado;
        await _db.SaveChangesAsync();
        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Eliminar(Guid id)
    {
        var cita = await _db.Citas.FindAsync(id)
            ?? throw new NotFoundException("Cita", id);

        cita.Activo = false;
        await _db.SaveChangesAsync();
        return NoContent();
    }
}

public record CrearCitaRequest(
    DateTime FechaHora,
    string Motivo,
    string? Notas,
    Guid? ClienteId,
    string? NombreContacto,
    string? TelefonoContacto,
    Guid? UsuarioAsignadoId
);