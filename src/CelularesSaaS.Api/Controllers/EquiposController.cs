using CelularesSaaS.Application.Common.Exceptions;
using CelularesSaaS.Application.Common.Interfaces;
using CelularesSaaS.Application.Equipos.DTOs;
using CelularesSaaS.Domain.Entities;
using CelularesSaaS.Domain.Enums;
using CelularesSaaS.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CelularesSaaS.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class EquiposController : ControllerBase
{
    private readonly ApplicationDbContext _db;
    private readonly ICurrentUserService _user;

    public EquiposController(ApplicationDbContext db, ICurrentUserService user)
    {
        _db = db;
        _user = user;
    }

    [HttpGet]
    public async Task<ActionResult<List<EquipoListadoDto>>> Listar(
        [FromQuery] EstadoEquipo? estado,
        [FromQuery] CondicionEquipo? condicion,
        [FromQuery] UbicacionEquipo? ubicacion,
        [FromQuery] string? busqueda)
    {
        var query = _db.Equipos.AsQueryable();

        if (estado.HasValue) query = query.Where(e => e.Estado == estado);
        if (condicion.HasValue) query = query.Where(e => e.Condicion == condicion);
        if (ubicacion.HasValue) query = query.Where(e => e.Ubicacion == ubicacion);
        if (!string.IsNullOrWhiteSpace(busqueda))
            query = query.Where(e =>
                EF.Functions.ILike(e.Imei, $"%{busqueda}%") ||
                EF.Functions.ILike(e.Marca, $"%{busqueda}%") ||
                EF.Functions.ILike(e.Modelo, $"%{busqueda}%"));

        var equipos = await query
            .OrderByDescending(e => e.FechaCreacion)
            .Select(e => new EquipoListadoDto(
                e.Id, e.Marca, e.Modelo, e.Capacidad, e.Color, e.Imei,
                e.Condicion, e.Estado, e.Ubicacion,
                e.PrecioVentaSugerido, e.MonedaVenta, e.FechaIngreso))
            .ToListAsync();

        return Ok(equipos);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<EquipoDto>> ObtenerPorId(Guid id)
    {
        var e = await _db.Equipos
            .Include(x => x.Proveedor)
            .Include(x => x.ClienteOrigen)
            .Include(x => x.Historial)
            .FirstOrDefaultAsync(x => x.Id == id)
            ?? throw new NotFoundException("Equipo", id);

        return Ok(MapToDto(e));
    }

    [HttpGet("imei/{imei}")]
    public async Task<ActionResult<EquipoDto>> ObtenerPorImei(string imei)
    {
        var e = await _db.Equipos
            .Include(x => x.Proveedor)
            .Include(x => x.ClienteOrigen)
            .FirstOrDefaultAsync(x => x.Imei == imei)
            ?? throw new NotFoundException("Equipo", imei);

        return Ok(MapToDto(e));
    }

    [HttpPost]
    public async Task<ActionResult<EquipoDto>> Crear([FromBody] CrearEquipoRequest request)
    {
        var tenantId = _user.TenantId!.Value;

        var existe = await _db.Equipos.AnyAsync(e => e.Imei == request.Imei);
        if (existe)
            throw new AppException($"Ya existe un equipo con IMEI {request.Imei}.", 409);

        // Verificar límite de equipos según plan
        var tenant = await _db.Tenants.FindAsync(tenantId)
            ?? throw new NotFoundException("Tenant", tenantId);

        var limiteEquipos = tenant.Plan switch
        {
            "Prueba" => tenant.FechaVencimientoPlan.HasValue && tenant.FechaVencimientoPlan > DateTime.UtcNow ? int.MaxValue : 50,
            "Basico" => 200,
            "Pro" => int.MaxValue,
            "Enterprise" => int.MaxValue,
            _ => 50
        };

        var cantEquipos = await _db.Equipos
            .IgnoreQueryFilters()
            .CountAsync(e => e.TenantId == tenantId && e.Activo);

        if (cantEquipos >= limiteEquipos)
            throw new AppException($"Tu plan {tenant.Plan} permite hasta {limiteEquipos} equipos. Actualizá tu plan para continuar.");

        var equipo = new Equipo
        {
            TenantId = tenantId,
            Marca = request.Marca,
            Modelo = request.Modelo,
            Capacidad = request.Capacidad,
            Color = request.Color,
            Imei = request.Imei,
            Imei2 = request.Imei2,
            NumeroSerie = request.NumeroSerie,
            Condicion = request.Condicion,
            Ubicacion = request.Ubicacion,
            BateriaPorcentaje = request.BateriaPorcentaje,
            PrecioCompra = request.PrecioCompra,
            MonedaCompra = request.MonedaCompra,
            CotizacionDolarCompra = request.CotizacionDolarCompra,
            PrecioVentaSugerido = request.PrecioVentaSugerido,
            MonedaVenta = request.MonedaVenta,
            Observaciones = request.Observaciones,
            GarantiaMeses = request.GarantiaMeses,
            ProveedorId = request.ProveedorId,
            ClienteOrigenId = request.ClienteOrigenId,
        };

        _db.Equipos.Add(equipo);

        _db.EquipoHistoriales.Add(new EquipoHistorial
        {
            TenantId = equipo.TenantId,
            EquipoId = equipo.Id,
            EstadoAnterior = EstadoEquipo.EnStock,
            EstadoNuevo = EstadoEquipo.EnStock,
            Motivo = "Ingreso",
            Detalle = "Equipo dado de alta en el sistema.",
            UsuarioId = _user.UserId
        });

        await _db.SaveChangesAsync();
        return CreatedAtAction(nameof(ObtenerPorId), new { id = equipo.Id }, MapToDto(equipo));
    }

    [HttpPatch("{id}")]
    public async Task<ActionResult<EquipoDto>> Actualizar(Guid id, [FromBody] ActualizarEquipoRequest request)
    {
        var equipo = await _db.Equipos.FindAsync(id)
            ?? throw new NotFoundException("Equipo", id);

        var estadoAnterior = equipo.Estado;

        if (request.Color != null) equipo.Color = request.Color;
        if (request.Estado.HasValue) equipo.Estado = request.Estado.Value;
        if (request.Ubicacion.HasValue) equipo.Ubicacion = request.Ubicacion.Value;
        if (request.BateriaPorcentaje.HasValue) equipo.BateriaPorcentaje = request.BateriaPorcentaje;
        if (request.PrecioVentaSugerido.HasValue) equipo.PrecioVentaSugerido = request.PrecioVentaSugerido.Value;
        if (request.MonedaVenta.HasValue) equipo.MonedaVenta = request.MonedaVenta.Value;
        if (request.Observaciones != null) equipo.Observaciones = request.Observaciones;
        if (request.GarantiaMeses.HasValue) equipo.GarantiaMeses = request.GarantiaMeses;

        if (request.Estado.HasValue && request.Estado.Value != estadoAnterior)
        {
            _db.EquipoHistoriales.Add(new EquipoHistorial
            {
                TenantId = equipo.TenantId,
                EquipoId = equipo.Id,
                EstadoAnterior = estadoAnterior,
                EstadoNuevo = request.Estado.Value,
                Motivo = "Actualización manual",
                UsuarioId = _user.UserId
            });
        }

        await _db.SaveChangesAsync();
        return Ok(MapToDto(equipo));
    }

    [HttpGet("{id}/historial")]
    public async Task<ActionResult> ObtenerHistorial(Guid id)
    {
        var equipo = await _db.Equipos
            .FirstOrDefaultAsync(e => e.Id == id)
            ?? throw new NotFoundException("Equipo", id);

        var historial = await _db.EquipoHistoriales
            .Include(h => h.Usuario)
            .Where(h => h.EquipoId == id)
            .OrderByDescending(h => h.FechaCreacion)
            .Select(h => new
            {
                id = h.Id,
                estadoAnterior = h.EstadoAnterior.ToString(),
                estadoNuevo = h.EstadoNuevo.ToString(),
                motivo = h.Motivo,
                detalle = h.Detalle,
                usuario = h.Usuario != null ? h.Usuario.NombreCompleto : null,
                fecha = h.FechaCreacion,
            })
            .ToListAsync();

        return Ok(new
        {
            equipo = new
            {
                id = equipo.Id,
                marca = equipo.Marca,
                modelo = equipo.Modelo,
                capacidad = equipo.Capacidad,
                color = equipo.Color,
                imei = equipo.Imei,
                estado = equipo.Estado.ToString(),
                condicion = equipo.Condicion.ToString(),
                garantiaMeses = equipo.GarantiaMeses,
                fechaVencimientoGarantia = equipo.FechaVencimientoGarantia,
            },
            historial
        });
    }

    [HttpGet("historial/buscar")]
    public async Task<ActionResult> BuscarHistorialPorImei([FromQuery] string imei)
    {
        var equipo = await _db.Equipos
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(e => EF.Functions.ILike(e.Imei, $"%{imei}%") && e.TenantId == _user.TenantId)
            ?? throw new NotFoundException("Equipo con IMEI", imei);

        var historial = await _db.EquipoHistoriales
            .IgnoreQueryFilters()
            .Include(h => h.Usuario)
            .Where(h => h.EquipoId == equipo.Id)
            .OrderByDescending(h => h.FechaCreacion)
            .Select(h => new
            {
                id = h.Id,
                estadoAnterior = h.EstadoAnterior.ToString(),
                estadoNuevo = h.EstadoNuevo.ToString(),
                motivo = h.Motivo,
                detalle = h.Detalle,
                usuario = h.Usuario != null ? h.Usuario.NombreCompleto : null,
                fecha = h.FechaCreacion,
            })
            .ToListAsync();

        return Ok(new
        {
            equipo = new
            {
                id = equipo.Id,
                marca = equipo.Marca,
                modelo = equipo.Modelo,
                capacidad = equipo.Capacidad,
                color = equipo.Color,
                imei = equipo.Imei,
                estado = equipo.Estado.ToString(),
                condicion = equipo.Condicion.ToString(),
                garantiaMeses = equipo.GarantiaMeses,
                fechaVencimientoGarantia = equipo.FechaVencimientoGarantia,
            },
            historial
        });
    }

    [HttpGet("debug")]
    public IActionResult Debug()
    {
        return Ok(new
        {
            tenantId = _user.TenantId,
            userId = _user.UserId,
            isAuthenticated = _user.IsAuthenticated,
        });
    }

    private static EquipoDto MapToDto(Equipo e) => new(
        e.Id, e.Marca, e.Modelo, e.Capacidad, e.Color, e.Imei, e.Imei2,
        e.Condicion, e.Estado, e.Ubicacion, e.BateriaPorcentaje,
        e.PrecioCompra, e.MonedaCompra, e.PrecioVentaSugerido, e.MonedaVenta,
        e.Observaciones, e.GarantiaMeses, e.FechaIngreso,
        e.Proveedor?.Nombre, e.ClienteOrigen?.NombreCompleto);

    [HttpGet("todos")]
    public async Task<IActionResult> Todos()
    {
        var equipos = await _db.Equipos
            .IgnoreQueryFilters()
            .Where(e => e.TenantId == _user.TenantId)
            .Select(e => new { e.Id, e.Marca, e.TenantId, e.Activo, e.Estado })
            .ToListAsync();
        return Ok(equipos);
    }
}