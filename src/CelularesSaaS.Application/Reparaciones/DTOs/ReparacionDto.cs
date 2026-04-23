using CelularesSaaS.Domain.Enums;

namespace CelularesSaaS.Application.Reparaciones.DTOs;

public record ReparacionDto(
    Guid Id,
    string NumeroOrden,
    string? ImeiEquipo,
    string? MarcaModelo,
    string? ClienteNombre,
    string? TecnicoNombre,
    string ProblemaReportado,
    string? DiagnosticoTecnico,
    EstadoReparacion Estado,
    decimal? PresupuestoMonto,
    decimal? TotalCobrado,
    DateTime FechaIngreso,
    DateTime? FechaEntrega
);

public record CrearReparacionRequest(
    Guid? EquipoId,
    string? ImeiExterno,
    string? MarcaExterna,
    string? ModeloExterno,
    string? ColorExterno,
    Guid? ClienteId,
    string ProblemaReportado,
    Guid? TecnicoId
);

public record ActualizarReparacionRequest(
    EstadoReparacion Estado,
    string? DiagnosticoTecnico,
    string? TrabajoRealizado,
    decimal? PresupuestoMonto,
    decimal? CostoRepuestos,
    decimal? ManoDeObra,
    decimal? TotalCobrado,
    Moneda? Moneda,
    DateTime? FechaEntrega,
    int? GarantiaDias,
    string? Comentario
);
