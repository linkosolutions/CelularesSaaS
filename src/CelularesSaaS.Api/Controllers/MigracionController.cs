using CelularesSaaS.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CelularesSaaS.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class MigracionController : ControllerBase
{
    private readonly ApplicationDbContext _db;

    public MigracionController(ApplicationDbContext db) => _db = db;

    [HttpPost("aplicar")]
    public async Task<IActionResult> Aplicar()
    {
        try
        {
            await _db.Database.MigrateAsync();
            return Ok(new { mensaje = "Migraciones aplicadas correctamente." });
        }
        catch (Exception ex)
        {
            return Ok(new { error = ex.Message, inner = ex.InnerException?.Message });
        }
    }

    [HttpPost("sql")]
    public async Task<IActionResult> EjecutarSql([FromBody] string sql)
    {
        try
        {
            await _db.Database.ExecuteSqlRawAsync(sql);
            return Ok(new { mensaje = "SQL ejecutado." });
        }
        catch (Exception ex)
        {
            return Ok(new { error = ex.Message });
        }
    }

    [HttpPost("setup")]
    public async Task<IActionResult> Setup()
    {
        try
        {
            await _db.Database.ExecuteSqlRawAsync(@"
                DELETE FROM ""Usuarios"" WHERE ""TenantId"" = '11111111-0000-0000-0000-000000000001'");

            await _db.Database.ExecuteSqlRawAsync(@"
                INSERT INTO ""Tenants"" (""Id"", ""Nombre"", ""NombreComercial"", ""Slug"", ""Plan"", ""Activo"", ""FechaCreacion"")
                VALUES ('11111111-0000-0000-0000-000000000001', 'Mi Local', 'Mi Local de Celulares', 'milocal', 'Pro', true, NOW())
                ON CONFLICT DO NOTHING");

            await _db.Database.ExecuteSqlRawAsync(@"
                INSERT INTO ""Usuarios"" (""Id"", ""TenantId"", ""NombreCompleto"", ""Email"", ""PasswordHash"", ""Rol"", ""Activo"", ""FechaCreacion"")
                VALUES (
                    gen_random_uuid(),
                    '11111111-0000-0000-0000-000000000001',
                    'Administrador',
                    'admin@milocal.com',
                    '$2a$11$wop6Wz/1jKbbUBiCJYupLuVMYGXzlbwGjm9jAzVnoHOtRJnGxl1Z2',
                    2, true, NOW()
                )");

            await _db.Database.ExecuteSqlRawAsync(@"
                INSERT INTO ""Usuarios"" (""Id"", ""TenantId"", ""NombreCompleto"", ""Email"", ""PasswordHash"", ""Rol"", ""Activo"", ""FechaCreacion"")
                VALUES (
                    gen_random_uuid(),
                    '11111111-0000-0000-0000-000000000001',
                    'Super Admin',
                    'superadmin@sistema.com',
                    '$2a$11$wop6Wz/1jKbbUBiCJYupLuVMYGXzlbwGjm9jAzVnoHOtRJnGxl1Z2',
                    1, true, NOW()
                )");

            return Ok(new { mensaje = "Setup completado." });
        }
        catch (Exception ex)
        {
            return Ok(new { error = ex.Message, inner = ex.InnerException?.Message });
        }
    }

    [HttpGet("verificar")]
    public async Task<IActionResult> Verificar([FromQuery] string email, [FromQuery] string password)
    {
        var usuario = await _db.Usuarios
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(u => u.Email == email);

        if (usuario == null)
            return Ok(new { error = "Usuario no encontrado" });

        var ok = BCrypt.Net.BCrypt.Verify(password, usuario.PasswordHash);

        return Ok(new
        {
            email = usuario.Email,
            activo = usuario.Activo,
            tenantId = usuario.TenantId,
            hashGuardado = usuario.PasswordHash,
            passwordOk = ok,
        });
    }

    [HttpGet("tenant/{id}")]
    public async Task<IActionResult> ObtenerTenant(Guid id)
    {
        var tenant = await _db.Tenants
            .IgnoreQueryFilters()
            .Where(t => t.Id == id)
            .Select(t => new { t.Id, t.Nombre, t.Slug, t.Plan })
            .FirstOrDefaultAsync();
        return Ok(tenant);
    }
}