// src/CelularesSaaS.Api/Controllers/MigracionController.cs
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
        await _db.Database.MigrateAsync();
        return Ok(new { mensaje = "Migraciones aplicadas correctamente." });
    }
}