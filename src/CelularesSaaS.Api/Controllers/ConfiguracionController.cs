using CelularesSaaS.Application.Common.Exceptions;
using CelularesSaaS.Application.Common.Interfaces;
using CelularesSaaS.Domain.Entities;
using CelularesSaaS.Domain.Enums;
using CelularesSaaS.Infrastructure.Persistence;
using CelularesSaaS.Infrastructure.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CelularesSaaS.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class ConfiguracionController : ControllerBase
{
    private readonly ApplicationDbContext _db;
    private readonly ICurrentUserService _user;
    private readonly ICloudinaryService _cloudinary;

    public ConfiguracionController(ApplicationDbContext db, ICurrentUserService user, ICloudinaryService cloudinary)
    {
        _db = db;
        _user = user;
        _cloudinary = cloudinary;
    }

    [HttpGet("negocio")]
    public async Task<ActionResult> ObtenerNegocio()
    {
        var tenant = await _db.Tenants.FindAsync(_user.TenantId)
            ?? throw new NotFoundException("Tenant", _user.TenantId!);

        return Ok(new
        {
            id = tenant.Id,
            nombre = tenant.Nombre,
            nombreComercial = tenant.NombreComercial,
            telefono = tenant.Telefono,
            email = tenant.Email,
            direccion = tenant.Direccion,
            logoUrl = tenant.LogoUrl,
            plan = tenant.Plan,
        });
    }

    [HttpPut("negocio")]
    public async Task<ActionResult> ActualizarNegocio([FromBody] ActualizarNegocioRequest request)
    {
        var tenant = await _db.Tenants.FindAsync(_user.TenantId)
            ?? throw new NotFoundException("Tenant", _user.TenantId!);

        tenant.Nombre = request.Nombre;
        tenant.NombreComercial = request.NombreComercial;
        tenant.Telefono = request.Telefono;
        tenant.Email = request.Email;
        tenant.Direccion = request.Direccion;

        await _db.SaveChangesAsync();
        return Ok();
    }

    [HttpPost("negocio/logo")]
    public async Task<ActionResult> SubirLogo(IFormFile archivo)
    {
        var tenant = await _db.Tenants.FindAsync(_user.TenantId)
            ?? throw new NotFoundException("Tenant", _user.TenantId!);

        if (archivo == null || archivo.Length == 0)
            throw new AppException("Archivo inválido.");

        var (url, publicId) = await _cloudinary.SubirImagenAsync(archivo, "logos");
        tenant.LogoUrl = url;
        await _db.SaveChangesAsync();

        return Ok(new { logoUrl = url });
    }

    [HttpGet("usuarios")]
    public async Task<ActionResult> ListarUsuarios()
    {
        var usuarios = await _db.Usuarios
            .Where(u => u.TenantId == _user.TenantId)
            .Select(u => new
            {
                id = u.Id,
                nombreCompleto = u.NombreCompleto,
                email = u.Email,
                rol = u.Rol.ToString(),
                rolNumero = u.Rol,
                ultimoLogin = u.UltimoLogin,
                activo = u.Activo,
            })
            .ToListAsync();

        return Ok(usuarios);
    }

    [HttpPost("usuarios")]
    public async Task<ActionResult> CrearUsuario([FromBody] CrearUsuarioRequest request)
    {
        var existe = await _db.Usuarios
            .AnyAsync(u => u.Email == request.Email && u.TenantId == _user.TenantId);

        if (existe)
            throw new AppException("Ya existe un usuario con ese email.");

        var usuario = new Usuario
        {
            TenantId = _user.TenantId!.Value,
            NombreCompleto = request.NombreCompleto,
            Email = request.Email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
            Rol = request.Rol,
        };

        _db.Usuarios.Add(usuario);
        await _db.SaveChangesAsync();

        return Ok(new { id = usuario.Id, mensaje = "Usuario creado correctamente." });
    }

    [HttpPatch("usuarios/{id}/rol")]
    public async Task<IActionResult> CambiarRol(Guid id, [FromBody] RolUsuario nuevoRol)
    {
        var usuario = await _db.Usuarios.FindAsync(id)
            ?? throw new NotFoundException("Usuario", id);

        if (usuario.TenantId != _user.TenantId)
            throw new ForbiddenException();

        usuario.Rol = nuevoRol;
        await _db.SaveChangesAsync();
        return NoContent();
    }

    [HttpDelete("usuarios/{id}")]
    public async Task<IActionResult> DesactivarUsuario(Guid id)
    {
        var usuario = await _db.Usuarios.FindAsync(id)
            ?? throw new NotFoundException("Usuario", id);

        if (usuario.TenantId != _user.TenantId)
            throw new ForbiddenException();

        if (usuario.Id == _user.UserId)
            throw new AppException("No podés desactivar tu propio usuario.");

        usuario.Activo = false;
        await _db.SaveChangesAsync();
        return NoContent();
    }
}

public record ActualizarNegocioRequest(
    string Nombre,
    string NombreComercial,
    string? Telefono,
    string? Email,
    string? Direccion
);

public record CrearUsuarioRequest(
    string NombreCompleto,
    string Email,
    string Password,
    RolUsuario Rol
);