namespace CelularesSaaS.Application.Reportes;

public record ResumenDashboardDto(
    decimal VentasHoy,
    decimal VentasMes,
    decimal GananciaHoy,
    decimal GananciaMes,
    int EquiposEnStock,
    int ReparacionesPendientes,
    int AccesoriosStockBajo,
    decimal CapitalInvertidoEquipos
);

public record VentaPorDiaDto(DateTime Fecha, decimal Total, decimal Ganancia, int CantidadVentas);

public record EquipoMasVendidoDto(string Marca, string Modelo, int CantidadVendida, decimal GananciaTotal);

public record StockEquipoDto(
    Guid Id,
    string Marca,
    string Modelo,
    string Capacidad,
    string Color,
    string Imei,
    string Condicion,
    string Ubicacion,
    decimal PrecioVentaSugerido,
    string MonedaVenta,
    int DiasEnStock
);
