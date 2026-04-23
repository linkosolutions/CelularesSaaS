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
            // Crear tenant
            await _db.Database.ExecuteSqlRawAsync(@"
                INSERT INTO ""Tenants"" (""Id"", ""Nombre"", ""NombreComercial"", ""Slug"", ""Plan"", ""Activo"", ""FechaCreacion"")
                VALUES ('11111111-0000-0000-0000-000000000001', 'Mi Local', 'Mi Local de Celulares', 'milocal', 'Pro', true, NOW())
                ON CONFLICT DO NOTHING");

            // Crear usuario admin (password: Admin1234)
            await _db.Database.ExecuteSqlRawAsync(@"
                INSERT INTO ""Usuarios"" (""Id"", ""TenantId"", ""NombreCompleto"", ""Email"", ""PasswordHash"", ""Rol"", ""Activo"", ""FechaCreacion"")
                VALUES (
                    gen_random_uuid(),
                    '11111111-0000-0000-0000-000000000001',
                    'Administrador',
                    'admin@milocal.com',
                    '$2a$11$2xSRef/VYL3BPVwkiAo9LOJUp.K110nTCEfDuW4Jc93nReNbNGbwe',
                    2, true, NOW()
                ) ON CONFLICT DO NOTHING");

            // Crear superadmin (password: Admin1234)
            await _db.Database.ExecuteSqlRawAsync(@"
                INSERT INTO ""Usuarios"" (""Id"", ""TenantId"", ""NombreCompleto"", ""Email"", ""PasswordHash"", ""Rol"", ""Activo"", ""FechaCreacion"")
                VALUES (
                    gen_random_uuid(),
                    '11111111-0000-0000-0000-000000000001',
                    'Super Admin',
                    'superadmin@sistema.com',
                    '$2a$11$2xSRef/VYL3BPVwkiAo9LOJUp.K110nTCEfDuW4Jc93nReNbNGbwe',
                    1, true, NOW()
                ) ON CONFLICT DO NOTHING");

            return Ok(new { mensaje = "Setup completado. Tenant y usuarios creados." });
        }
        catch (Exception ex)
        {
            return Ok(new { error = ex.Message, inner = ex.InnerException?.Message });
        }
    }
}