using CelularesSaaS.Domain.Enums;

namespace CelularesSaaS.Application.Equipos.DTOs;

public record EquipoDto(
    Guid Id,
    string Marca,
    string Modelo,
    string Capacidad,
    string Color,
    string Imei,
    string? Imei2,
    CondicionEquipo Condicion,
    EstadoEquipo Estado,
    UbicacionEquipo Ubicacion,
    int? BateriaPorcentaje,
    decimal PrecioCompra,
    Moneda MonedaCompra,
    decimal PrecioVentaSugerido,
    Moneda MonedaVenta,
    string? Observaciones,
    int? GarantiaMeses,
    DateTime FechaIngreso,
    string? ProveedorNombre,
    string? ClienteOrigenNombre
);

public record CrearEquipoRequest(
    string Marca,
    string Modelo,
    string Capacidad,
    string Color,
    string Imei,
    string? Imei2,
    string? NumeroSerie,
    CondicionEquipo Condicion,
    UbicacionEquipo Ubicacion,
    int? BateriaPorcentaje,
    decimal PrecioCompra,
    Moneda MonedaCompra,
    decimal? CotizacionDolarCompra,
    decimal PrecioVentaSugerido,
    Moneda MonedaVenta,
    string? Observaciones,
    int? GarantiaMeses,
    Guid? ProveedorId,
    Guid? ClienteOrigenId
);

public record ActualizarEquipoRequest(
    string? Color,
    EstadoEquipo? Estado,
    UbicacionEquipo? Ubicacion,
    int? BateriaPorcentaje,
    decimal? PrecioVentaSugerido,
    Moneda? MonedaVenta,
    string? Observaciones,
    int? GarantiaMeses
);

public record EquipoListadoDto(
    Guid Id,
    string Marca,
    string Modelo,
    string Capacidad,
    string Color,
    string Imei,
    CondicionEquipo Condicion,
    EstadoEquipo Estado,
    UbicacionEquipo Ubicacion,
    decimal PrecioVentaSugerido,
    Moneda MonedaVenta,
    DateTime FechaIngreso
);
