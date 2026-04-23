// Reemplazar en src/CelularesSaaS.Application/Auth/DTOs/LoginResponse.cs

namespace CelularesSaaS.Application.Auth.DTOs;

public record LoginResponse(
    string AccessToken,
    string RefreshToken,
    DateTime Expira,
    string NombreCompleto,
    string Email,
    string Rol,
    string TenantNombre,
    // Licencia
    string Plan,
    DateTime? FechaVencimientoPlan,
    int? DiasRestantes,
    bool LicenciaVencida
);
