using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using StarkAid.Api.Data;
using StarkAid.Api.Entities;

namespace StarkAid.Api.Services.V1.Music
{
    public interface IYouTubeMusicService
    {
        Task<(string? videoId, string? title)> SearchMusicAsync(string query);
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

        public async Task<(string? videoId, string? title)> SearchMusicAsync(string query)
        {
            string normalized = MusicQueryNormalizer.Normalize(query);
            if (string.IsNullOrEmpty(normalized)) return (null, null);

            string cacheKey = $"yt_music_{normalized}";

            // 1. Memory Cache (L1)
            if (_memoryCache.TryGetValue(cacheKey, out (string? vid, string? tit) cached))
            {
                _logger.LogInformation("YouTube L1 Cache Hit (Memory): {Query}", normalized);
                await UpdateHitCountAsync(cached.vid);
                return cached;
            }

            // 2. Database Cache (L2)
            var dbEntry = await _dbContext.YouTubeMusicCaches
                .FirstOrDefaultAsync(x => x.NormalizedQuery == normalized);

            if (dbEntry != null)
            {
                _logger.LogInformation("YouTube L2 Cache Hit (DB): {Query}", normalized);
                dbEntry.HitCount++;
                dbEntry.LastUsedAt = DateTimeOffset.UtcNow;
                await _dbContext.SaveChangesAsync();

                var result = (dbEntry.VideoId, dbEntry.Title);
                
                // Fix: Specify size for MemoryCache entry
                var cacheEntryOptions = new MemoryCacheEntryOptions()
                    .SetAbsoluteExpiration(TimeSpan.FromMinutes(30))
                    .SetSize(1);
                    
                _memoryCache.Set(cacheKey, result, cacheEntryOptions);
                return result;
            }

            // 3. YouTube API Search (Fallback)
            _logger.LogWarning("YouTube Cache Miss. Calling API for: {Query}. Quota usage: 100 units.", normalized);
            
            try
            {
                var searchUrl = $"https://www.googleapis.com/youtube/v3/search?part=snippet&q={Uri.EscapeDataString(normalized + " official audio")}&type=video&maxResults=5&key={_apiKey}";
                var response = await _httpClient.GetFromJsonAsync<YouTubeSearchResponse>(searchUrl);
                
                if (response?.Items == null || response.Items.Count == 0) return (null, null);

                // Heurística de seleção
                var bestItem = response.Items.FirstOrDefault(item => 
                    !item.Snippet.Title.ToLower().Contains("cover") &&
                    !item.Snippet.Title.ToLower().Contains("karaoke") &&
                    !item.Snippet.Title.ToLower().Contains("live") &&
                    !item.Snippet.Title.ToLower().Contains("ao vivo")
                ) ?? response.Items.First();

                // Salvar no Cache
                var newCache = new YouTubeMusicCache
                {
                    NormalizedQuery = normalized,
                    VideoId = bestItem.Id.VideoId,
                    Title = bestItem.Snippet.Title,
                    Channel = bestItem.Snippet.ChannelTitle,
                    Source = "YouTube",
                    HitCount = 1
                };

                _dbContext.YouTubeMusicCaches.Add(newCache);
                await _dbContext.SaveChangesAsync();

                var finalResult = (newCache.VideoId, newCache.Title);
                
                // Fix: Specify size for MemoryCache entry
                var cacheEntryOptions = new MemoryCacheEntryOptions()
                    .SetAbsoluteExpiration(TimeSpan.FromMinutes(30))
                    .SetSize(1); // Each entry counts as 1 unit
                
                _memoryCache.Set(cacheKey, finalResult, cacheEntryOptions);

                return finalResult;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao buscar no YouTube API");
                return (null, null);
            }
        }

        private async Task UpdateHitCountAsync(string? videoId)
        {
            if (string.IsNullOrEmpty(videoId)) return;
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
            catch { /* Ignore logging hit count errors */ }
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
