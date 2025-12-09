using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;
using StarkAid.Api.Features.TuyaAdmin.Models;
using StarkAid.Api.Options;

namespace StarkAid.Api.Features.TuyaAdmin.Services
{
    public class TuyaAdminService : ITuyaAdminService
    {
        private readonly TuyaConfig _config;
        private readonly IHttpClientFactory _httpFactory;
        private readonly TuyaTokenProvider _tokenProvider;
        private readonly ILogger<TuyaAdminService> _logger;

        public TuyaAdminService(
            IOptions<TuyaConfig> config,
            IHttpClientFactory httpFactory,
            TuyaTokenProvider tokenProvider,
            ILogger<TuyaAdminService> logger)
        {
            _config = config.Value;
            _httpFactory = httpFactory;
            _tokenProvider = tokenProvider;
            _logger = logger;
        }

        // ============================================================
        // 1) LISTAR / BUSCAR USUÁRIO POR EMAIL
        // ============================================================
        public async Task<TuyaUserDto?> GetUserByEmailAsync(string email, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(email)) return null;

            var accessToken = await _tokenProvider.GetAccessTokenAsync(ct);
            var timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds().ToString();

            var path = $"/v1.0/users/account?country_code={_config.CountryCode}&username={Uri.EscapeDataString(email)}";

            var sign = TuyaSignHelper.BuildSign(
                clientId: _config.AccessId,
                secret: _config.AccessSecret,
                method: "GET",
                pathAndQuery: path,
                accessToken: accessToken,
                timestamp: timestamp
            );

            var url = $"{_config.BaseUrl}{path}";
            var req = new HttpRequestMessage(HttpMethod.Get, url);
            req.Headers.Add("client_id", _config.AccessId);
            req.Headers.Add("access_token", accessToken);
            req.Headers.Add("t", timestamp);
            req.Headers.Add("sign", sign);
            req.Headers.Add("sign_method", "HMAC-SHA256");

            var client = _httpFactory.CreateClient("TuyaAdmin");
            var resp = await client.SendAsync(req, ct);
            var content = await resp.Content.ReadAsStringAsync(ct);

            if (!resp.IsSuccessStatusCode)
            {
                _logger.LogWarning("Tuya GetUserByEmailAsync falhou: {Status} - {Body}", resp.StatusCode, content);
                return null;
            }

            using var doc = JsonDocument.Parse(content);
            var root = doc.RootElement;

            if (!root.TryGetProperty("success", out var successProp) || !successProp.GetBoolean())
                return null;

            if (!root.TryGetProperty("result", out var result))
                return null;

            var uid = result.GetProperty("uid").GetString() ?? string.Empty;
            var username = result.GetProperty("username").GetString() ?? email;
            var country = result.GetProperty("country_code").GetString() ?? _config.CountryCode;
            var createTime = result.TryGetProperty("create_time", out var ctProp) ? ctProp.GetString() : null;

            return new TuyaUserDto(uid, username, country, createTime);
        }

        // ============================================================
        // 2) DELETAR USUÁRIO POR UID
        // ============================================================
        public async Task<bool> DeleteUserByUidAsync(string uid, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(uid))
                return false;

            var accessToken = await _tokenProvider.GetAccessTokenAsync(ct);
            var timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds().ToString();

            var path = $"/v1.0/users/{Uri.EscapeDataString(uid)}";

            var sign = TuyaSignHelper.BuildSign(
                clientId: _config.AccessId,
                secret: _config.AccessSecret,
                method: "DELETE",
                pathAndQuery: path,
                accessToken: accessToken,
                timestamp: timestamp
            );

            var url = $"{_config.BaseUrl}{path}";
            var req = new HttpRequestMessage(HttpMethod.Delete, url);
            req.Headers.Add("client_id", _config.AccessId);
            req.Headers.Add("access_token", accessToken);
            req.Headers.Add("t", timestamp);
            req.Headers.Add("sign", sign);
            req.Headers.Add("sign_method", "HMAC-SHA256");

            var client = _httpFactory.CreateClient("TuyaAdmin");
            var resp = await client.SendAsync(req, ct);
            var body = await resp.Content.ReadAsStringAsync(ct);

            if (!resp.IsSuccessStatusCode)
            {
                _logger.LogWarning("Tuya DeleteUser falhou: {Status} - {Content}", resp.StatusCode, body);
                return false;
            }

            using var doc = JsonDocument.Parse(body);
            var root = doc.RootElement;

            return root.TryGetProperty("success", out var ok) && ok.GetBoolean();
        }

        // ============================================================
        // 3) CLEAN DUPLICATES — DELETAR VÁRIOS E-MAILS
        // ============================================================
        public async Task<IEnumerable<(string email, bool deleted, string message)>> CleanDuplicatesAsync(
            IEnumerable<string> emails,
            CancellationToken ct = default)
        {
            var results = new List<(string email, bool deleted, string message)>();

            foreach (var email in emails)
            {
                try
                {
                    var user = await GetUserByEmailAsync(email, ct);

                    if (user == null)
                    {
                        results.Add((email, false, "Usuário não encontrado"));
                        continue;
                    }

                    var ok = await DeleteUserByUidAsync(user.Uid, ct);
                    results.Add((email, ok, ok ? "Deletado" : "Falha ao deletar"));
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Erro ao processar {Email}", email);
                    results.Add((email, false, $"Erro: {ex.Message}"));
                }
            }

            return results;
        }


        // ADICIONE este método no TuyaAdminService
        public async Task<TuyaUserDto?> CreateUserInCloudProjectAsync(string email, string password, CancellationToken ct = default)
        {
            try
            {
                var accessToken = await _tokenProvider.GetAccessTokenAsync(ct);
                var timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds().ToString();

                // Caminho correto SEM QUERY
                var path = "/v1.0/users";

                // Corpo REAL que será enviado
                var bodyObject = new
                {
                    username = email,
                    password = password,
                    country_code = _config.CountryCode,
                    nickname = email.Split('@')[0]
                };

                string bodyJson = JsonSerializer.Serialize(bodyObject);

                // Monta a assinatura COM O BODY REAL
                var sign = TuyaSignHelper.BuildSign(
                    clientId: _config.AccessId,
                    secret: _config.AccessSecret,
                    method: "POST",
                    pathAndQuery: path,
                    accessToken: accessToken,
                    timestamp: timestamp,
                    bodyJson: bodyJson   // <-- AQUI QUE ESTAVA FALTANDO
                );

                var url = $"{_config.BaseUrl}{path}";

                using var req = new HttpRequestMessage(HttpMethod.Post, url);
                req.Headers.Add("client_id", _config.AccessId);
                req.Headers.Add("access_token", accessToken);
                req.Headers.Add("t", timestamp);
                req.Headers.Add("sign", sign);
                req.Headers.Add("sign_method", "HMAC-SHA256");

                req.Content = new StringContent(bodyJson, Encoding.UTF8, "application/json");

                var client = _httpFactory.CreateClient("TuyaAdmin");
                var resp = await client.SendAsync(req, ct);
                var content = await resp.Content.ReadAsStringAsync(ct);

                _logger.LogInformation("📨 Resposta criação usuário: {Status} - {Content}",
                    resp.StatusCode, content);

                if (!resp.IsSuccessStatusCode)
                {
                    _logger.LogWarning("❌ Falha HTTP ao criar usuário: {Status} - {Content}",
                        resp.StatusCode, content);
                    return null;
                }

                using var doc = JsonDocument.Parse(content);
                var root = doc.RootElement;

                if (!root.GetProperty("success").GetBoolean())
                {
                    var code = root.TryGetProperty("code", out var c) ? c.GetInt32() : -1;
                    var msg = root.TryGetProperty("msg", out var m) ? m.GetString() : "sem mensagem";

                    _logger.LogError("❌ Tuya retornou erro ao criar usuário: code={Code}, msg={Msg}, json={Json}",
                        code, msg, content);

                    return null;
                }

                var result = root.GetProperty("result");
                var uid = result.GetProperty("uid").GetString()!;
                var username = result.GetProperty("username").GetString()!;

                return new TuyaUserDto(
                    uid,
                    username,
                    _config.CountryCode,
                    DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss")
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao criar usuário no Cloud Project");
                return null;
            }
        }
    }
}