using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using StarkAid.Api.Data;

namespace StarkAid.Api.Middlewares;

/// <summary>
/// Valida a API‑Key enviada nos headers para rotas de dispositivos.
/// </summary>
public class DeviceApiKeyMiddleware
{
    private const string HeaderName = "Api-Key";
    private readonly RequestDelegate _next;
    private readonly ILogger<DeviceApiKeyMiddleware> _logger;

    public DeviceApiKeyMiddleware(RequestDelegate next, ILogger<DeviceApiKeyMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context, AppDbContext db)
    {
        // PathString permite StartsWithSegments
        var path = context.Request.Path;
        var method = context.Request.Method.ToUpperInvariant();

        // Rotas de pareamento (/pair) são públicas
        if (path.StartsWithSegments("/api/devices/pair"))
        {
            await _next(context);
            return;
        }

        // POST /api/devices (criação) aceita sem API‑Key – usa JWT
        if (path.Equals("/api/devices", StringComparison.OrdinalIgnoreCase) && method == "POST")
        {
            await _next(context);
            return;
        }

        // Rotas já protegidas por JWT passam antes deste middleware
        if (context.User.Identity?.IsAuthenticated == true)
        {
            await _next(context);
            return;
        }

        // Demais rotas de dispositivos exigem ApiKey
        if (path.StartsWithSegments("/api/devices"))
        {
            if (!context.Request.Headers.TryGetValue(HeaderName, out var apiKey))
            {
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                await context.Response.WriteAsync("API Key não informada.");
                return;
            }

            var device = await db.Devices.FirstOrDefaultAsync(d => d.ApiKey == apiKey);
            if (device == null)
            {
                context.Response.StatusCode = StatusCodes.Status403Forbidden;
                await context.Response.WriteAsync("API Key inválida.");
                return;
            }

            // Opcional: colocar o device no HttpContext.Items para uso nos controllers
            context.Items["Device"] = device;
        }

        await _next(context);
    }
}
