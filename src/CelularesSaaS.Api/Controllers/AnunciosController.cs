using CelularesSaaS.Application.Common.Exceptions;
using CelularesSaaS.Application.Common.Interfaces;
using CelularesSaaS.Domain.Entities;
using CelularesSaaS.Domain.Enums;
using CelularesSaaS.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CelularesSaaS.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AnunciosController : ControllerBase
{
    private readonly ApplicationDbContext _db;
    private readonly ICurrentUserService _user;

    public AnunciosController(ApplicationDbContext db, ICurrentUserService user)
    {
        _db = db;
        _user = user;
    }

    // GET público — cualquier tenant autenticado puede ver el anuncio activo
    [Authorize]
    [HttpGet("activo")]
    public async Task<IActionResult> ObtenerActivo()
    {
        var anuncio = await _db.Anuncios
            .Where(a => a.Activo)
            .OrderByDescending(a => a.FechaCreacion)
            .FirstOrDefaultAsync();

        if (anuncio == null) return NoContent();

        return Ok(new
        {
            id = anuncio.Id,
            titulo = anuncio.Titulo,
            contenido = anuncio.Contenido,
            fecha = anuncio.FechaCreacion,
        });
    }

    // GET todos — solo SuperAdmin
    [Authorize]
    [HttpGet]
    public async Task<IActionResult> Listar()
    {
        EsSuperAdmin();
        var anuncios = await _db.Anuncios
            .OrderByDescending(a => a.FechaCreacion)
            .Select(a => new { a.Id, a.Titulo, a.Contenido, a.Activo, a.FechaCreacion })
            .ToListAsync();
        return Ok(anuncios);
    }

    // POST — solo SuperAdmin
    [Authorize]
    [HttpPost]
    public async Task<IActionResult> Crear([FromBody] AnuncioRequest request)
    {
        EsSuperAdmin();

        // Desactivar todos los anteriores
        var anteriores = await _db.Anuncios.Where(a => a.Activo).ToListAsync();
        anteriores.ForEach(a => a.Activo = false);

        var anuncio = new Anuncio
        {
            Titulo = request.Titulo,
            Contenido = request.Contenido,
            Activo = request.Activo,
        };

        _db.Anuncios.Add(anuncio);
        await _db.SaveChangesAsync();
        return Ok(new { id = anuncio.Id });
    }

    // PUT — solo SuperAdmin
    [Authorize]
    [HttpPut("{id}")]
    public async Task<IActionResult> Actualizar(Guid id, [FromBody] AnuncioRequest request)
    {
        EsSuperAdmin();

        var anuncio = await _db.Anuncios.FindAsync(id)
            ?? throw new NotFoundException("Anuncio", id);

        // Si se activa este, desactivar los demás
        if (request.Activo)
        {
            var otros = await _db.Anuncios.Where(a => a.Activo && a.Id != id).ToListAsync();
            otros.ForEach(a => a.Activo = false);
        }

        anuncio.Titulo = request.Titulo;
        anuncio.Contenido = request.Contenido;
        anuncio.Activo = request.Activo;

        await _db.SaveChangesAsync();
        return NoContent();
    }

    // DELETE — solo SuperAdmin
    [Authorize]
    [HttpDelete("{id}")]
    public async Task<IActionResult> Eliminar(Guid id)
    {
        EsSuperAdmin();
        var anuncio = await _db.Anuncios.FindAsync(id)
            ?? throw new NotFoundException("Anuncio", id);
        _db.Anuncios.Remove(anuncio);
        await _db.SaveChangesAsync();
        return NoContent();
    }

    private void EsSuperAdmin()
    {
        if (_user.Rol != "SuperAdmin")
            throw new ForbiddenException();
    }
}

public record AnuncioRequest(string Titulo, string Contenido, bool Activo, string Tipo);