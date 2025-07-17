using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using StarkAid.Api.Data;
using System;
using System.Threading.Tasks;

public class DeviceApiKeyMiddleware
{
    private const string ApiKeyHeaderName = "Api-Key";
    private readonly RequestDelegate _next;
    private readonly ILogger<DeviceApiKeyMiddleware> _logger;

    public DeviceApiKeyMiddleware(RequestDelegate next, ILogger<DeviceApiKeyMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context, AppDbContext db)
    {
        var path = context.Request.Path.Value?.ToLower() ?? string.Empty;
        var method = context.Request.Method.ToUpper();

        _logger.LogInformation("Requisição para {Path} via {Method}", path, method);

        // Ignora rota de pareamento de dispositivos
        if (context.Request.Path.StartsWithSegments("/api/devices/pair", StringComparison.OrdinalIgnoreCase))
        {
            await _next(context);
            return;
        }

        // Permite POST em /api/devices sem API Key
        if (context.Request.Path.Equals("/api/devices", StringComparison.OrdinalIgnoreCase) && method == "POST")
        {
            await _next(context);
            return;
        }

        // Se usuário autenticado via JWT, permite
        if (context.User.Identity?.IsAuthenticated == true)
        {
            _logger.LogInformation("Requisição autenticada via JWT.");
            await _next(context);
            return;
        }

        // Para rotas de dispositivos, exige ApiKey
        if (context.Request.Path.StartsWithSegments("/api/devices", StringComparison.OrdinalIgnoreCase))
        {
            if (!context.Request.Headers.TryGetValue(ApiKeyHeaderName, out var extractedApiKey))
            {
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                await context.Response.WriteAsync("API Key não fornecida.");
                return;
            }

            var device = await db.Devices
                .FirstOrDefaultAsync(d => d.ApiKey == extractedApiKey.ToString());

            if (device == null)
            {
                context.Response.StatusCode = StatusCodes.Status403Forbidden;
                await context.Response.WriteAsync("API Key inválida.");
                return;
            }
            // Se quiser armazenar no contexto para uso no controller, faz aqui
            context.Items["Device"] = device;

            _logger.LogInformation("Dispositivo {DeviceId} autenticado via API Key.", device.Id);
        }

        await _next(context);
    }
}
