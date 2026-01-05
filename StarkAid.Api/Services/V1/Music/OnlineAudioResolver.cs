using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Caching.Memory;
using YoutubeExplode;
using YoutubeExplode.Videos.Streams;
using StarkAid.Api.DTOs.V1.Music;
using System.Web;
using System.Net.Http;
using System.Net.Http.Json;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;

namespace StarkAid.Api.Services.V1.Music
{
    public class OnlineAudioResolver : IExternalAudioResolver
    {
        private readonly ILogger<OnlineAudioResolver> _logger;
        private readonly IMemoryCache _cache;
        private readonly YoutubeClient _youtubeClient;
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly IHttpClientFactory _httpClientFactory;

        public OnlineAudioResolver(ILogger<OnlineAudioResolver> logger, IMemoryCache cache, IServiceScopeFactory scopeFactory, IHttpClientFactory httpClientFactory)
        {
            _logger = logger;
            _cache = cache;
            _scopeFactory = scopeFactory;
            _httpClientFactory = httpClientFactory;
            _youtubeClient = new YoutubeClient();
        }

        public async Task<ExternalAudioStreamResult?> GetAudioStreamUrlAsync(string externalId)
        {
            if (string.IsNullOrWhiteSpace(externalId)) return null;

            // 1. Check Cache
            var cacheKey = $"audio_stream_{externalId}";
            if (_cache.TryGetValue(cacheKey, out ExternalAudioStreamResult? cachedResult) && cachedResult != null)
            {
                 if (cachedResult.ExpiresAt > DateTimeOffset.UtcNow.AddMinutes(5))
                 {
                     _logger.LogInformation("Returning cached audio stream for ID: {Id}", externalId);
                     return cachedResult;
                 }
            }

            // 2. Check for Remote Resolver in Database
            string? remoteDomain = null;
            using (var scope = _scopeFactory.CreateScope())
            {
                var dbContext = scope.ServiceProvider.GetRequiredService<StarkAid.Api.Data.AppDbContext>();
                var config = await dbContext.ConfiguracoesSistema.FirstOrDefaultAsync();
                remoteDomain = config?.DominioAudioResolver;
            }

            if (!string.IsNullOrWhiteSpace(remoteDomain))
            {
                return await GetRemoteAudioStreamUrlAsync(externalId, remoteDomain);
            }

            // 3. Local Resolution (Fallback)
            return await GetLocalAudioStreamUrlAsync(externalId, cacheKey);
        }

        private async Task<ExternalAudioStreamResult?> GetRemoteAudioStreamUrlAsync(string externalId, string domain)
        {
            try
            {
                _logger.LogInformation("Resolving stream via REMOTE tunnel: {Domain} for ID: {Id}", domain, externalId);
                
                var client = _httpClientFactory.CreateClient();
                client.Timeout = TimeSpan.FromSeconds(30);
                
                // Header de segurança fixo para o túnel local
                client.DefaultRequestHeaders.Add("X-Audio-Secret", "AUDIO_RESOLVER_DEFAULT_SECRET");

                var url = $"{domain.TrimEnd('/')}/resolve/{externalId}";
                var response = await client.GetAsync(url);

                if (response.IsSuccessStatusCode)
                {
                    var result = await response.Content.ReadFromJsonAsync<ExternalAudioStreamResult>();
                    if (result != null)
                    {
                        var cacheEntryOptions = new MemoryCacheEntryOptions()
                            .SetAbsoluteExpiration(result.ExpiresAt.AddMinutes(-5))
                            .SetSize(1);

                        _cache.Set($"audio_stream_{externalId}", result, cacheEntryOptions);
                        return result;
                    }
                }
                
                _logger.LogWarning("Remote resolver failed with status: {Status}", response.StatusCode);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calling remote audio resolver at {Domain}", domain);
            }

            return null;
        }

        private async Task<ExternalAudioStreamResult?> GetLocalAudioStreamUrlAsync(string externalId, string cacheKey)
        {
            try
            {
                _logger.LogInformation("Resolving audio stream LOCALLY for ID: {Id}", externalId);
                var streamManifest = await _youtubeClient.Videos.Streams.GetManifestAsync(externalId);
                var streamInfo = streamManifest.GetAudioOnlyStreams().GetWithHighestBitrate() 
                                ?? streamManifest.GetAudioStreams().OrderByDescending(s => s.Bitrate).FirstOrDefault();

                if (streamInfo == null)
                {
                    _logger.LogWarning("No audio streams found locally for ID: {Id}", externalId);
                    return null;
                }

                DateTimeOffset expiration = DateTimeOffset.UtcNow.AddHours(2); 
                try 
                {
                    var uri = new Uri(streamInfo.Url);
                    var query = HttpUtility.ParseQueryString(uri.Query);
                    var expireUnix = query["expire"];
                    if (!string.IsNullOrEmpty(expireUnix) && long.TryParse(expireUnix, out long expireSeconds))
                        expiration = DateTimeOffset.FromUnixTimeSeconds(expireSeconds);
                }
                catch { }

                var result = new ExternalAudioStreamResult { StreamUrl = streamInfo.Url, ExpiresAt = expiration };

                var cacheEntryOptions = new MemoryCacheEntryOptions()
                    .SetAbsoluteExpiration(expiration.AddMinutes(-5))
                    .SetSize(1);

                _cache.Set(cacheKey, result, cacheEntryOptions);
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to resolve online stream locally for {Id}", externalId);
                return null;
            }
        }
    }
}
