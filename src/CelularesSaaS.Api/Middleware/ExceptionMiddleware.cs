using System.Net;
using System.Text.Json;
using CelularesSaaS.Application.Common.Exceptions;

namespace CelularesSaaS.Api.Middleware;

public class ExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionMiddleware> _logger;

    public ExceptionMiddleware(RequestDelegate next, ILogger<ExceptionMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error no controlado: {Message}", ex.Message);
            await HandleExceptionAsync(context, ex);
        }
    }

    private static async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        context.Response.ContentType = "application/json";

        var (statusCode, message, errors) = exception switch
        {
            NotFoundException e => (e.StatusCode, e.Message, (object?)null),
            ValidationException e => (e.StatusCode, e.Message, (object?)e.Errors),
            UnauthorizedException e => (e.StatusCode, e.Message, (object?)null),
            ForbiddenException e => (e.StatusCode, e.Message, (object?)null),
            AppException e => (e.StatusCode, e.Message, (object?)null),
            _ => (500, "Error interno del servidor.", (object?)null)
        };

        context.Response.StatusCode = statusCode;

        var response = new { status = statusCode, message, errors };
        await context.Response.WriteAsync(JsonSerializer.Serialize(response,
            new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase }));
    }
}
