using CelularesSaaS.Domain.Enums;

namespace CelularesSaaS.Application.Ventas.DTOs;

public record VentaDto(
    Guid Id,
    string NumeroVenta,
    DateTime Fecha,
    string? ClienteNombre,
    decimal Subtotal,
    decimal Descuento,
    decimal Total,
    Moneda MonedaTotal,
    decimal CotizacionDolar,
    decimal Ganancia,
    bool Anulada,
    EstadoVenta EstadoVenta,  // ← nuevo
    List<VentaItemDto> Items,
    List<PagoDto> Pagos
);

public record VentaItemDto(
    Guid Id,
    TipoItemVenta Tipo,
    string Descripcion,
    int Cantidad,
    decimal PrecioUnitario,
    decimal CostoUnitario,
    decimal Subtotal,
    Moneda Moneda,
    string? Imei
);

public record PagoDto(
    Guid Id,
    FormaPago FormaPago,
    decimal Monto,
    Moneda Moneda,
    decimal MontoConvertido,
    string? Referencia
);

public record CrearVentaRequest(
    Guid? ClienteId,
    Moneda MonedaTotal,
    decimal CotizacionDolar,
    decimal Descuento,
    string? Observaciones,
    List<CrearVentaItemRequest> Items,
    List<CrearPagoRequest> Pagos,
    CrearPartePagoRequest? PartePago
);

public record CrearVentaItemRequest(
    TipoItemVenta Tipo,
    Guid? EquipoId,
    Guid? AccesorioId,
    Guid? ProductoId,
    int Cantidad,
    decimal PrecioUnitario,
    decimal? PrecioUnitarioARS
);

public record CrearPagoRequest(
    FormaPago FormaPago,
    decimal Monto,
    Moneda Moneda,
    decimal CotizacionDolar,
    string? Referencia
);

public record CrearPartePagoRequest(
    string Marca,
    string Modelo,
    string Capacidad,
    string Color,
    string Imei,
    int? BateriaPorcentaje,
    string? Observaciones,
    decimal ValorTomado,
    Moneda MonedaValor
);
