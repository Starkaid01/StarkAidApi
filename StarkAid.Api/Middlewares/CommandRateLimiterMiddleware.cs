namespace StarkAid.Api.Middlewares;

using Microsoft.Extensions.Caching.Memory;
using System.Security.Claims;

public class CommandRateLimiterMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IMemoryCache _cache;
    private const int LimitPerWindow = 5;
    private static readonly TimeSpan Window = TimeSpan.FromSeconds(10);

    public CommandRateLimiterMiddleware(RequestDelegate next, IMemoryCache cache)
    {
        _next = next;
        _cache = cache;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var path = context.Request.Path.Value?.ToLower() ?? string.Empty;

        // Só limita comandos MQTT
        if (context.User.Identity?.IsAuthenticated == true &&
            path.StartsWith("/api/commands/publish"))
        {
            var userId = context.User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (!string.IsNullOrEmpty(userId))
            {
                var cacheKey = $"CommandLimit_{userId}";
                var count = _cache.Get<int>(cacheKey);

                if (count >= LimitPerWindow)
                {
                    context.Response.StatusCode = StatusCodes.Status429TooManyRequests;
                    await context.Response.WriteAsync("Limite de comandos excedido. Tente novamente em instantes.");
                    return;
                }

                _cache.Set(cacheKey, count + 1, Window);
            }
        }

        await _next(context);
    }
}

