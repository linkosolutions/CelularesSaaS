using CelularesSaaS.Application.Common.Exceptions;
using CelularesSaaS.Application.Common.Interfaces;
using CelularesSaaS.Application.Productos.DTOs;
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
public class ProductosController : ControllerBase
{
    private readonly ApplicationDbContext _db;
    private readonly ICurrentUserService _user;
    private readonly ICloudinaryService _cloudinary;

    public ProductosController(ApplicationDbContext db, ICurrentUserService user, ICloudinaryService cloudinary)
    {
        _db = db;
        _user = user;
        _cloudinary = cloudinary;
    }

    [HttpGet]
    public async Task<ActionResult<List<ProductoDto>>> Listar(
        [FromQuery] TipoProducto? tipo,
        [FromQuery] string? busqueda)
    {
        var query = _db.Productos.Include(p => p.Proveedor).AsQueryable();

        if (tipo.HasValue)
            query = query.Where(p => p.TipoProducto == tipo);

        if (!string.IsNullOrWhiteSpace(busqueda))
            query = query.Where(p =>
                p.Nombre.Contains(busqueda) ||
                p.Codigo.Contains(busqueda) ||
                (p.CodigoBarras != null && p.CodigoBarras.Contains(busqueda)) ||
                (p.Marca != null && p.Marca.Contains(busqueda)));

        var productos = await query
            .OrderBy(p => p.TipoProducto)
            .ThenBy(p => p.Nombre)
            .Select(p => new ProductoDto(
                p.Id, p.Codigo, p.CodigoBarras, p.Nombre, p.Descripcion,
                p.Marca, p.Modelo, p.Compatibilidad, p.TipoProducto,
                p.PrecioCompraARS, p.PrecioCompraUSD,
                p.PrecioVentaARS, p.PrecioVentaUSD,
                p.Stock, p.StockMinimo, p.ImagenUrl,
                p.Proveedor != null ? p.Proveedor.Nombre : null))
            .ToListAsync();

        return Ok(productos);
    }

    // Buscador unificado para ventas (celulares + productos)
    [HttpGet("buscar")]
    public async Task<ActionResult<List<BusquedaProductoDto>>> Buscar([FromQuery] string q)
    {
        if (string.IsNullOrWhiteSpace(q) || q.Length < 2)
            return Ok(new List<BusquedaProductoDto>());

        var resultados = new List<BusquedaProductoDto>();

        // Buscar en equipos
        var equipos = await _db.Equipos
    .Where(e => e.Estado == EstadoEquipo.EnStock && (
        EF.Functions.ILike(e.Imei, $"%{q}%") ||
        EF.Functions.ILike(e.Marca, $"%{q}%") ||
        EF.Functions.ILike(e.Modelo, $"%{q}%")))
    .Take(5)
    .ToListAsync();

        resultados.AddRange(equipos.Select(e => new BusquedaProductoDto(
            "equipo", e.Id,
            $"{e.Marca} {e.Modelo} {e.Capacidad} {e.Color}",
            null, e.Imei,
            e.MonedaVenta == Moneda.ARS ? e.PrecioVentaSugerido : e.PrecioVentaSugerido * 1200,
            e.MonedaVenta == Moneda.USD ? e.PrecioVentaSugerido : e.PrecioVentaSugerido / 1200,
            1, null)));

        // Buscar en productos
        var productos = await _db.Productos
    .Where(p => p.Stock > 0 && (
        EF.Functions.ILike(p.Nombre, $"%{q}%") ||
        EF.Functions.ILike(p.Codigo, $"%{q}%") ||
        (p.CodigoBarras != null && EF.Functions.ILike(p.CodigoBarras, $"%{q}%")) ||
        (p.Marca != null && EF.Functions.ILike(p.Marca, $"%{q}%"))))
    .Take(5)
    .ToListAsync();

        resultados.AddRange(productos.Select(p => new BusquedaProductoDto(
            "producto", p.Id,
            $"{p.Nombre}{(p.Marca != null ? " - " + p.Marca : "")}",
            p.CodigoBarras, null,
            p.PrecioVentaARS, p.PrecioVentaUSD,
            p.Stock, p.ImagenUrl)));

        return Ok(resultados);
    }

    [HttpPost]
    public async Task<ActionResult<ProductoDto>> Crear([FromBody] CrearProductoRequest request)
    {
        var existe = await _db.Productos.AnyAsync(p => p.Codigo == request.Codigo);
        if (existe) throw new AppException($"Ya existe un producto con código {request.Codigo}.", 409);

        var producto = new Producto
        {
            TenantId       = _user.TenantId!.Value,
            Codigo         = request.Codigo,
            CodigoBarras   = request.CodigoBarras,
            Nombre         = request.Nombre,
            Descripcion    = request.Descripcion,
            Marca          = request.Marca,
            Modelo         = request.Modelo,
            Compatibilidad = request.Compatibilidad,
            TipoProducto   = request.TipoProducto,
            PrecioCompraARS = request.PrecioCompraARS,
            PrecioCompraUSD = request.PrecioCompraUSD,
            PrecioVentaARS  = request.PrecioVentaARS,
            PrecioVentaUSD  = request.PrecioVentaUSD,
            Stock          = request.Stock,
            StockMinimo    = request.StockMinimo,
            ProveedorId    = request.ProveedorId,
        };

        _db.Productos.Add(producto);
        await _db.SaveChangesAsync();

        return Ok(MapToDto(producto));
    }

    // Upload de imagen
    [HttpPost("{id}/imagen")]
    public async Task<ActionResult> SubirImagen(Guid id, IFormFile archivo)
    {
        var producto = await _db.Productos.FindAsync(id)
            ?? throw new NotFoundException("Producto", id);

        if (archivo == null || archivo.Length == 0)
            throw new AppException("Archivo inválido.");

        // Eliminar imagen anterior si existe
        if (!string.IsNullOrEmpty(producto.ImagenPublicId))
            await _cloudinary.EliminarImagenAsync(producto.ImagenPublicId);

        var (url, publicId) = await _cloudinary.SubirImagenAsync(archivo, "productos");
        producto.ImagenUrl = url;
        producto.ImagenPublicId = publicId;

        await _db.SaveChangesAsync();
        return Ok(new { imagenUrl = url });
    }

    // Eliminar producto (soft delete)
    [HttpDelete("{id}")]
    public async Task<IActionResult> Eliminar(Guid id)
    {
        var producto = await _db.Productos.FindAsync(id)
            ?? throw new NotFoundException("Producto", id);

        if (producto.Stock > 0)
            throw new AppException("No podés eliminar un producto con stock. Ajustá el stock a 0 primero.");

        producto.Activo = false;
        await _db.SaveChangesAsync();
        return NoContent();
    }

    // Ajuste de stock manual
    [HttpPost("{id}/stock")]
    public async Task<IActionResult> AjustarStock(Guid id, [FromBody] ActualizarStockRequest request)
    {
        var producto = await _db.Productos.FindAsync(id)
            ?? throw new NotFoundException("Producto", id);

        var stockAnterior = producto.Stock;
        producto.Stock += request.Cantidad;

        if (producto.Stock < 0)
            throw new AppException("El stock no puede quedar negativo.");

        _db.Set<MovimientoStockProducto>().Add(new MovimientoStockProducto
        {
            TenantId      = producto.TenantId,
            ProductoId    = producto.Id,
            Cantidad      = request.Cantidad,
            StockAnterior = stockAnterior,
            StockNuevo    = producto.Stock,
            Motivo        = request.Motivo,
            UsuarioId     = _user.UserId,
        });

        await _db.SaveChangesAsync();
        return Ok(new { stockNuevo = producto.Stock });
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Actualizar(Guid id, [FromBody] ActualizarProductoRequest request)
    {
        var producto = await _db.Productos.FindAsync(id)
            ?? throw new NotFoundException("Producto", id);

        producto.Nombre = request.Nombre;
        producto.Descripcion = request.Descripcion;
        producto.Marca = request.Marca;
        producto.Modelo = request.Modelo;
        producto.Compatibilidad = request.Compatibilidad;
        producto.PrecioCompraARS = request.PrecioCompraARS;
        producto.PrecioCompraUSD = request.PrecioCompraUSD;
        producto.PrecioVentaARS = request.PrecioVentaARS;
        producto.PrecioVentaUSD = request.PrecioVentaUSD;
        producto.StockMinimo = request.StockMinimo;

        await _db.SaveChangesAsync();
        return Ok(MapToDto(producto));
    }

    private static ProductoDto MapToDto(Producto p) => new(
        p.Id, p.Codigo, p.CodigoBarras, p.Nombre, p.Descripcion,
        p.Marca, p.Modelo, p.Compatibilidad, p.TipoProducto,
        p.PrecioCompraARS, p.PrecioCompraUSD,
        p.PrecioVentaARS, p.PrecioVentaUSD,
        p.Stock, p.StockMinimo, p.ImagenUrl,
        p.Proveedor?.Nombre);

    public record ActualizarProductoRequest(
    string Nombre,
    string? Descripcion,
    string? Marca,
    string? Modelo,
    string? Compatibilidad,
    decimal PrecioCompraARS,
    decimal PrecioCompraUSD,
    decimal PrecioVentaARS,
    decimal PrecioVentaUSD,
    int StockMinimo
);
}
