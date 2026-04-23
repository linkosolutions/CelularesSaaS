using CelularesSaaS.Application.Common.Exceptions;
using CelularesSaaS.Application.Common.Interfaces;
using CelularesSaaS.Domain.Entities;
using CelularesSaaS.Domain.Enums;
using CelularesSaaS.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CelularesSaaS.Api.Controllers;

[Authorize(Roles = "SuperAdmin")]
[ApiController]
[Route("api/[controller]")]
public class SuperAdminController : ControllerBase
{
    private readonly ApplicationDbContext _db;

    public SuperAdminController(ApplicationDbContext db) => _db = db;

    // ── Listar todos los tenants ──
    [HttpGet("tenants")]
    public async Task<ActionResult> ListarTenants()
    {
        var tenants = await _db.Tenants
            .OrderByDescending(t => t.FechaCreacion)
            .Select(t => new
            {
                id                    = t.Id,
                nombre                = t.Nombre,
                nombreComercial       = t.NombreComercial,
                slug                  = t.Slug,
                plan                  = t.Plan,
                activo                = t.Activo,
                fechaCreacion         = t.FechaCreacion,
                fechaVencimientoPlan  = t.FechaVencimientoPlan,
                diasRestantes         = t.FechaVencimientoPlan.HasValue
                    ? (int)(t.FechaVencimientoPlan.Value - DateTime.UtcNow).TotalDays
                    : (int?)null,
                cantidadUsuarios      = _db.Usuarios.IgnoreQueryFilters()
                    .Count(u => u.TenantId == t.Id && u.Activo),
            })
            .ToListAsync();

        return Ok(tenants);
    }

    // ── Crear nuevo tenant con usuario admin ──
    [HttpPost("tenants")]
    public async Task<ActionResult> CrearTenant([FromBody] CrearTenantRequest request)
    {
        var slugExiste = await _db.Tenants.AnyAsync(t => t.Slug == request.Slug);
        if (slugExiste)
            throw new AppException($"El slug '{request.Slug}' ya está en uso.", 409);

        var tenant = new Tenant
        {
            Nombre          = request.Nombre,
            NombreComercial = request.NombreComercial,
            Slug            = request.Slug.ToLower().Trim(),
            Plan            = "Prueba",
            FechaVencimientoPlan = DateTime.UtcNow.AddDays(7),
        };

        _db.Tenants.Add(tenant);

        var usuario = new Usuario
        {
            TenantId       = tenant.Id,
            NombreCompleto = request.AdminNombre,
            Email          = request.AdminEmail,
            PasswordHash   = BCrypt.Net.BCrypt.HashPassword(request.AdminPassword),
            Rol            = RolUsuario.AdminTenant,
        };

        _db.Usuarios.Add(usuario);
        await _db.SaveChangesAsync();

        return Ok(new
        {
            tenantId  = tenant.Id,
            slug      = tenant.Slug,
            adminId   = usuario.Id,
            mensaje   = $"Tenant creado. Vence en 7 días ({tenant.FechaVencimientoPlan:dd/MM/yyyy}).",
        });
    }

    // ── Extender licencia ──
    [HttpPatch("tenants/{id}/licencia")]
    public async Task<ActionResult> ExtenderLicencia(Guid id, [FromBody] ExtenderLicenciaRequest request)
    {
        var tenant = await _db.Tenants.FindAsync(id)
            ?? throw new NotFoundException("Tenant", id);

        // Si ya venció, empezar desde hoy. Si no, sumar desde la fecha actual
        var base_ = tenant.FechaVencimientoPlan.HasValue && tenant.FechaVencimientoPlan > DateTime.UtcNow
            ? tenant.FechaVencimientoPlan.Value
            : DateTime.UtcNow;

        tenant.FechaVencimientoPlan = base_.AddDays(request.DiasAgregar);
        tenant.Plan = request.Plan ?? tenant.Plan;
        tenant.Activo = true;

        await _db.SaveChangesAsync();

        return Ok(new
        {
            nuevaFechaVencimiento = tenant.FechaVencimientoPlan,
            diasRestantes         = (int)(tenant.FechaVencimientoPlan.Value - DateTime.UtcNow).TotalDays,
            plan                  = tenant.Plan,
        });
    }

    // ── Desactivar tenant ──
    [HttpDelete("tenants/{id}")]
    public async Task<IActionResult> DesactivarTenant(Guid id)
    {
        var tenant = await _db.Tenants.FindAsync(id)
            ?? throw new NotFoundException("Tenant", id);

        tenant.Activo = false;
        await _db.SaveChangesAsync();
        return NoContent();
    }
}

public record CrearTenantRequest(
    string Nombre,
    string NombreComercial,
    string Slug,
    string AdminNombre,
    string AdminEmail,
    string AdminPassword
);

public record ExtenderLicenciaRequest(
    int DiasAgregar,
    string? Plan
);
