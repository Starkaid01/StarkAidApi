using System.Net.Http.Json;
using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using StarkAid.Api.Data;
using StarkAid.Api.Entities;

namespace StarkAid.Api.Services.V1.Music
{
    public class YouTubeVideoResult
    {
        public string VideoId { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
    }

    public interface IYouTubeMusicService
    {
        Task<List<YouTubeVideoResult>> SearchMusicAsync(string query);
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

        public async Task<List<YouTubeVideoResult>> SearchMusicAsync(string query)
        {
            string normalized = MusicQueryNormalizer.Normalize(query);
            if (string.IsNullOrEmpty(normalized)) return new List<YouTubeVideoResult>();

            string cacheKey = $"yt_music_list_{normalized}";

            // 1. Memory Cache (L1)
            if (_memoryCache.TryGetValue(cacheKey, out List<YouTubeVideoResult>? cached) && cached != null)
            {
                _logger.LogInformation("YouTube L1 Cache Hit (Memory): {Query}", normalized);
                if (cached.Count > 0) await UpdateHitCountAsync(cached[0].VideoId);
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

                var result = new List<YouTubeVideoResult> { new YouTubeVideoResult { VideoId = dbEntry.VideoId, Title = dbEntry.Title } };
                
                var cacheEntryOptions = new MemoryCacheEntryOptions()
                    .SetAbsoluteExpiration(TimeSpan.FromMinutes(30))
                    .SetSize(1);
                    
                _memoryCache.Set(cacheKey, result, cacheEntryOptions);
                return result;
            }

            // 3. YouTube API Search (Fallback)
            try
            {
                var searchUrl = $"https://www.googleapis.com/youtube/v3/search?part=snippet&q={Uri.EscapeDataString(normalized + " official audio")}&type=video&maxResults=5&key={_apiKey}";
                var response = await _httpClient.GetFromJsonAsync<YouTubeSearchResponse>(searchUrl);
                
                if (response?.Items == null || response.Items.Count == 0) return new List<YouTubeVideoResult>();

                var results = response.Items.Select(item => new YouTubeVideoResult 
                { 
                    VideoId = item.Id.VideoId, 
                    Title = item.Snippet.Title 
                }).ToList();

                // Salvar o melhor no Cache do DB
                var best = results.FirstOrDefault(item => 
                    !item.Title.ToLower().Contains("cover") &&
                    !item.Title.ToLower().Contains("karaoke") &&
                    !item.Title.ToLower().Contains("live")
                ) ?? results.First();

                if (!await _dbContext.YouTubeMusicCaches.AnyAsync(x => x.NormalizedQuery == normalized))
                {
                    var newCache = new YouTubeMusicCache
                    {
                        NormalizedQuery = normalized,
                        VideoId = best.VideoId,
                        Title = best.Title,
                        Channel = response.Items.First(x => x.Id.VideoId == best.VideoId).Snippet.ChannelTitle,
                        Source = "YouTube",
                        HitCount = 1
                    };
                    _dbContext.YouTubeMusicCaches.Add(newCache);
                    await _dbContext.SaveChangesAsync();
                }

                var cacheEntryOptions = new MemoryCacheEntryOptions()
                    .SetAbsoluteExpiration(TimeSpan.FromMinutes(30))
                    .SetSize(1);
                
                _memoryCache.Set(cacheKey, results, cacheEntryOptions);

                return results;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao buscar no YouTube API");
                return new List<YouTubeVideoResult>();
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
