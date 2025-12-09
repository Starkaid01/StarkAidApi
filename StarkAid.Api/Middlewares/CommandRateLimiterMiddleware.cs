using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Memory;
using System.Security.Claims;
using System.Threading.Tasks;

namespace StarkAid.Api.Middlewares;

/// <summary>
/// Limita a quantidade de comandos MQTT por usuário (ex.: 5 comandos a cada 10 s).
/// </summary>
public class CommandRateLimiterMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IMemoryCache _cache;
    private const int Limit = 5;
    private static readonly TimeSpan Window = TimeSpan.FromSeconds(10);

    public CommandRateLimiterMiddleware(RequestDelegate next, IMemoryCache cache)
    {
        _next = next;
        _cache = cache;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var path = context.Request.Path.Value?.ToLowerInvariant() ?? string.Empty;

        if (context.User.Identity?.IsAuthenticated == true &&
            path.StartsWith("/api/commands/publish"))
        {
            var userId = context.User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!string.IsNullOrEmpty(userId))
            {
                var key = $"cmd_{userId}";
                var count = _cache.GetOrCreate(key, e =>
                {
                    e.AbsoluteExpirationRelativeToNow = Window;
                    return 0;
                });

                if (count >= Limit)
                {
                    context.Response.StatusCode = StatusCodes.Status429TooManyRequests;
                    await context.Response.WriteAsync("Limite de comandos excedido. Tente novamente em alguns segundos.");
                    return;
                }

                _cache.Set(key, count + 1, Window);
            }
        }

        await _next(context);
    }
}
