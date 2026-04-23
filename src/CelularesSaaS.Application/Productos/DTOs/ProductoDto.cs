using CelularesSaaS.Domain.Enums;

namespace CelularesSaaS.Application.Productos.DTOs;

public record ProductoDto(
    Guid Id,
    string Codigo,
    string? CodigoBarras,
    string Nombre,
    string? Descripcion,
    string? Marca,
    string? Modelo,
    string? Compatibilidad,
    TipoProducto TipoProducto,
    decimal PrecioCompraARS,
    decimal PrecioCompraUSD,
    decimal PrecioVentaARS,
    decimal PrecioVentaUSD,
    int Stock,
    int StockMinimo,
    string? ImagenUrl,
    string? ProveedorNombre
);

public record CrearProductoRequest(
    string Codigo,
    string? CodigoBarras,
    string Nombre,
    string? Descripcion,
    string? Marca,
    string? Modelo,
    string? Compatibilidad,
    TipoProducto TipoProducto,
    decimal PrecioCompraARS,
    decimal PrecioCompraUSD,
    decimal PrecioVentaARS,
    decimal PrecioVentaUSD,
    int Stock,
    int StockMinimo,
    Guid? ProveedorId
);

public record ActualizarStockRequest(
    int Cantidad,
    string Motivo
);

// Para el buscador unificado en ventas
public record BusquedaProductoDto(
    string TipoItem,       // "equipo" | "producto"
    Guid Id,
    string Nombre,
    string? CodigoBarras,
    string? Imei,
    decimal PrecioVentaARS,
    decimal PrecioVentaUSD,
    int? Stock,
    string? ImagenUrl
);
