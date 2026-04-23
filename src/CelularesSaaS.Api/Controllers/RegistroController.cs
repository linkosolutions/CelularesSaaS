using CelularesSaaS.Domain.Entities;
using CelularesSaaS.Domain.Enums;
using CelularesSaaS.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CelularesSaaS.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class RegistroController : ControllerBase
{
    private readonly ApplicationDbContext _db;

    public RegistroController(ApplicationDbContext db) => _db = db;

    [HttpPost]
    public async Task<ActionResult> Registrar([FromBody] RegistroRequest request)
    {
        // Validar email único
        var emailExiste = await _db.Usuarios
            .IgnoreQueryFilters()
            .AnyAsync(u => u.Email == request.Email);
        if (emailExiste)
            return BadRequest(new { message = "Ya existe una cuenta con ese email." });

        // Generar slug único desde el nombre del local
        var slugBase = request.NombreLocal
            .ToLower()
            .Replace(" ", "-")
            .Replace("á", "a").Replace("é", "e").Replace("í", "i")
            .Replace("ó", "o").Replace("ú", "u").Replace("ñ", "n");

        var slug = slugBase;
        var i = 1;
        while (await _db.Tenants.AnyAsync(t => t.Slug == slug))
            slug = $"{slugBase}-{i++}";

        // Crear tenant con 7 días de prueba
        var tenant = new Tenant
        {
            Nombre = request.NombreLocal,
            NombreComercial = request.NombreLocal,
            Slug = slug,
            Plan = "Prueba",
            Telefono = request.Telefono,
            Email = request.Email,
            FechaVencimientoPlan = DateTime.UtcNow.AddDays(7),
        };

        _db.Tenants.Add(tenant);

        // Crear usuario admin
        var usuario = new Usuario
        {
            TenantId = tenant.Id,
            NombreCompleto = request.NombreContacto,
            Email = request.Email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
            Rol = RolUsuario.AdminTenant,
        };

        _db.Usuarios.Add(usuario);
        await _db.SaveChangesAsync();

        return Ok(new
        {
            slug = tenant.Slug,
            mensaje = $"Cuenta creada. Tu local es: {tenant.Slug}. Tenés 7 días de prueba gratis.",
        });
    }
}

public record RegistroRequest(
    string NombreLocal,
    string NombreContacto,
    string Email,
    string Password,
    string? Telefono
);