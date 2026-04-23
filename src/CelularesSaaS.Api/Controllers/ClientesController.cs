using CelularesSaaS.Application.Clientes.DTOs;
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
public class ClientesController : ControllerBase
{
    private readonly ApplicationDbContext _db;
    private readonly ICurrentUserService _user;

    public ClientesController(ApplicationDbContext db, ICurrentUserService user)
    {
        _db = db;
        _user = user;
    }

    [HttpGet]
    public async Task<ActionResult<List<ClienteDto>>> Listar([FromQuery] string? busqueda)
    {
        var query = _db.Clientes.AsQueryable();
        if (!string.IsNullOrWhiteSpace(busqueda))
            query = query.Where(c =>
                c.NombreCompleto.Contains(busqueda) ||
                (c.Dni != null && c.Dni.Contains(busqueda)) ||
                (c.Telefono != null && c.Telefono.Contains(busqueda)));

        var clientes = await query
            .OrderBy(c => c.NombreCompleto)
            .Select(c => new ClienteDto(
                c.Id, c.NombreCompleto, c.Dni, c.Telefono, c.Email, c.Direccion, c.Notas,
                c.Ventas.Count, c.Reparaciones.Count))
            .ToListAsync();

        return Ok(clientes);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<ClienteDto>> ObtenerPorId(Guid id)
    {
        var c = await _db.Clientes
            .Include(x => x.Ventas)
            .Include(x => x.Reparaciones)
            .FirstOrDefaultAsync(x => x.Id == id)
            ?? throw new NotFoundException("Cliente", id);

        return Ok(new ClienteDto(c.Id, c.NombreCompleto, c.Dni, c.Telefono,
            c.Email, c.Direccion, c.Notas, c.Ventas.Count, c.Reparaciones.Count));
    }

    [HttpPost]
    public async Task<ActionResult<ClienteDto>> Crear([FromBody] CrearClienteRequest request)
    {
        var cliente = new Cliente
        {
            TenantId = _user.TenantId!.Value,
            NombreCompleto = request.NombreCompleto,
            Dni = request.Dni,
            Telefono = request.Telefono,
            Email = request.Email,
            Direccion = request.Direccion,
            Notas = request.Notas
        };

        _db.Clientes.Add(cliente);
        await _db.SaveChangesAsync();

        return CreatedAtAction(nameof(ObtenerPorId), new { id = cliente.Id },
            new ClienteDto(cliente.Id, cliente.NombreCompleto, cliente.Dni,
                cliente.Telefono, cliente.Email, cliente.Direccion, cliente.Notas, 0, 0));
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<ClienteDto>> Actualizar(Guid id, [FromBody] CrearClienteRequest request)
    {
        var cliente = await _db.Clientes.FindAsync(id)
            ?? throw new NotFoundException("Cliente", id);

        cliente.NombreCompleto = request.NombreCompleto;
        cliente.Dni = request.Dni;
        cliente.Telefono = request.Telefono;
        cliente.Email = request.Email;
        cliente.Direccion = request.Direccion;
        cliente.Notas = request.Notas;

        await _db.SaveChangesAsync();
        return Ok(new ClienteDto(cliente.Id, cliente.NombreCompleto, cliente.Dni,
            cliente.Telefono, cliente.Email, cliente.Direccion, cliente.Notas, 0, 0));
    }
}
