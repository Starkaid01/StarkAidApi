using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Caching.Memory;
using StarkAid.Api.DTOs.Config;

namespace StarkAid.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ConfigController : ControllerBase
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<ConfigController> _logger;
    private readonly IMemoryCache _cache;
    private const string CacheKey = "AppConfig";
    private static readonly TimeSpan CacheExpiration = TimeSpan.FromMinutes(5); // Cache de 5 minutos

    public ConfigController(
        IConfiguration configuration, 
        ILogger<ConfigController> logger,
        IMemoryCache cache)
    {
        _configuration = configuration;
        _logger = logger;
        _cache = cache;
    }

    /// <summary>
    /// Retorna configurações públicas necessárias para os aplicativos cliente funcionarem.
    /// Não requer autenticação, mas está protegido por rate limiting (90 req/min por IP).
    /// Respostas são cacheadas por 5 minutos para melhor performance.
    /// </summary>
    [HttpGet("app-config")]
    [AllowAnonymous]
    [EnableRateLimiting("ConfigEndpoint")]
    public IActionResult GetAppConfig()
    {
        try
        {
            // Verificar cache primeiro (se disponível)
            if (_cache != null && _cache.TryGetValue(CacheKey, out AppConfigDto? cachedConfig) && cachedConfig != null)
            {
                _logger.LogDebug("Retornando configuração do cache");
                return Ok(cachedConfig);
            }

            // Obter base URL da API (pode vir de configuração ou do request)
            var apiBaseUrl = _configuration["ApiBaseUrl"] 
                ?? $"{Request.Scheme}://{Request.Host}";

            // Remover /api/ do final se existir, pois os apps precisam da URL base
            if (!string.IsNullOrEmpty(apiBaseUrl))
            {
                if (apiBaseUrl.EndsWith("/api/", StringComparison.OrdinalIgnoreCase))
                {
                    apiBaseUrl = apiBaseUrl.Substring(0, apiBaseUrl.Length - 4);
                }
                else if (apiBaseUrl.EndsWith("/api", StringComparison.OrdinalIgnoreCase))
                {
                    apiBaseUrl = apiBaseUrl.Substring(0, apiBaseUrl.Length - 3);
                }
            }

            var config = new AppConfigDto
            {
                ApiBaseUrl = apiBaseUrl?.TrimEnd('/') ?? $"{Request.Scheme}://{Request.Host}",
                Spotify = new SpotifyConfigDto
                {
                    ClientId = _configuration["Spotify:ClientId"] ?? string.Empty,
                    ClientSecret = _configuration["Spotify:ClientSecret"] ?? string.Empty,
                    TokenUrl = "https://accounts.spotify.com/api/token"
                },
                Ewelink = new EwelinkConfigDto
                {
                    ClientId = _configuration["Ewelink:ClientId"] ?? string.Empty,
                    ClientSecret = _configuration["Ewelink:ClientSecret"] ?? string.Empty,
                    RedirectUri = _configuration["Ewelink:RedirectUri"] 
                        ?? $"{Request.Scheme}://{Request.Host}/auth/ewelink/callback.html"
                }
            };

            // Armazenar no cache (se disponível)
            try
            {
                if (_cache != null)
                {
                    var cacheOptions = new MemoryCacheEntryOptions
                    {
                        AbsoluteExpirationRelativeToNow = CacheExpiration
                    };
                    _cache.Set(CacheKey, config, cacheOptions);
                }
            }
            catch (Exception cacheEx)
            {
                _logger.LogWarning(cacheEx, "Erro ao armazenar no cache, continuando sem cache");
            }

            _logger.LogInformation("Configuração do app gerada. IP: {IpAddress}", 
                HttpContext.Connection.RemoteIpAddress?.ToString());

            return Ok(config);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao obter configurações do app. IP: {IpAddress}. Erro: {ErrorMessage}", 
                HttpContext.Connection.RemoteIpAddress?.ToString(), ex.Message);
            return StatusCode(500, new { error = "Erro ao obter configurações", message = ex.Message });
        }
    }
}

