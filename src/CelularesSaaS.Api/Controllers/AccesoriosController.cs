using CelularesSaaS.Application.Accesorios.DTOs;
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
public class AccesoriosController : ControllerBase
{
    private readonly ApplicationDbContext _db;
    private readonly ICurrentUserService _user;

    public AccesoriosController(ApplicationDbContext db, ICurrentUserService user)
    {
        _db = db;
        _user = user;
    }

    [HttpGet]
    public async Task<ActionResult<List<AccesorioDto>>> Listar([FromQuery] string? busqueda)
    {
        var query = _db.Accesorios.AsQueryable();
        if (!string.IsNullOrWhiteSpace(busqueda))
            query = query.Where(a => a.Nombre.Contains(busqueda) || a.Codigo.Contains(busqueda));

        var items = await query.OrderBy(a => a.Nombre)
            .Select(a => new AccesorioDto(
                a.Id, a.Codigo, a.CodigoBarras, a.Nombre,
                a.Categoria, a.Marca, a.Compatibilidad,
                a.PrecioCompra, a.MonedaCompra,
                a.PrecioVenta, a.MonedaVenta,
                a.Stock, a.StockMinimo))
            .ToListAsync();

        return Ok(items);
    }

    [HttpPost]
    public async Task<ActionResult<AccesorioDto>> Crear([FromBody] CrearAccesorioRequest request)
    {
        var accesorio = new Accesorio
        {
            TenantId = _user.TenantId!.Value,
            Codigo = request.Codigo,
            CodigoBarras = request.CodigoBarras,
            Nombre = request.Nombre,
            Descripcion = request.Descripcion,
            Categoria = request.Categoria,
            Marca = request.Marca,
            Compatibilidad = request.Compatibilidad,
            PrecioCompra = request.PrecioCompra,
            MonedaCompra = request.MonedaCompra,
            PrecioVenta = request.PrecioVenta,
            MonedaVenta = request.MonedaVenta,
            Stock = request.Stock,
            StockMinimo = request.StockMinimo,
            ProveedorId = request.ProveedorId
        };

        _db.Accesorios.Add(accesorio);
        await _db.SaveChangesAsync();

        return Ok(new AccesorioDto(accesorio.Id, accesorio.Codigo, accesorio.CodigoBarras,
            accesorio.Nombre, accesorio.Categoria, accesorio.Marca, accesorio.Compatibilidad,
            accesorio.PrecioCompra, accesorio.MonedaCompra,
            accesorio.PrecioVenta, accesorio.MonedaVenta,
            accesorio.Stock, accesorio.StockMinimo));
    }
}
