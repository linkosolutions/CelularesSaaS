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
public class ProveedoresController : ControllerBase
{
    private readonly ApplicationDbContext _db;
    private readonly ICurrentUserService _user;

    public ProveedoresController(ApplicationDbContext db, ICurrentUserService user)
    {
        _db = db;
        _user = user;
    }

    [HttpGet]
    public async Task<ActionResult> Listar([FromQuery] string? busqueda)
    {
        var query = _db.Proveedores.AsQueryable();

        if (!string.IsNullOrWhiteSpace(busqueda))
            query = query.Where(p =>
                EF.Functions.ILike(p.Nombre, $"%{busqueda}%") ||
                (p.Telefono != null && p.Telefono.Contains(busqueda)));

        var proveedores = await query
            .OrderBy(p => p.Nombre)
            .Select(p => new
            {
                id = p.Id,
                nombre = p.Nombre,
                cuit = p.Cuit,
                telefono = p.Telefono,
                email = p.Email,
                direccion = p.Direccion,
                notas = p.Notas,
                cantidadEquipos = p.Equipos.Count(),
                cantidadAccesorios = p.Accesorios.Count(),
            })
            .ToListAsync();

        return Ok(proveedores);
    }

    [HttpPost]
    public async Task<ActionResult> Crear([FromBody] CrearProveedorRequest request)
    {
        var proveedor = new Proveedor
        {
            TenantId = _user.TenantId!.Value,
            Nombre = request.Nombre,
            Cuit = request.Cuit,
            Telefono = request.Telefono,
            Email = request.Email,
            Direccion = request.Direccion,
            Notas = request.Notas,
        };

        _db.Proveedores.Add(proveedor);
        await _db.SaveChangesAsync();
        return Ok(new { id = proveedor.Id });
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Actualizar(Guid id, [FromBody] CrearProveedorRequest request)
    {
        var proveedor = await _db.Proveedores.FindAsync(id)
            ?? throw new NotFoundException("Proveedor", id);

        proveedor.Nombre = request.Nombre;
        proveedor.Cuit = request.Cuit;
        proveedor.Telefono = request.Telefono;
        proveedor.Email = request.Email;
        proveedor.Direccion = request.Direccion;
        proveedor.Notas = request.Notas;

        await _db.SaveChangesAsync();
        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Eliminar(Guid id)
    {
        var proveedor = await _db.Proveedores.FindAsync(id)
            ?? throw new NotFoundException("Proveedor", id);

        var tieneEquipos = await _db.Equipos.AnyAsync(e => e.ProveedorId == id);
        if (tieneEquipos)
            throw new AppException("No podés eliminar un proveedor que tiene equipos asociados.");

        proveedor.Activo = false;
        await _db.SaveChangesAsync();
        return NoContent();
    }
}

public record CrearProveedorRequest(
    string Nombre,
    string? Cuit,
    string? Telefono,
    string? Email,
    string? Direccion,
    string? Notas
);