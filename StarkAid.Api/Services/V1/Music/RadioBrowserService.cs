using System.Net.Http.Json;
using Microsoft.Extensions.Caching.Memory;
using StarkAid.Api.DTOs.V1.Music;

namespace StarkAid.Api.Services.V1.Music
{
    public class RadioBrowserService : IRadioBrowserService
    {
        private readonly HttpClient _httpClient;
        private readonly IMemoryCache _cache;
        private readonly ILogger<RadioBrowserService> _logger;
        private const string BaseUrl = "https://nl1.api.radio-browser.info/json";

        public RadioBrowserService(HttpClient httpClient, IMemoryCache cache, ILogger<RadioBrowserService> logger)
        {
            _httpClient = httpClient;
            _cache = cache;
            _logger = logger;
            
            if (!_httpClient.DefaultRequestHeaders.Contains("User-Agent"))
            {
                _httpClient.DefaultRequestHeaders.Add("User-Agent", "StarkAidAssistant/1.0");
            }
        }

        public async Task<List<MusicStationStation>> SearchAsync(string? name = null, string? tag = null, string? country = "Brazil")
        {
            var cacheKey = $"radio_v3_{name}_{tag}_{country}";
            if (_cache.TryGetValue(cacheKey, out List<MusicStationStation>? cachedResults))
            {
                return cachedResults!;
            }

            try
            {
                var queryParams = new List<string>();
                if (!string.IsNullOrEmpty(name)) queryParams.Add($"name={Uri.EscapeDataString(name)}");
                if (!string.IsNullOrEmpty(tag)) queryParams.Add($"tag={Uri.EscapeDataString(tag)}");
                if (!string.IsNullOrEmpty(country)) queryParams.Add($"country={Uri.EscapeDataString(country)}");
                
                queryParams.Add("order=clickcount");
                queryParams.Add("reverse=true");
                queryParams.Add("limit=25");

                var url = $"{BaseUrl}/stations/search?{string.Join("&", queryParams)}";
                var response = await _httpClient.GetFromJsonAsync<List<RadioBrowserStation>>(url);

                var results = response?.Select(s => new MusicStationStation
                {
                    Name = s.Name,
                    StreamUrl = s.Url_Resolved ?? s.Url,
                    Tags = s.Tags,
                    Country = s.Country,
                    Bitrate = s.Bitrate
                }).ToList() ?? new List<MusicStationStation>();

                _cache.Set(cacheKey, results, TimeSpan.FromHours(2));
                return results;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching radio stations");
                return new List<MusicStationStation>();
            }
        }

        public async Task<MusicStationStation?> ResolveBestRadioAsync(string query, string? category = null)
        {
            _logger.LogInformation("Resolving radio for query: {Query}, category: {Category}", query, category);

            // Heurística Brasileira de Mapeamento (Alexa-style)
            var artistMapping = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                { "cesar menotti", "sertanejo" },
                { "cesar menotti e fabiano", "sertanejo" },
                { "charlie brown", "rock nacional" },
                { "charlie brown jr", "rock nacional" },
                { "ze neto", "sertanejo" },
                { "cristiano araujo", "sertanejo" },
                { "jorge e mateus", "sertanejo" },
                { "skank", "pop rock" },
                { "legiao urbana", "rock nacional" },
                { "os baroes da pisadinha", "piseiro" },
                { "marilia mendonca", "sertanejo" }
            };

            // 1. Tenta busca por nome exato (Artista ou Estação)
            var stations = await SearchAsync(name: query);

            // 2. Se não achou e temos mapeamento, degue para o gênero
            if (stations.Count == 0 && artistMapping.TryGetValue(query, out var mappedGenre))
            {
                _logger.LogInformation("Artista mapeado para gênero: {Genre}", mappedGenre);
                stations = await SearchAsync(tag: mappedGenre);
            }

            // 3. Tenta por Categoria original
            if (stations.Count == 0 && !string.IsNullOrEmpty(category))
            {
                stations = await SearchAsync(tag: category);
            }

            // 4. Fallback por tags individuais da query
            if (stations.Count == 0)
            {
                stations = await SearchAsync(tag: query);
            }

            // Requisito: Critério relaxado (bitrate >= 48) para rádios brasileiras
            var best = stations
                .Where(s => !string.IsNullOrEmpty(s.StreamUrl))
                .Where(s => s.Bitrate >= 48) 
                .OrderByDescending(s => s.Bitrate)
                .FirstOrDefault();

            return best;
        }

        private class RadioBrowserStation
        {
            public string Name { get; set; } = string.Empty;
            public string Url { get; set; } = string.Empty;
            public string? Url_Resolved { get; set; }
            public string Tags { get; set; } = string.Empty;
            public string Country { get; set; } = string.Empty;
            public int Bitrate { get; set; }
        }
    }
}
