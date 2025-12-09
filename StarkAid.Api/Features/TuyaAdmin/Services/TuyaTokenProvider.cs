using Microsoft.Extensions.Options;
using StarkAid.Api.Options;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace StarkAid.Api.Features.TuyaAdmin.Services
{
    public class TuyaTokenProvider
    {
        private readonly TuyaConfig _config;
        private readonly IHttpClientFactory _httpFactory;
        private readonly ILogger<TuyaTokenProvider> _logger;

        private string? _cachedToken;
        private DateTimeOffset _expiresAt = DateTimeOffset.MinValue;
        private readonly SemaphoreSlim _mutex = new(1, 1);

        public TuyaTokenProvider(
            IOptions<TuyaConfig> config,
            IHttpClientFactory httpFactory,
            ILogger<TuyaTokenProvider> logger)
        {
            _config = config.Value;
            _httpFactory = httpFactory;
            _logger = logger;
        }

        public async Task<string> GetAccessTokenAsync(CancellationToken ct = default)
        {
            // Cache
            if (!string.IsNullOrEmpty(_cachedToken) && DateTimeOffset.UtcNow < _expiresAt)
                return _cachedToken!;

            await _mutex.WaitAsync(ct);
            try
            {
                if (!string.IsNullOrEmpty(_cachedToken) && DateTimeOffset.UtcNow < _expiresAt)
                    return _cachedToken!;

                var client = _httpFactory.CreateClient("TuyaAdmin");

                var timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds().ToString();
                var path = "/v1.0/token?grant_type=1";

                var sign = TuyaSignHelper.BuildTokenRequestSign(
                    clientId: _config.AccessId,
                    secret: _config.AccessSecret,
                    method: "GET",
                    pathAndQuery: path,
                    timestamp: timestamp
                );

                var url = $"{_config.BaseUrl}{path}";
                var req = new HttpRequestMessage(HttpMethod.Get, url);
                req.Headers.Add("client_id", _config.AccessId);
                req.Headers.Add("t", timestamp);
                req.Headers.Add("sign", sign);
                req.Headers.Add("sign_method", "HMAC-SHA256");

                var resp = await client.SendAsync(req, ct);
                resp.EnsureSuccessStatusCode();

                var json = await resp.Content.ReadFromJsonAsync<JsonElement>(cancellationToken: ct);

                if (!json.TryGetProperty("result", out var result))
                    throw new Exception("Resposta inesperada do Tuya ao obter token.");

                var token = result.GetProperty("access_token").GetString()!;
                var expires = result.GetProperty("expire_time").GetInt64();

                _cachedToken = token;
                _expiresAt = DateTimeOffset.UtcNow.AddSeconds(expires - 60);

                return token;
            }
            finally
            {
                _mutex.Release();
            }
        }
    }
}