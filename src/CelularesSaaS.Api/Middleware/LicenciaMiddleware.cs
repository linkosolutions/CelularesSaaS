using CelularesSaaS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace CelularesSaaS.Api.Middleware;

public class LicenciaMiddleware
{
    private readonly RequestDelegate _next;

    public LicenciaMiddleware(RequestDelegate next) => _next = next;

    public async Task InvokeAsync(HttpContext context, ApplicationDbContext db)
    {
        // Solo aplica a rutas autenticadas que no sean superadmin o auth
        var path = context.Request.Path.Value ?? "";
        if (path.StartsWith("/api/superadmin") ||
            path.StartsWith("/api/auth") ||
            path.StartsWith("/api/dev"))
        {
            await _next(context);
            return;
        }

        var user = context.User;
        if (!user.Identity?.IsAuthenticated ?? true)
        {
            await _next(context);
            return;
        }

        // SuperAdmin no tiene restricción
        var rol = user.FindFirstValue(ClaimTypes.Role);
        if (rol == "SuperAdmin")
        {
            await _next(context);
            return;
        }

        var tenantIdClaim = user.FindFirstValue("tenantId");
        if (!Guid.TryParse(tenantIdClaim, out var tenantId))
        {
            await _next(context);
            return;
        }

        var tenant = await db.Tenants.FindAsync(tenantId);
        if (tenant == null || !tenant.Activo)
        {
            context.Response.StatusCode = 403;
            await context.Response.WriteAsJsonAsync(new
            {
                status  = 403,
                message = "Tenant inactivo.",
                codigo  = "TENANT_INACTIVO"
            });
            return;
        }

        // Si tiene fecha de vencimiento y ya venció, bloquear escrituras
        if (tenant.FechaVencimientoPlan.HasValue &&
            tenant.FechaVencimientoPlan.Value < DateTime.UtcNow)
        {
            var metodo = context.Request.Method;
            // GET permitido, todo lo demás bloqueado
            if (metodo != "GET")
            {
                context.Response.StatusCode = 402;
                await context.Response.WriteAsJsonAsync(new
                {
                    status  = 402,
                    message = "Tu período de prueba ha vencido. Contactá a soporte para renovar.",
                    codigo  = "LICENCIA_VENCIDA",
                    vencimiento = tenant.FechaVencimientoPlan
                });
                return;
            }
        }

        await _next(context);
    }
}
