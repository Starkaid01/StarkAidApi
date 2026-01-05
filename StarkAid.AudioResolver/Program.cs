using Microsoft.AspNetCore.Mvc;
using YoutubeExplode;
using YoutubeExplode.Videos.Streams;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.OpenApi.Models;
using Microsoft.OpenApi.Any;
using Swashbuckle.AspNetCore.SwaggerGen;
using System.Threading;
using System.Collections.Concurrent;

var builder = WebApplication.CreateBuilder(args);

// Configuração de Segurança (ApiKey simples via Header)
var apiSecret = builder.Configuration["AudioResolver:Secret"] ?? "AUDIO_RESOLVER_DEFAULT_SECRET";

// Concorrência Limitada (SemaphoreSlim)
var semaphore = new SemaphoreSlim(2); // Máximo 2 resoluções simultâneas para evitar bloqueios de IP

builder.Services.AddSingleton<YoutubeClient>();
builder.Services.AddMemoryCache();

// Configuração do Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "StarkAid Audio Resolver", Version = "v1" });
    
    // Adiciona o campo do Header no Swagger para fácil teste
    c.OperationFilter<AddAuthHeaderOperationFilter>();
});

var app = builder.Build();

// Habilita Swagger sempre para facilitar seu teste
app.UseSwagger();
app.UseSwaggerUI();

app.MapGet("/", () => Results.Redirect("/swagger"));

app.MapGet("/resolve/{id}", async (string id, [FromHeader(Name = "X-Audio-Secret")] string? secret, YoutubeClient youtube, IMemoryCache cache, ILogger<Program> logger) =>
{
    // 1. Validação de Segurança
    if (secret != apiSecret)
    {
        logger.LogWarning("Unauthorized access attempt for ID: {Id}", id);
        return Results.Unauthorized();
    }

    if (string.IsNullOrWhiteSpace(id)) return Results.BadRequest("ID is required.");

    // 2. Cache L1 (Ouro)
    var cacheKey = $"stream_{id}";
    if (cache.TryGetValue(cacheKey, out object? cachedResult))
    {
        logger.LogInformation("Returning cached result for {Id}", id);
        return Results.Ok(cachedResult);
    }

    // 3. Fila / Semáforo
    await semaphore.WaitAsync();
    try
    {
        logger.LogInformation("Resolving stream for {Id}...", id);
        
        var streamManifest = await youtube.Videos.Streams.GetManifestAsync(id);
        var streamInfo = streamManifest.GetAudioOnlyStreams().GetWithHighestBitrate() 
                       ?? streamManifest.GetAudioStreams().OrderByDescending(s => s.Bitrate).FirstOrDefault();

        if (streamInfo == null)
        {
            logger.LogWarning("No audio streams found for {Id}", id);
            return Results.NotFound("Audio stream not found.");
        }

        // Tenta pegar expiração da URL
        DateTimeOffset expiration = DateTimeOffset.UtcNow.AddHours(2);
        try 
        {
            var uri = new Uri(streamInfo.Url);
            var query = System.Web.HttpUtility.ParseQueryString(uri.Query);
            var expireUnix = query["expire"];
            if (!string.IsNullOrEmpty(expireUnix) && long.TryParse(expireUnix, out long expireSeconds))
            {
                expiration = DateTimeOffset.FromUnixTimeSeconds(expireSeconds);
            }
        }
        catch { }

        var result = new
        {
            streamUrl = streamInfo.Url,
            expiresAt = expiration,
            resolvedAt = DateTimeOffset.UtcNow
        };

        // Cache de 30 minutos ou até expirar (o que for menor)
        var cacheDuration = expiration - DateTimeOffset.UtcNow;
        if (cacheDuration.TotalMinutes > 30) cacheDuration = TimeSpan.FromMinutes(30);
        
        if (cacheDuration.TotalSeconds > 0)
        {
            cache.Set(cacheKey, result, cacheDuration);
        }

        return Results.Ok(result);
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Error resolving stream for {Id}", id);
        return Results.Problem($"Failed to resolve stream: {ex.Message}");
    }
    finally
    {
        semaphore.Release();
    }
});

app.Run();

// Filtro para mostrar o Header de Autenticação no Swagger
public class AddAuthHeaderOperationFilter : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        if (operation.Parameters == null) operation.Parameters = new List<OpenApiParameter>();

        // Verifica se o parâmetro já existe para não duplicar
        if (!operation.Parameters.Any(p => p.Name == "X-Audio-Secret"))
        {
            operation.Parameters.Add(new OpenApiParameter
            {
                Name = "X-Audio-Secret",
                In = ParameterLocation.Header,
                Required = false, 
                Schema = new OpenApiSchema { Type = "string", Default = new OpenApiString("AUDIO_RESOLVER_DEFAULT_SECRET") }
            });
        }
    }
}
