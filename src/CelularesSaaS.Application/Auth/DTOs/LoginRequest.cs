namespace CelularesSaaS.Application.Auth.DTOs;

public record LoginRequest(string Email, string Password, string Slug);
