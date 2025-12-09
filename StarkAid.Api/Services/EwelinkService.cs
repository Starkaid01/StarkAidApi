using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using StarkAid.Api.Data;
using StarkAid.Api.DTOs.Ewelink;
using StarkAid.Api.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace StarkAid.Api.Services
{
    public class EwelinkService : IEwelinkService
    {
        private readonly HttpClient _http;
        private readonly AppDbContext _context;
        private readonly IConfiguration _configuration;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly string _clientId;
        private readonly string _clientSecret;

        private string GetRedirectUri()
        {
            var redirectUri = _configuration["Ewelink:RedirectUri"];
            if (!string.IsNullOrEmpty(redirectUri))
                return redirectUri;
            
            // Fallback: construir dinamicamente baseado no request
            if (_httpContextAccessor.HttpContext != null)
            {
                var request = _httpContextAccessor.HttpContext.Request;
                return $"{request.Scheme}://{request.Host}/auth/ewelink/callback.html";
            }
            
            return "https://starkaid.runasp.net/auth/ewelink/callback.html";
        }

        public EwelinkService(HttpClient http, AppDbContext context, IConfiguration configuration, IHttpContextAccessor httpContextAccessor)
        {
            _http = http;
            _context = context;
            _configuration = configuration;
            _httpContextAccessor = httpContextAccessor;
            
            _clientId = _configuration["Ewelink:ClientId"] 
                ?? throw new InvalidOperationException("Ewelink:ClientId não configurado.");
            _clientSecret = _configuration["Ewelink:ClientSecret"] 
                ?? throw new InvalidOperationException("Ewelink:ClientSecret não configurado.");
        }

        private string HmacSign(string message)
        {
            var key = Encoding.UTF8.GetBytes(_clientSecret);
            var msg = Encoding.UTF8.GetBytes(message);

            using var hmac = new HMACSHA256(key);
            return Convert.ToBase64String(hmac.ComputeHash(msg));
        }

        // Obter URL base do endpoint baseado na região
        private string GetApiBaseUrl(string region)
        {
            return region?.ToLower() switch
            {
                "cn" => "https://cn-apia.coolkit.cn",
                "us" => "https://us-apia.coolkit.cc",
                "eu" => "https://eu-apia.coolkit.cc",
                "as" or _ => "https://as-apia.coolkit.cc" // Padrão: Asia
            };
        }

        public async Task<object> TrocarCodePorTokenAsync(string code, string region = "as")
        {
            long timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            // Nonce deve ter 8 caracteres alfanuméricos
            string nonce = Guid.NewGuid().ToString("N").Substring(0, 8);

            // IMPORTANTE: redirectUrl deve ser EXATAMENTE igual ao usado na URL de autorização
            string redirectUrl = GetRedirectUri();
            
            // Obter URL base do endpoint baseado na região
            string apiBaseUrl = GetApiBaseUrl(region);
            string tokenEndpoint = $"{apiBaseUrl}/v2/user/oauth/token";

            // IMPORTANTE: A ordem dos campos pode importar para a assinatura
            // Usar JObject para garantir ordem específica se necessário
            var body = new
            {
                clientId = _clientId,
                clientSecret = _clientSecret,
                code = code,
                grantType = "authorization_code",
                redirectUrl = redirectUrl
            };

            // Serializar com configuração que preserva a ordem
            var jsonBody = JsonConvert.SerializeObject(body, new JsonSerializerSettings
            {
                NullValueHandling = NullValueHandling.Ignore
            });
            
            // Log do JSON serializado para verificar ordem
            System.Console.WriteLine($"[EWELINK TOKEN] JSON serializado: {jsonBody}");
            // Para POST, a assinatura é baseada no corpo JSON completo
            string sign = HmacSign(jsonBody);

            // Log para debug - IMPORTANTE: Verificar se redirectUrl está correto
            System.Console.WriteLine($"[EWELINK TOKEN REQUEST] region={region}, endpoint={tokenEndpoint}");
            System.Console.WriteLine($"[EWELINK TOKEN REQUEST] redirectUrl={redirectUrl}");
            System.Console.WriteLine($"[EWELINK TOKEN REQUEST] code={code?.Substring(0, Math.Min(20, code?.Length ?? 0))}...");
            System.Console.WriteLine($"[EWELINK TOKEN REQUEST] Body JSON: {jsonBody}");

            var req = new HttpRequestMessage(HttpMethod.Post, tokenEndpoint);
            req.Content = new StringContent(jsonBody, Encoding.UTF8, "application/json");

            // Adicionar header Authorization conforme documentação
            req.Headers.Add("Authorization", $"Sign {sign}");
            req.Headers.Add("x-ck-appid", _clientId);
            req.Headers.Add("x-ck-nonce", nonce);
            req.Headers.Add("x-ck-timestamp", timestamp.ToString());
            req.Headers.Add("x-ck-sign", sign);

            var response = await _http.SendAsync(req);
            var json = await response.Content.ReadAsStringAsync();

            // Log para debug - SEMPRE logar a resposta
            System.Console.WriteLine($"[EWELINK TOKEN] Status: {response.StatusCode}");
            System.Console.WriteLine($"[EWELINK TOKEN] Request redirectUrl: {redirectUrl}");
            System.Console.WriteLine($"[EWELINK TOKEN] Request body: {jsonBody}");
            System.Console.WriteLine($"[EWELINK TOKEN] Response: {json}");
            
            if (!response.IsSuccessStatusCode)
            {
                System.Diagnostics.Debug.WriteLine($"Ewelink Token Error: Status {response.StatusCode}, Response: {json}");
            }

            var result = JsonConvert.DeserializeObject(json);
            
            // Log do resultado parseado
            if (result != null)
            {
                var resultStr = JsonConvert.SerializeObject(result);
                System.Console.WriteLine($"[EWELINK TOKEN] Parsed result: {resultStr}");
            }
            
            return result;
        }

        public async Task<object> RefreshTokenAsync(string refreshToken)
        {
            long timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            // Nonce deve ter exatamente 8 caracteres
            string nonce = Guid.NewGuid().ToString("N").Substring(0, 8);

            var body = new
            {
                clientId = _clientId,
                clientSecret = _clientSecret,
                grantType = "refresh_token",
                refreshToken = refreshToken
            };

            var jsonBody = JsonConvert.SerializeObject(body);
            string sign = HmacSign(jsonBody);

            var req = new HttpRequestMessage(HttpMethod.Post, "https://as-apia.coolkit.cc/v2/user/oauth/refresh");
            req.Content = new StringContent(jsonBody, Encoding.UTF8, "application/json");

            req.Headers.Add("x-ck-appid", _clientId);
            req.Headers.Add("x-ck-nonce", nonce);
            req.Headers.Add("x-ck-timestamp", timestamp.ToString());
            req.Headers.Add("x-ck-sign", sign);

            var response = await _http.SendAsync(req);
            var json = await response.Content.ReadAsStringAsync();

            return JsonConvert.DeserializeObject(json);
        }

        public async Task<object> LoginDiretoAsync(string email, string password, string areaCode = "+55")
        {
            long timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            // O nonce deve ter exatamente 8 caracteres para o login direto
            string nonce = Guid.NewGuid().ToString("N").Substring(0, 8);

            var body = new
            {
                appid = _clientId,
                email = email,
                password = password,
                ts = timestamp,
                version = 8,
                nonce = nonce,
                countryCode = areaCode
            };

            var jsonBody = JsonConvert.SerializeObject(body);
            // Para login direto, a assinatura é baseada no corpo JSON completo (como no OAuth token)
            string sign = HmacSign(jsonBody);

            var req = new HttpRequestMessage(HttpMethod.Post, "https://as-apia.coolkit.cc/v2/user/login");
            req.Content = new StringContent(jsonBody, Encoding.UTF8, "application/json");

            // Adicionar header Authorization com a assinatura
            req.Headers.Add("Authorization", $"Sign {sign}");
            req.Headers.Add("x-ck-appid", _clientId);
            req.Headers.Add("x-ck-nonce", nonce);
            req.Headers.Add("x-ck-timestamp", timestamp.ToString());
            req.Headers.Add("x-ck-sign", sign);

            var response = await _http.SendAsync(req);
            var json = await response.Content.ReadAsStringAsync();

            // Log para debug (remover em produção se necessário)
            if (!response.IsSuccessStatusCode)
            {
                // Log do erro para debug
                System.Diagnostics.Debug.WriteLine($"Ewelink Login Error: Status {response.StatusCode}, Response: {json}");
            }

            return JsonConvert.DeserializeObject(json);
        }

        public async Task<object> ListarFamiliasAsync(string accessToken, string region = "as")
        {
            long timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            // Nonce deve ter exatamente 8 caracteres
            string nonce = Guid.NewGuid().ToString("N").Substring(0, 8);
            string msg = $"{_clientId}{timestamp}";
            string sign = HmacSign(msg);

            string apiBaseUrl = GetApiBaseUrl(region);
            var req = new HttpRequestMessage(HttpMethod.Get, $"{apiBaseUrl}/v2/family");
            req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

            req.Headers.Add("x-ck-appid", _clientId);
            req.Headers.Add("x-ck-nonce", nonce);
            req.Headers.Add("x-ck-timestamp", timestamp.ToString());
            req.Headers.Add("x-ck-sign", sign);

            var response = await _http.SendAsync(req);
            var json = await response.Content.ReadAsStringAsync();
            
            System.Console.WriteLine($"[LISTAR FAMÍLIAS] Região: {region}");
            System.Console.WriteLine($"[LISTAR FAMÍLIAS] Resposta JSON: {json}");

            return JsonConvert.DeserializeObject(json);
        }

        public async Task<object> ListarDispositivosAsync(string accessToken, string familyId, string region = "as")
        {
            long timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            // Nonce deve ter exatamente 8 caracteres
            string nonce = Guid.NewGuid().ToString("N").Substring(0, 8);
            string msg = $"{_clientId}{timestamp}";
            string sign = HmacSign(msg);

            string apiBaseUrl = GetApiBaseUrl(region);
            var req = new HttpRequestMessage(HttpMethod.Get, $"{apiBaseUrl}/v2/device/thing?num=0&familyId={familyId}");
            req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

            req.Headers.Add("x-ck-appid", _clientId);
            req.Headers.Add("x-ck-nonce", nonce);
            req.Headers.Add("x-ck-timestamp", timestamp.ToString());
            req.Headers.Add("x-ck-sign", sign);

            var response = await _http.SendAsync(req);
            var json = await response.Content.ReadAsStringAsync();
            
            System.Console.WriteLine($"[LISTAR DISPOSITIVOS] Família: {familyId}, Região: {region}");
            System.Console.WriteLine($"[LISTAR DISPOSITIVOS] Resposta JSON: {json}");

            return JsonConvert.DeserializeObject(json);
        }

        public async Task<object> ControlarDispositivoAsync(string accessToken, string deviceId, object parameters, string region = "as")
        {
            long timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            // Nonce deve ter exatamente 8 caracteres
            string nonce = Guid.NewGuid().ToString("N").Substring(0, 8);

            // 🔥 CORREÇÃO: Usando a classe específica
            var body = new EwelinkApiControlRequest
            {
                Id = deviceId,
                Params = parameters
            };

            var jsonBody = JsonConvert.SerializeObject(body);

            // Para POST, a assinatura é baseada no corpo JSON completo
            string sign = HmacSign(jsonBody);

            System.Console.WriteLine($"[CONTROLAR DISPOSITIVO API] DeviceId: {deviceId}, Região: {region}");
            System.Console.WriteLine($"[CONTROLAR DISPOSITIVO API] Body JSON: {jsonBody}");
            System.Console.WriteLine($"[CONTROLAR DISPOSITIVO API] Sign: {sign}");

            string apiBaseUrl = GetApiBaseUrl(region);
            var req = new HttpRequestMessage(HttpMethod.Post, $"{apiBaseUrl}/v2/device/thing/status");
            req.Content = new StringContent(jsonBody, Encoding.UTF8, "application/json");
            req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

            req.Headers.Add("x-ck-appid", _clientId);
            req.Headers.Add("x-ck-nonce", nonce);
            req.Headers.Add("x-ck-timestamp", timestamp.ToString());
            req.Headers.Add("x-ck-sign", sign);

            var response = await _http.SendAsync(req);
            var json = await response.Content.ReadAsStringAsync();
            
            System.Console.WriteLine($"[CONTROLAR DISPOSITIVO API] Status: {response.StatusCode}");
            System.Console.WriteLine($"[CONTROLAR DISPOSITIVO API] Resposta: {json}");

            return JsonConvert.DeserializeObject(json);
        }

        // Métodos para trabalhar com banco de dados
        public async Task<EwelinkAccount> SaveOrUpdateAccountAsync(Guid userId, string accessToken, string refreshToken, long accessTokenExpiry, long refreshTokenExpiry, string? region = null)
        {
            var account = await _context.EwelinkAccounts.FirstOrDefaultAsync(a => a.UserId == userId);
            
            if (account == null)
            {
                account = new EwelinkAccount
                {
                    UserId = userId,
                    AccessToken = accessToken,
                    RefreshToken = refreshToken,
                    AccessTokenExpiry = accessTokenExpiry,
                    RefreshTokenExpiry = refreshTokenExpiry,
                    Region = region,
                    CreatedAt = DateTimeOffset.UtcNow,
                    IsActive = true
                };
                _context.EwelinkAccounts.Add(account);
            }
            else
            {
                account.AccessToken = accessToken;
                account.RefreshToken = refreshToken;
                account.AccessTokenExpiry = accessTokenExpiry;
                account.RefreshTokenExpiry = refreshTokenExpiry;
                account.Region = region ?? account.Region;
                account.LastUpdatedAt = DateTimeOffset.UtcNow;
                account.IsActive = true;
            }

            await _context.SaveChangesAsync();
            return account;
        }

        public async Task<EwelinkAccount?> GetAccountByUserIdAsync(Guid userId)
        {
            return await _context.EwelinkAccounts
                .FirstOrDefaultAsync(a => a.UserId == userId && a.IsActive);
        }

        public async Task<List<Entities.EwelinkDevice>> SaveOrUpdateDevicesAsync(Guid userId, List<Entities.EwelinkDevice> devices)
        {
            var savedDevices = new List<Entities.EwelinkDevice>();

            foreach (var device in devices)
            {
                var existingDevice = await _context.EwelinkDevices
                    .FirstOrDefaultAsync(d => d.UserId == userId && d.DeviceId == device.DeviceId);

                if (existingDevice == null)
                {
                    device.UserId = userId;
                    device.CreatedAt = DateTimeOffset.UtcNow;
                    _context.EwelinkDevices.Add(device);
                    savedDevices.Add(device);
                }
                else
                {
                    existingDevice.Name = device.Name;
                    existingDevice.Type = device.Type;
                    existingDevice.Uiid = device.Uiid;
                    existingDevice.Params = device.Params;
                    existingDevice.Online = device.Online;
                    existingDevice.FamilyId = device.FamilyId;
                    existingDevice.RoomId = device.RoomId;
                    existingDevice.LastUpdatedAt = DateTimeOffset.UtcNow;
                    savedDevices.Add(existingDevice);
                }
            }

            await _context.SaveChangesAsync();
            return savedDevices;
        }

        public async Task<List<EwelinkDeviceResponse>> GetUserDevicesAsync(Guid userId)
        {
            // Refresh automático do token antes de listar dispositivos
            await RefreshAccountTokenIfNeededAsync(userId);
            
            var devices = await _context.EwelinkDevices
                .Where(d => d.UserId == userId)
                .ToListAsync();

            var result = new List<EwelinkDeviceResponse>();

            foreach (var device in devices)
            {
                dynamic? paramsObj = null;
                if (!string.IsNullOrEmpty(device.Params))
                {
                    paramsObj = JsonConvert.DeserializeObject(device.Params);
                }

                bool isOn = false;
                if (paramsObj != null)
                {
                    try
                    {
                        var jObject = paramsObj as JObject;
                        if (jObject != null)
                        {
                            var switchValue = jObject["switch"]?.ToString();
                            isOn = switchValue == "on";
                        }
                    }
                    catch
                    {
                        // Ignorar erro se não conseguir acessar a propriedade switch
                    }
                }

                result.Add(new EwelinkDeviceResponse
                {
                    Id = device.Id,
                    DeviceId = device.DeviceId,
                    Name = device.Name,
                    Type = device.Type,
                    Uiid = device.Uiid,
                    Params = paramsObj,
                    Online = device.Online,
                    FamilyId = device.FamilyId,
                    RoomId = device.RoomId,
                    IsOn = isOn
                });
            }

            return result;
        }

        public async Task<EwelinkDeviceResponse?> GetDeviceStatusAsync(Guid userId, string deviceId)
        {
            var device = await _context.EwelinkDevices
                .FirstOrDefaultAsync(d => d.UserId == userId && d.DeviceId == deviceId);

            if (device == null)
                return null;

            var account = await GetAccountByUserIdAsync(userId);
            if (account == null)
                return null;

            // Atualizar status do dispositivo da API
            try
            {
                await RefreshAccountTokenIfNeededAsync(userId);
                account = await GetAccountByUserIdAsync(userId);
                if (account == null) return null;

                var region = account.Region ?? "as";
                var familias = await ListarFamiliasAsync(account.AccessToken, region);
                if (familias != null)
                {
                    var familiasList = JsonConvert.DeserializeObject<dynamic>(JsonConvert.SerializeObject(familias));
                    if (familiasList?.familyList != null)
                    {
                        foreach (var familia in familiasList.familyList)
                        {
                            var dispositivos = await ListarDispositivosAsync(account.AccessToken, familia.id.ToString(), region);
                            if (dispositivos != null)
                            {
                                var dispositivosList = JsonConvert.DeserializeObject<dynamic>(JsonConvert.SerializeObject(dispositivos));
                                if (dispositivosList?.thingList != null)
                                {
                                    foreach (var dev in dispositivosList.thingList)
                                    {
                                        if (dev.itemData?.deviceid?.ToString() == deviceId)
                                        {
                                            // Atualizar dispositivo no banco
                                            device.Name = dev.itemData?.name?.ToString() ?? device.Name;
                                            device.Online = dev.itemData?.online == true;
                                            device.Params = JsonConvert.SerializeObject(dev.itemData?.@params);
                                            device.LastUpdatedAt = DateTimeOffset.UtcNow;
                                            await _context.SaveChangesAsync();
                                            break;
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
            catch
            {
                // Se falhar, retorna o status do banco
            }

            dynamic? paramsObj = null;
            if (!string.IsNullOrEmpty(device.Params))
            {
                paramsObj = JsonConvert.DeserializeObject(device.Params);
            }

            bool isOn = false;
            if (paramsObj != null)
            {
                    try
                    {
                        var jObject = paramsObj as JObject;
                        if (jObject != null)
                        {
                            var switchValue = jObject["switch"]?.ToString();
                            isOn = switchValue == "on";
                        }
                    }
                    catch
                    {
                        // Ignorar erro se não conseguir acessar a propriedade switch
                    }
            }

            return new EwelinkDeviceResponse
            {
                Id = device.Id,
                DeviceId = device.DeviceId,
                Name = device.Name,
                Type = device.Type,
                Uiid = device.Uiid,
                Params = paramsObj,
                Online = device.Online,
                FamilyId = device.FamilyId,
                RoomId = device.RoomId,
                IsOn = isOn
            };
        }

        public async Task<bool> ControlDeviceAsync(Guid userId, string deviceId, bool switchOn)
        {
            var account = await GetAccountByUserIdAsync(userId);
            if (account == null)
                return false;

            var device = await _context.EwelinkDevices
                .FirstOrDefaultAsync(d => d.UserId == userId && d.DeviceId == deviceId);
            
            if (device == null)
                return false;
            
            var region = account.Region ?? "as";

            var parameters = new { @switch = switchOn ? "on" : "off" };
            var result = await ControlarDispositivoAsync(account.AccessToken, deviceId, parameters, region);

            // Atualizar status no banco após comando
            if (result != null)
            {
                var resultObj = JsonConvert.DeserializeObject<dynamic>(JsonConvert.SerializeObject(result));
                if (resultObj?.error == 0)
                {
                    // Atualizar params do dispositivo
                    dynamic? currentParams = null;
                    if (!string.IsNullOrEmpty(device.Params))
                    {
                        currentParams = JsonConvert.DeserializeObject(device.Params);
                    }
                    else
                    {
                        currentParams = new { };
                    }

                    if (currentParams != null)
                    {
                        // Usar JObject para manipular propriedade switch
                        var jObject = currentParams as JObject;
                        if (jObject != null)
                        {
                            jObject["switch"] = switchOn ? "on" : "off";
                            device.Params = jObject.ToString();
                        }
                        else
                        {
                            // Tentar deserializar como JObject
                            try
                            {
                                jObject = JsonConvert.DeserializeObject<JObject>(device.Params);
                                if (jObject != null)
                                {
                                    jObject["switch"] = switchOn ? "on" : "off";
                                    device.Params = jObject.ToString();
                                }
                            }
                            catch
                            {
                                // Se falhar, criar novo objeto
                                jObject = new JObject();
                                jObject["switch"] = switchOn ? "on" : "off";
                                device.Params = jObject.ToString();
                            }
                        }
                        device.LastUpdatedAt = DateTimeOffset.UtcNow;
                        await _context.SaveChangesAsync();
                    }
                    return true;
                }
            }

            return false;
        }

        public async Task<bool> RefreshAccountTokenIfNeededAsync(Guid userId)
        {
            var account = await GetAccountByUserIdAsync(userId);
            if (account == null)
                return false;

            // Verificar se o token está expirado (com margem de 5 minutos)
            var expiryTime = DateTimeOffset.FromUnixTimeMilliseconds(account.AccessTokenExpiry);
            if (DateTimeOffset.UtcNow.AddMinutes(5) >= expiryTime)
            {
                try
                {
                    var refreshResult = await RefreshTokenAsync(account.RefreshToken);
                    if (refreshResult != null)
                    {
                        var resultObj = JsonConvert.DeserializeObject<dynamic>(JsonConvert.SerializeObject(refreshResult));
                        if (resultObj?.error == 0 && resultObj?.data != null)
                        {
                            var data = resultObj.data;
                            await SaveOrUpdateAccountAsync(
                                userId,
                                data.at?.ToString() ?? account.AccessToken,
                                data.rt?.ToString() ?? account.RefreshToken,
                                (long)(data.atExpiredAt ?? account.AccessTokenExpiry),
                                (long)(data.rtExpiredAt ?? account.RefreshTokenExpiry),
                                data.region?.ToString() ?? account.Region
                            );
                            return true;
                        }
                    }
                }
                catch
                {
                    return false;
                }
            }

            return true;
        }
    }
}