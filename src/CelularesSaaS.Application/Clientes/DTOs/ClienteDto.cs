namespace CelularesSaaS.Application.Clientes.DTOs;

public record ClienteDto(
    Guid Id,
    string NombreCompleto,
    string? Dni,
    string? Telefono,
    string? Email,
    string? Direccion,
    string? Notas,
    int TotalCompras,
    int TotalReparaciones
);

public record CrearClienteRequest(
    string NombreCompleto,
    string? Dni,
    string? Telefono,
    string? Email,
    string? Direccion,
    string? Notas
);
