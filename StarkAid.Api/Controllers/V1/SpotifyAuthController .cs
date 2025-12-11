using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using StarkAid.Api.Data;
using StarkAid.Api.DTOs.V1.Spotify;
using System.Net.Http.Headers;
using System.Text;
using static System.Net.WebRequestMethods;

namespace StarkAid.Api.Controllers.V1
{
        [ApiVersion("1.0")]
    [ApiController]
    [Route("api/v{version:apiVersion}/[controller]")]
    public class SpotifyAuthController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly IConfiguration _config;
        private readonly IHttpClientFactory _httpClientFactory;

        public SpotifyAuthController(AppDbContext context, IConfiguration config, IHttpClientFactory httpClientFactory)
        {
            _context = context;
            _config = config;
            _httpClientFactory = httpClientFactory;
        }
        private HttpClient CreateHttpClient()
        {
            return _httpClientFactory.CreateClient();
        }


        [HttpGet("me/token/{userId}")]
        public async Task<IActionResult> GetValidToken(Guid userId)
        {
            var user = await _context.Users.FindAsync(userId);
            if (user == null) return NotFound("Usuário não encontrado");

            if (user.SpotifyTokenExpiresAt <= DateTime.UtcNow)
            {
                // Faz refresh
                return await RefreshToken(userId, user.SpotifyRefreshToken);
            }

            return Ok(new { accessToken = user.SpotifyAccessToken });
        }

        [HttpGet("token")]
        public async Task<IActionResult> GetAccessToken(Guid userId)
        {
            var user = await _context.Users.FindAsync(userId);
            if (user == null) return NotFound("Usuário não encontrado");

            // se expirado, faz refresh automático
            if (user.SpotifyTokenExpiresAt <= DateTime.UtcNow)
            {
                var clientId = _config["Spotify:ClientId"];
                var clientSecret = _config["Spotify:ClientSecret"];
                var basic = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{clientId}:{clientSecret}"));

                using var httpClient = _httpClientFactory.CreateClient();
                var request = new HttpRequestMessage(HttpMethod.Post, "https://accounts.spotify.com/api/token");
                request.Headers.Authorization = new AuthenticationHeaderValue("Basic", basic);
                request.Content = new FormUrlEncodedContent(new Dictionary<string, string>
                {
                    {"grant_type", "refresh_token"},
                    {"refresh_token", user.SpotifyRefreshToken}
                });

                var response = await httpClient.SendAsync(request);
                if (!response.IsSuccessStatusCode)
                {
                    var error = await response.Content.ReadAsStringAsync();
                    return BadRequest($"Erro ao renovar token: {error}");
                }

                var json = await response.Content.ReadAsStringAsync();
                var tokenResponse = JsonConvert.DeserializeObject<SpotifyTokenResponse>(json);

                user.SpotifyAccessToken = tokenResponse.AccessToken;
                if (!string.IsNullOrEmpty(tokenResponse.RefreshToken))
                    user.SpotifyRefreshToken = tokenResponse.RefreshToken;
                user.SpotifyTokenExpiresAt = DateTime.UtcNow.AddSeconds(tokenResponse.ExpiresIn);

                await _context.SaveChangesAsync();
            }

            return Ok(new { accessToken = user.SpotifyAccessToken });
        }

        [HttpPost("play-by-name")]
        public async Task<IActionResult> PlayByName([FromBody] PlayByNameRequest request)
        {
            var user = await _context.Users.FindAsync(request.UserId);
            if (user == null || string.IsNullOrEmpty(user.SpotifyAccessToken))
                return NotFound("Usuário não encontrado ou não autenticado no Spotify");

            // 🔹 1. Se token expirado → refresh
            if (user.SpotifyTokenExpiresAt <= DateTime.UtcNow)
            {
                var clientId = _config["Spotify:ClientId"];
                var clientSecret = _config["Spotify:ClientSecret"];
                var basic = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{clientId}:{clientSecret}"));

                using var httpClient = _httpClientFactory.CreateClient();
                var tokenRequest = new HttpRequestMessage(HttpMethod.Post, "https://accounts.spotify.com/api/token");
                tokenRequest.Headers.Authorization = new AuthenticationHeaderValue("Basic", basic);
                tokenRequest.Content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            {"grant_type", "refresh_token"},
            {"refresh_token", user.SpotifyRefreshToken}
        });

                var tokenResponse = await httpClient.SendAsync(tokenRequest);
                if (!tokenResponse.IsSuccessStatusCode)
                {
                    var error = await tokenResponse.Content.ReadAsStringAsync();
                    return BadRequest($"Erro ao renovar token: {error}");
                }

                var json = await tokenResponse.Content.ReadAsStringAsync();
                var spotifyToken = JsonConvert.DeserializeObject<SpotifyTokenResponse>(json);

                user.SpotifyAccessToken = spotifyToken.AccessToken;
                if (!string.IsNullOrEmpty(spotifyToken.RefreshToken))
                    user.SpotifyRefreshToken = spotifyToken.RefreshToken;
                user.SpotifyTokenExpiresAt = DateTime.UtcNow.AddSeconds(spotifyToken.ExpiresIn);

                await _context.SaveChangesAsync();
            }

            using var client = _httpClientFactory.CreateClient();

            // 🔹 2. Descobrir o deviceId do Web Player ativo
            var devicesReq = new HttpRequestMessage(HttpMethod.Get, "https://api.spotify.com/v1/me/player/devices");
            devicesReq.Headers.Authorization = new AuthenticationHeaderValue("Bearer", user.SpotifyAccessToken);
            var devicesRes = await client.SendAsync(devicesReq);
            if (!devicesRes.IsSuccessStatusCode)
            {
                var err = await devicesRes.Content.ReadAsStringAsync();
                return BadRequest($"Erro ao listar devices: {err}");
            }

            var devicesJson = await devicesRes.Content.ReadAsStringAsync();
            dynamic devices = JsonConvert.DeserializeObject(devicesJson);
            string deviceId = null;

            foreach (var d in devices.devices)
            {
                if (d.type == "Computer" || d.name == "StarkAid Web Player")
                {
                    deviceId = d.id;
                    break;
                }
            }

            if (string.IsNullOrEmpty(deviceId))
                return BadRequest("Nenhum Web Player ativo encontrado");

            // 🔹 3. Buscar track pelo nome
            var searchReq = new HttpRequestMessage(
                HttpMethod.Get,
                $"https://api.spotify.com/v1/search?q={Uri.EscapeDataString(request.TrackName)}&type=track&limit=1"
            );
            searchReq.Headers.Authorization = new AuthenticationHeaderValue("Bearer", user.SpotifyAccessToken);

            var searchRes = await client.SendAsync(searchReq);
            if (!searchRes.IsSuccessStatusCode)
                return BadRequest("Erro ao buscar música");

            var searchJson = await searchRes.Content.ReadAsStringAsync();
            dynamic searchData = JsonConvert.DeserializeObject(searchJson);
            if (searchData.tracks.items.Count == 0)
                return NotFound("Música não encontrada");

            string trackUri = searchData.tracks.items[0].uri;

            // 🔹 4. Tocar música
            var playReq = new HttpRequestMessage(
                HttpMethod.Put,
                $"https://api.spotify.com/v1/me/player/play?device_id={deviceId}"
            );
            playReq.Headers.Authorization = new AuthenticationHeaderValue("Bearer", user.SpotifyAccessToken);
            playReq.Content = new StringContent(
                JsonConvert.SerializeObject(new { uris = new[] { trackUri } }),
                Encoding.UTF8,
                "application/json"
            );

            var playRes = await client.SendAsync(playReq);
            if (!playRes.IsSuccessStatusCode)
            {
                var err = await playRes.Content.ReadAsStringAsync();
                return BadRequest($"Erro ao tocar música: {playRes.StatusCode} - {err}");
            }

            return Ok(new { success = true, playedTrack = trackUri });
        }

        [HttpPost("exchange")]
        public async Task<IActionResult> ExchangeCode([FromBody] SpotifyCodeDto dto)
        {
            using var httpClient = CreateHttpClient();
            if (string.IsNullOrEmpty(dto.Code) || dto.UserId == Guid.Empty)
                return BadRequest("Code e UserId são obrigatórios");

            var clientId = _config["Spotify:ClientId"];
            var clientSecret = _config["Spotify:ClientSecret"];
            var redirectUri = _config["Spotify:RedirectUri"];

            var basic = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes($"{clientId}:{clientSecret}"));

            var request = new HttpRequestMessage(HttpMethod.Post, "https://accounts.spotify.com/api/token");
            request.Headers.Authorization = new AuthenticationHeaderValue("Basic", basic);
            request.Content = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                {"grant_type", "authorization_code"},
                {"code", dto.Code},
                {"redirect_uri", redirectUri}
            });
            request.Content.Headers.ContentType = new MediaTypeHeaderValue("application/x-www-form-urlencoded");

            var response = await httpClient.SendAsync(request);
            if (!response.IsSuccessStatusCode)
            {
                var body = await response.Content.ReadAsStringAsync();
                return BadRequest($"Erro ao trocar code no Spotify: {response.StatusCode} - {body}");
            }

            var json = await response.Content.ReadAsStringAsync();
            var tokenResponse = JsonConvert.DeserializeObject<SpotifyTokenResponse>(json);

            var user = await _context.Users.FindAsync(dto.UserId);
            if (user == null) return NotFound("Usuário não encontrado");

            user.SpotifyAccessToken = tokenResponse.AccessToken;
            user.SpotifyRefreshToken = tokenResponse.RefreshToken;
            user.SpotifyTokenExpiresAt = DateTime.UtcNow.AddSeconds(tokenResponse.ExpiresIn);

            await _context.SaveChangesAsync();

            return Ok(new { success = true, accessToken = tokenResponse.AccessToken, refreshToken = tokenResponse.RefreshToken, expiresIn = tokenResponse.ExpiresIn });
        }

        [HttpGet("premium-status/{userId}")]
        public async Task<IActionResult> PremiumStatus(Guid userId)
        {
            using var httpClient = CreateHttpClient();

            var user = await _context.Users.FindAsync(userId);
            if (user == null || string.IsNullOrEmpty(user.SpotifyAccessToken))
                return NotFound();

            var request = new HttpRequestMessage(HttpMethod.Get, "https://api.spotify.com/v1/me");
            request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", user.SpotifyAccessToken);

            var response = await httpClient.SendAsync(request);
            if (!response.IsSuccessStatusCode)
                return BadRequest("Erro ao consultar Spotify");

            var json = await response.Content.ReadAsStringAsync();
            dynamic data = JsonConvert.DeserializeObject(json);

            bool isPremium = data.product == "premium";
            return Ok(new { isPremium });
        }

        // GET: api/spotifyauth/search-track?userId=...&query=...
        [HttpGet("search-track")]
        public async Task<IActionResult> SearchTrack([FromQuery] Guid userId, [FromQuery] string query)
        {
            using var httpClient = CreateHttpClient();

            var user = await _context.Users.FindAsync(userId);
            if (user == null || string.IsNullOrEmpty(user.SpotifyAccessToken))
                return NotFound("Usuário não encontrado ou não autenticado no Spotify");

            var request = new HttpRequestMessage(
                HttpMethod.Get,
                $"https://api.spotify.com/v1/search?q={Uri.EscapeDataString(query)}&type=track&limit=1"
            );
            request.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", user.SpotifyAccessToken);

            var response = await httpClient.SendAsync(request);
            if (!response.IsSuccessStatusCode)
                return BadRequest("Erro ao buscar música no Spotify");

            var json = await response.Content.ReadAsStringAsync();
            dynamic data = JsonConvert.DeserializeObject(json);

            if (data.tracks.items.Count == 0)
                return NotFound("Música não encontrada");

            string trackUri = data.tracks.items[0].uri;
            return Ok(new { trackUri });
        }

        [HttpPost("refresh-token")]
        public async Task<IActionResult> RefreshToken(Guid userId, string refreshToken)
        {
            using var httpClient = CreateHttpClient();

            var clientId = _config["Spotify:ClientId"];
            var clientSecret = _config["Spotify:ClientSecret"];

            var basic = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes($"{clientId}:{clientSecret}"));

            var request = new HttpRequestMessage(HttpMethod.Post, "https://accounts.spotify.com/api/token");
            request.Headers.Authorization = new AuthenticationHeaderValue("Basic", basic);
            request.Content = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                {"grant_type", "refresh_token"},
                {"refresh_token", refreshToken}
            });

            var response = await httpClient.SendAsync(request);
            if (!response.IsSuccessStatusCode) return BadRequest("Erro no refresh do Spotify");

            var json = await response.Content.ReadAsStringAsync();
            var tokenResponse = JsonConvert.DeserializeObject<SpotifyTokenResponse>(json);

            var user = await _context.Users.FindAsync(userId);
            if (user == null) return NotFound("Usuário não encontrado");

            user.SpotifyAccessToken = tokenResponse.AccessToken;
            if (!string.IsNullOrEmpty(tokenResponse.RefreshToken))
                user.SpotifyRefreshToken = tokenResponse.RefreshToken;
            user.SpotifyTokenExpiresAt = DateTime.UtcNow.AddSeconds(tokenResponse.ExpiresIn);

            await _context.SaveChangesAsync();

            return Ok(new
            {
                success = true,
                accessToken = tokenResponse.AccessToken,
                refreshToken = tokenResponse.RefreshToken,
                expiresIn = tokenResponse.ExpiresIn
            });
        }

        public record SpotifyConfigDto(string ClientId, string RedirectUri);

        [HttpGet("config")]
        public IActionResult Config()
        {
            var clientId = _config["Spotify:ClientId"];
            var redirectUri = _config["Spotify:RedirectUri"];
            if (string.IsNullOrWhiteSpace(clientId) || string.IsNullOrWhiteSpace(redirectUri))
                return BadRequest("Spotify config ausente.");

            return Ok(new SpotifyConfigDto(clientId, redirectUri));
        }

        // Endpoint para tocar música
        [HttpPost("play")]
        public async Task<IActionResult> PlayTrack([FromBody] PlayRequest request)
        {
            using var httpClient = CreateHttpClient();

            var user = await _context.Users.FindAsync(request.UserId);
            if (user == null || string.IsNullOrEmpty(user.SpotifyAccessToken))
                return NotFound("Usuário não encontrado ou não autenticado no Spotify");

            var requestUri = "https://api.spotify.com/v1/me/player/play";

            // Adiciona device_id se fornecido
            if (!string.IsNullOrEmpty(request.DeviceId))
            {
                requestUri += $"?device_id={request.DeviceId}";
            }

            var httpRequest = new HttpRequestMessage(HttpMethod.Put, requestUri);
            httpRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", user.SpotifyAccessToken);
            httpRequest.Content = new StringContent(
                JsonConvert.SerializeObject(new { uris = new[] { request.TrackUri } }),
                Encoding.UTF8,
                "application/json");

            var response = await httpClient.SendAsync(httpRequest);
            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync();
                return BadRequest($"Erro ao tocar música: {response.StatusCode} - {error}");
            }

            return Ok();
        }

        // Endpoint para pausar
        [HttpPost("pause")]
        public async Task<IActionResult> PauseTrack([FromBody] DeviceRequest request)
        {
            using var httpClient = CreateHttpClient();

            var user = await _context.Users.FindAsync(request.UserId);
            if (user == null || string.IsNullOrEmpty(user.SpotifyAccessToken))
                return NotFound("Usuário não encontrado ou não autenticado no Spotify");

            var httpRequest = new HttpRequestMessage(HttpMethod.Put, "https://api.spotify.com/v1/me/player/pause");
            httpRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", user.SpotifyAccessToken);
            httpRequest.Content = new StringContent("", Encoding.UTF8, "application/json");

            var response = await httpClient.SendAsync(httpRequest);
            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync();
                return BadRequest($"Erro ao pausar música: {response.StatusCode} - {error}");
            }

            return Ok();
        }
    }

    // Adicione estas classes no controller
    public class PlayRequest
    {
        public Guid UserId { get; set; }
        public string TrackUri { get; set; }
        public string DeviceId { get; set; }
    }


    public class DeviceRequest
    {
        public Guid UserId { get; set; }
    }



    public class SpotifyCodeDto
    {
        public Guid UserId { get; set; }
        public string Code { get; set; }
    }

    public class SpotifyTokenResponse
    {
        [JsonProperty("access_token")] public string AccessToken { get; set; }
        [JsonProperty("refresh_token")] public string RefreshToken { get; set; }
        [JsonProperty("expires_in")] public int ExpiresIn { get; set; }
        [JsonProperty("token_type")] public string TokenType { get; set; }
        [JsonProperty("scope")] public string Scope { get; set; }
    }
}
