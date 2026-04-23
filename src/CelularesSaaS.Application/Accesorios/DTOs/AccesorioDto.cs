using CelularesSaaS.Domain.Enums;

namespace CelularesSaaS.Application.Accesorios.DTOs;

public record AccesorioDto(
    Guid Id,
    string Codigo,
    string? CodigoBarras,
    string Nombre,
    string? Categoria,
    string? Marca,
    string? Compatibilidad,
    decimal PrecioCompra,
    Moneda MonedaCompra,
    decimal PrecioVenta,
    Moneda MonedaVenta,
    int Stock,
    int StockMinimo
);

public record CrearAccesorioRequest(
    string Codigo,
    string? CodigoBarras,
    string Nombre,
    string? Descripcion,
    string? Categoria,
    string? Marca,
    string? Compatibilidad,
    decimal PrecioCompra,
    Moneda MonedaCompra,
    decimal PrecioVenta,
    Moneda MonedaVenta,
    int Stock,
    int StockMinimo,
    Guid? ProveedorId
);
