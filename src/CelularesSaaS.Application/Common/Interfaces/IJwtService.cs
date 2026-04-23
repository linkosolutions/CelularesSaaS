using CelularesSaaS.Domain.Entities;

namespace CelularesSaaS.Application.Common.Interfaces;

public interface IJwtService
{
    string GenerateAccessToken(Usuario usuario);
    string GenerateRefreshToken();
    Guid? GetUserIdFromToken(string token);
}
