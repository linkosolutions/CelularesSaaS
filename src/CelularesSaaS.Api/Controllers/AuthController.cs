using CelularesSaaS.Application.Auth.DTOs;
using CelularesSaaS.Application.Common.Exceptions;
using CelularesSaaS.Application.Common.Interfaces;
using CelularesSaaS.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CelularesSaaS.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly ApplicationDbContext _db;
    private readonly IJwtService _jwt;

    public AuthController(ApplicationDbContext db, IJwtService jwt)
    {
        _db = db;
        _jwt = jwt;
    }

    [HttpPost("login")]
    public async Task<ActionResult<LoginResponse>> Login([FromBody] LoginRequest request)
    {
        var tenant = await _db.Tenants
            .FirstOrDefaultAsync(t => t.Slug == request.Slug.ToLower().Trim() && t.Activo);

        if (tenant == null)
            throw new UnauthorizedException("Local no encontrado.");

        var usuario = await _db.Usuarios
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(u => u.Email == request.Email
                && u.TenantId == tenant.Id && u.Activo);

        if (usuario == null || !BCrypt.Net.BCrypt.Verify(request.Password, usuario.PasswordHash))
            throw new UnauthorizedException("Credenciales incorrectas.");

        var accessToken  = _jwt.GenerateAccessToken(usuario);
        var refreshToken = _jwt.GenerateRefreshToken();

        usuario.RefreshToken       = refreshToken;
        usuario.RefreshTokenExpira = DateTime.UtcNow.AddDays(30);
        usuario.UltimoLogin        = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        // Calcular estado de licencia
        int? diasRestantes = tenant.FechaVencimientoPlan.HasValue
            ? (int)(tenant.FechaVencimientoPlan.Value - DateTime.UtcNow).TotalDays
            : null;
        bool licenciaVencida = tenant.FechaVencimientoPlan.HasValue
            && tenant.FechaVencimientoPlan.Value < DateTime.UtcNow;

        return Ok(new LoginResponse(
            accessToken,
            refreshToken,
            DateTime.UtcNow.AddHours(8),
            usuario.NombreCompleto,
            usuario.Email,
            usuario.Rol.ToString(),
            tenant.NombreComercial,
            tenant.Plan,
            tenant.FechaVencimientoPlan,
            diasRestantes,
            licenciaVencida
        ));
    }

    [HttpPost("refresh")]
    public async Task<ActionResult<LoginResponse>> Refresh([FromBody] string refreshToken)
    {
        var usuario = await _db.Usuarios
            .IgnoreQueryFilters()
            .Include(u => u.Tenant)
            .FirstOrDefaultAsync(u => u.RefreshToken == refreshToken
                && u.RefreshTokenExpira > DateTime.UtcNow);

        if (usuario == null)
            throw new UnauthorizedException("Refresh token inválido o expirado.");

        var newAccess  = _jwt.GenerateAccessToken(usuario);
        var newRefresh = _jwt.GenerateRefreshToken();

        usuario.RefreshToken       = newRefresh;
        usuario.RefreshTokenExpira = DateTime.UtcNow.AddDays(30);
        await _db.SaveChangesAsync();

        int? diasRestantes = usuario.Tenant.FechaVencimientoPlan.HasValue
            ? (int)(usuario.Tenant.FechaVencimientoPlan.Value - DateTime.UtcNow).TotalDays
            : null;
        bool licenciaVencida = usuario.Tenant.FechaVencimientoPlan.HasValue
            && usuario.Tenant.FechaVencimientoPlan.Value < DateTime.UtcNow;

        return Ok(new LoginResponse(
            newAccess, newRefresh, DateTime.UtcNow.AddHours(8),
            usuario.NombreCompleto, usuario.Email,
            usuario.Rol.ToString(), usuario.Tenant.NombreComercial,
            usuario.Tenant.Plan,
            usuario.Tenant.FechaVencimientoPlan,
            diasRestantes, licenciaVencida));
    }
}
