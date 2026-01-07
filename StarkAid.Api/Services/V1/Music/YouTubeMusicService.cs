using System.Net.Http.Json;
using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using StarkAid.Api.Data;
using StarkAid.Api.Entities;
using StarkAid.Api.Helpers;

namespace StarkAid.Api.Services.V1.Music
{
    public class YouTubeVideoResult
    {
        public string VideoId { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string? Channel { get; set; }
    }

    public interface IYouTubeMusicService
    {
        Task<List<YouTubeVideoResult>> SearchMusicAsync(string query, MusicKind kind = MusicKind.Song);
    }

    public class YouTubeMusicService : IYouTubeMusicService
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<YouTubeMusicService> _logger;
        private readonly IMemoryCache _memoryCache;
        private readonly AppDbContext _dbContext;
        private readonly string _apiKey;

        public YouTubeMusicService(
            HttpClient httpClient, 
            ILogger<YouTubeMusicService> logger, 
            IConfiguration config,
            IMemoryCache memoryCache,
            AppDbContext dbContext)
        {
            _httpClient = httpClient;
            _logger = logger;
            _memoryCache = memoryCache;
            _dbContext = dbContext;
            _apiKey = config["YouTube:ApiKey"] ?? string.Empty;
        }

        public async Task<List<YouTubeVideoResult>> SearchMusicAsync(string query, MusicKind kind = MusicKind.Song)
        {
            string normalized = MusicQueryNormalizer.Normalize(query);
            if (string.IsNullOrEmpty(normalized)) return new List<YouTubeVideoResult>();

            string cacheKey = $"yt_music_list_{normalized}_{kind}";

            // 1. Memory Cache (L1) - Rápido para evitar DB em repetições imediatas
            if (_memoryCache.TryGetValue(cacheKey, out List<YouTubeVideoResult>? cached) && cached != null)
            {
                _logger.LogInformation("YouTube L1 Cache Hit (Memory): {Query}", normalized);
                if (cached.Count > 0) await UpdateHitCountAsync(cached[0].VideoId);
                return cached;
            }

            // 2. Database Cache (L2) 
            var dbEntries = await _dbContext.YouTubeMusicCaches
                .Where(x => x.NormalizedQuery == normalized && x.Kind == kind)
                .OrderBy(x => x.LastUsedAt) // Menos usada primeiro para variedade
                .ToListAsync();

            if (dbEntries.Any())
            {
                _logger.LogInformation("YouTube L2 Cache Hit (DB): {Query} ({Count} items)", normalized, dbEntries.Count);
                
                // Pegamos a menos usada
                var selected = dbEntries.First();
                selected.HitCount++;
                selected.LastUsedAt = DateTimeOffset.UtcNow;
                await _dbContext.SaveChangesAsync();

                var result = new List<YouTubeVideoResult> { new YouTubeVideoResult { VideoId = selected.VideoId, Title = selected.Title } };
                
                // Cacheamos na memória por pouco tempo para não quebrar a rotação rapidamente se o usuário repetir o comando logo em seguida?
                // Na verdade, se cachearmos na memória, a rotação só acontece quando o cache expira. 
                // Por isso, para ARTISTA, talvez não devamos fazer cache L1 agressivo.
                if (kind == MusicKind.Song)
                {
                    _memoryCache.Set(cacheKey, result, new MemoryCacheEntryOptions 
                    { 
                        AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(30),
                        Size = 1 
                    });
                }
                
                return result;
            }

            // 2.b Smart L2 Cache (Procura música específica dentro de pools de artistas já salvos)
            if (kind == MusicKind.Song)
            {
                var smartMatch = await TrySmartSearchAsync(normalized);
                if (smartMatch != null)
                {
                    _logger.LogInformation("YouTube Smart Cache Hit (Fuzzy DB): {Query} encontrado em pool de {Title}", normalized, smartMatch.Title);
                    await UpdateHitCountAsync(smartMatch.VideoId);
                    
                    var result = new List<YouTubeVideoResult> { new YouTubeVideoResult { VideoId = smartMatch.VideoId, Title = smartMatch.Title } };
                    _memoryCache.Set(cacheKey, result, new MemoryCacheEntryOptions 
                    { 
                        AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(30),
                        Size = 1 
                    });
                    return result;
                }
            }

            // 3. YouTube API Search (Fallback)
            try
            {
                int maxResults = kind == MusicKind.Artist ? 10 : 5;
                string searchQuery = kind == MusicKind.Artist ? normalized : normalized + " official audio";
                
                var searchUrl = $"https://www.googleapis.com/youtube/v3/search?part=snippet&q={Uri.EscapeDataString(searchQuery)}&type=video&maxResults={maxResults}&key={_apiKey}";
                _logger.LogInformation("YouTube API Search: {Url}", searchUrl);
                
                var response = await _httpClient.GetFromJsonAsync<YouTubeSearchResponse>(searchUrl);
                
                if (response?.Items == null || response.Items.Count == 0)
                {
                    _logger.LogWarning("YouTube API returned NO results for query: {Query}", searchQuery);
                    return new List<YouTubeVideoResult>();
                }

                _logger.LogInformation("YouTube API returned {Count} results.", response.Items.Count);

                var results = response.Items.Select(item => new YouTubeVideoResult 
                { 
                    VideoId = item.Id.VideoId, 
                    Title = item.Snippet.Title,
                    Channel = item.Snippet.ChannelTitle
                }).ToList();

                // Salvar todos no Cache do DB se for Artista, ou o melhor se for Música
                if (kind == MusicKind.Artist)
                {
                    bool first = true;
                    foreach (var res in results)
                    {
                        // Evita duplicados por VideoId no mesmo Kind
                        if (await _dbContext.YouTubeMusicCaches.AnyAsync(x => x.VideoId == res.VideoId && x.Kind == kind))
                            continue;

                        var entry = new YouTubeMusicCache
                        {
                            NormalizedQuery = normalized,
                            VideoId = res.VideoId,
                            Title = res.Title,
                            Channel = res.Channel,
                            Kind = kind,
                            HitCount = first ? 1 : 0,
                            LastUsedAt = first ? DateTimeOffset.UtcNow : DateTimeOffset.UtcNow.AddMinutes(-5)
                        };
                        _dbContext.YouTubeMusicCaches.Add(entry);
                        first = false;
                    }
                    await _dbContext.SaveChangesAsync();
                }
                else
                {
                    // Música específica: salva apenas a melhor
                    var best = results.FirstOrDefault(item => 
                        !item.Title.ToLower().Contains("cover") &&
                        !item.Title.ToLower().Contains("karaoke") &&
                        !item.Title.ToLower().Contains("live")
                    ) ?? results.First();

                    // Verifica se essa música já existe no banco (mesmo com query diferente)
                    var existing = await _dbContext.YouTubeMusicCaches.FirstOrDefaultAsync(x => x.VideoId == best.VideoId && x.Kind == kind);
                    if (existing != null)
                    {
                        existing.HitCount++;
                        existing.LastUsedAt = DateTimeOffset.UtcNow;
                    }
                    else
                    {
                        var newCache = new YouTubeMusicCache
                        {
                            NormalizedQuery = normalized,
                            VideoId = best.VideoId,
                            Title = best.Title,
                            Channel = best.Channel,
                            Kind = kind,
                            HitCount = 1,
                            LastUsedAt = DateTimeOffset.UtcNow
                        };
                        _dbContext.YouTubeMusicCaches.Add(newCache);
                    }
                    
                    await _dbContext.SaveChangesAsync();
                    
                    results = new List<YouTubeVideoResult> { best };
                    _memoryCache.Set(cacheKey, results, new MemoryCacheEntryOptions 
                    { 
                        AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(30),
                        Size = 1 
                    });
                }

                if (kind == MusicKind.Artist)
                {
                    return results;
                }
                
                return results.Take(1).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao buscar no YouTube API");
                return new List<YouTubeVideoResult>();
            }
        }

        private async Task<YouTubeMusicCache?> TrySmartSearchAsync(string normalized)
        {
            // 1️⃣ Normaliza a consulta e remove stop‑words
            var cleanedQuery = Helpers.TextHelper.NormalizarParaBusca(normalized);
            // Carrega todo o cache (tamanho atual ainda é pequeno)
            var allEntries = await _dbContext.YouTubeMusicCaches.ToListAsync();
            // 2️⃣ Primeiro tenta encontrar um título que contenha a consulta limpa
            var containmentMatch = allEntries
                .FirstOrDefault(e =>
                    Helpers.TextHelper.NormalizarTexto(e.Title).Contains(cleanedQuery));
            if (containmentMatch != null)
                return containmentMatch;
            // 3️⃣ Fallback: similaridade de Jaccard (limiar mais baixo)
            var best = allEntries
                .Select(entry => new
                {
                    Entry = entry,
                    Similarity = TextHelper.JaccardSimilarity(
                        cleanedQuery,
                        Helpers.TextHelper.NormalizarTexto(entry.Title))
                })
                .Where(x => x.Similarity >= 0.2) // aceita sobreposição mínima
                .OrderByDescending(x => x.Similarity)
                .ThenByDescending(x => x.Entry.HitCount)
                .ThenByDescending(x => x.Entry.LastUsedAt)
                .Select(x => x.Entry)
                .FirstOrDefault();
            return best;
        }

        private async Task UpdateHitCountAsync(string videoId)
        {
            try
            {
                var entry = await _dbContext.YouTubeMusicCaches.FirstOrDefaultAsync(x => x.VideoId == videoId);
                if (entry != null)
                {
                    entry.HitCount++;
                    entry.LastUsedAt = DateTimeOffset.UtcNow;
                    await _dbContext.SaveChangesAsync();
                }
            }
            catch { /* Ignore */ }
        }

        private class YouTubeSearchResponse
        {
            public List<YouTubeItem>? Items { get; set; }
        }

        private class YouTubeItem
        {
            public YouTubeId Id { get; set; } = new();
            public YouTubeSnippet Snippet { get; set; } = new();
        }

        private class YouTubeId
        {
            public string VideoId { get; set; } = string.Empty;
        }

        private class YouTubeSnippet
        {
            public string Title { get; set; } = string.Empty;
            public string ChannelTitle { get; set; } = string.Empty;
        }
    }
}
