using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using StarkAid.Web.DTOs;

namespace StarkAid.Web.Services
{
    public class AdminService
    {
        private readonly HttpClient _http;

        public AdminService(HttpClient http)
        {
            _http = http;
        }

        private void SetHeaders(string token, string apiKey)
        {
            _http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            if (!_http.DefaultRequestHeaders.Contains("Api-Key"))
            {
                _http.DefaultRequestHeaders.Add("Api-Key", apiKey);
            }
        }

        public async Task<AdminStatsDto?> GetStatsAsync(string token, string apiKey)
        {
            var request = new HttpRequestMessage(HttpMethod.Get, "api/v1/Admin/stats");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            request.Headers.Add("Api-Key", apiKey);

            var response = await _http.SendAsync(request);
            if (!response.IsSuccessStatusCode) return null;

            return await response.Content.ReadFromJsonAsync<AdminStatsDto>(new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        }

        public async Task<List<UserWithPlanDto>> GetUsersWithPlansAsync(string token, string apiKey)
        {
            var request = new HttpRequestMessage(HttpMethod.Get, "api/v1/Admin/users-with-plans");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            request.Headers.Add("Api-Key", apiKey);

            var response = await _http.SendAsync(request);
            if (!response.IsSuccessStatusCode) return new List<UserWithPlanDto>();

            return await response.Content.ReadFromJsonAsync<List<UserWithPlanDto>>(new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? new List<UserWithPlanDto>();
        }

        public async Task<List<AdminUserListDto>> GetAllUsersAsync(string token, string apiKey)
        {
            var request = new HttpRequestMessage(HttpMethod.Get, "api/v1/Admin/users");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            request.Headers.Add("Api-Key", apiKey);

            var response = await _http.SendAsync(request);
            if (!response.IsSuccessStatusCode) return new List<AdminUserListDto>();

            return await response.Content.ReadFromJsonAsync<List<AdminUserListDto>>(new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? new List<AdminUserListDto>();
        }

        public async Task<UserDashboardDto?> GetUserDashboardAsync(Guid userId, string token, string apiKey)
        {
            var request = new HttpRequestMessage(HttpMethod.Get, $"api/v1/Admin/users/{userId}/dashboard");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            request.Headers.Add("Api-Key", apiKey);

            var response = await _http.SendAsync(request);
            if (!response.IsSuccessStatusCode) return null;

            return await response.Content.ReadFromJsonAsync<UserDashboardDto>(new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        }

        // Maintenance Methods (ManutencaoController)

        public async Task<bool> IniciarManutencaoSoftwareAsync(Guid userId, string token, string apiKey)
        {
            var request = new HttpRequestMessage(HttpMethod.Post, "api/v1/manutencao/software/iniciar");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            request.Headers.Add("Api-Key", apiKey);
            request.Content = JsonContent.Create(new { UserId = userId });

            var response = await _http.SendAsync(request);
            return response.IsSuccessStatusCode;
        }

        public async Task<List<DispositivoEspDto>> GetDispositivosManutencaoAsync(Guid userId, string token, string apiKey)
        {
            var request = new HttpRequestMessage(HttpMethod.Get, $"api/v1/manutencao/software/dispositivos/{userId}");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            request.Headers.Add("Api-Key", apiKey);

            var response = await _http.SendAsync(request);
            if (!response.IsSuccessStatusCode) return new List<DispositivoEspDto>();

            return await response.Content.ReadFromJsonAsync<List<DispositivoEspDto>>(new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? new List<DispositivoEspDto>();
        }

        public async Task<List<DeviceDto>> GetStarkSwitchDevicesAsync(Guid userId, string token, string apiKey)
        {
            var request = new HttpRequestMessage(HttpMethod.Get, $"api/v1/Admin/users/{userId}/details"); // This endpoint returns many things, but we can extract devices
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            request.Headers.Add("Api-Key", apiKey);

            var response = await _http.SendAsync(request);
             if (!response.IsSuccessStatusCode) return new List<DeviceDto>();

            // The endpoint returns an anonymous object with 'devices' property
            var json = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(json);
            if(doc.RootElement.TryGetProperty("devices", out var devicesProp))
            {
                return JsonSerializer.Deserialize<List<DeviceDto>>(devicesProp.GetRawText(), new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? new List<DeviceDto>();
            }
            return new List<DeviceDto>();
        }

         public async Task<List<ComandoSocialDto>> GetComandosSociaisManutencaoAsync(Guid userId, string token, string apiKey)
        {
            var request = new HttpRequestMessage(HttpMethod.Get, $"api/v1/manutencao/software/comandos-sociais/{userId}");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            request.Headers.Add("Api-Key", apiKey);

            var response = await _http.SendAsync(request);
             if (!response.IsSuccessStatusCode) return new List<ComandoSocialDto>();

            return await response.Content.ReadFromJsonAsync<List<ComandoSocialDto>>(new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? new List<ComandoSocialDto>();
        }

        public async Task<bool> ClearCacheSoftAsync(Guid userId, string token, string apiKey)
        {
            var request = new HttpRequestMessage(HttpMethod.Post, "api/v1/manutencao/software/limpar-cache");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            request.Headers.Add("Api-Key", apiKey);
            request.Content = JsonContent.Create(new { UserId = userId });
            
            var response = await _http.SendAsync(request);
            return response.IsSuccessStatusCode;
        }

        public async Task<bool> ClearDataSoftAsync(Guid userId, string token, string apiKey)
        {
            var request = new HttpRequestMessage(HttpMethod.Post, "api/v1/manutencao/software/limpar-dados");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            request.Headers.Add("Api-Key", apiKey);
             request.Content = JsonContent.Create(new { UserId = userId });

            var response = await _http.SendAsync(request);
            return response.IsSuccessStatusCode;
        }

        public async Task<bool> LogoutSoftAsync(Guid userId, string token, string apiKey)
        {
             var request = new HttpRequestMessage(HttpMethod.Post, "api/v1/manutencao/software/logout");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            request.Headers.Add("Api-Key", apiKey);
             request.Content = JsonContent.Create(new { UserId = userId });

            var response = await _http.SendAsync(request);
            return response.IsSuccessStatusCode;
        }


        public async Task<bool> ClearCacheAppAsync(Guid userId, string token, string apiKey)
        {
            var request = new HttpRequestMessage(HttpMethod.Post, "api/v1/manutencao/app/limpar-cache");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            request.Headers.Add("Api-Key", apiKey);
             request.Content = JsonContent.Create(new { UserId = userId });

            var response = await _http.SendAsync(request);
            return response.IsSuccessStatusCode;
        }

         public async Task<bool> ClearDataAppAsync(Guid userId, string token, string apiKey)
        {
            var request = new HttpRequestMessage(HttpMethod.Post, "api/v1/manutencao/app/limpar-dados");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            request.Headers.Add("Api-Key", apiKey);
             request.Content = JsonContent.Create(new { UserId = userId });

            var response = await _http.SendAsync(request);
            return response.IsSuccessStatusCode;
        }

        public async Task<bool> LogoutAppAsync(Guid userId, string token, string apiKey)
        {
             var request = new HttpRequestMessage(HttpMethod.Post, "api/v1/manutencao/app/logout");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            request.Headers.Add("Api-Key", apiKey);
             request.Content = JsonContent.Create(new { UserId = userId });

            var response = await _http.SendAsync(request);
            return response.IsSuccessStatusCode;
        }

        public async Task<bool> RestartAppAsync(Guid userId, string token, string apiKey)
        {
            var request = new HttpRequestMessage(HttpMethod.Post, "api/v1/manutencao/app/reiniciar");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            request.Headers.Add("Api-Key", apiKey);
            request.Content = JsonContent.Create(new { UserId = userId });

            var response = await _http.SendAsync(request);
            return response.IsSuccessStatusCode;
        }

        public async Task<bool> DropDatabaseAsync(Guid userId, string token, string apiKey)
        {
            var request = new HttpRequestMessage(HttpMethod.Post, "api/v1/manutencao/app/limpar-banco");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            request.Headers.Add("Api-Key", apiKey);
            request.Content = JsonContent.Create(new { UserId = userId });

            var response = await _http.SendAsync(request);
            return response.IsSuccessStatusCode;
        }

        public async Task<bool> SendAlertAsync(Guid userId, string message, string token, string apiKey)
        {
            var request = new HttpRequestMessage(HttpMethod.Post, "api/v1/manutencao/app/enviar-alerta");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            request.Headers.Add("Api-Key", apiKey);
            request.Content = JsonContent.Create(new
            {
                UserId = userId,
                Message = message
            });

            var response = await _http.SendAsync(request);
            return response.IsSuccessStatusCode;
        }

        public async Task<bool> CreateComandoSocialAsync(ComandoSocialDto comando, string token, string apiKey)
        {
            var request = new HttpRequestMessage(HttpMethod.Post, "api/v1/Admin/comandos-sociais");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            request.Headers.Add("Api-Key", apiKey);
            request.Content = JsonContent.Create(comando);

            var response = await _http.SendAsync(request);
            return response.IsSuccessStatusCode;
        }

        public async Task<bool> UpdateComandoSocialAsync(ComandoSocialDto comando, string token, string apiKey)
        {
            var request = new HttpRequestMessage(HttpMethod.Put, $"api/v1/Admin/comandos-sociais/{comando.Id}");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            request.Headers.Add("Api-Key", apiKey);
            request.Content = JsonContent.Create(comando);

            var response = await _http.SendAsync(request);
            return response.IsSuccessStatusCode;
        }

        public async Task<bool> DeleteComandoSocialAsync(Guid id, string token, string apiKey)
        {
            var request = new HttpRequestMessage(HttpMethod.Delete, $"api/v1/Admin/comandos-sociais/{id}");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            request.Headers.Add("Api-Key", apiKey);

            var response = await _http.SendAsync(request);
            return response.IsSuccessStatusCode;
        }

        public async Task<bool> UpdateDeviceAsync(Guid deviceId, AdminUpdateDeviceRequest device, string token, string apiKey)
        {
            var request = new HttpRequestMessage(HttpMethod.Put, $"api/v1/Admin/devices/{deviceId}");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            request.Headers.Add("Api-Key", apiKey);
            request.Content = JsonContent.Create(device);

            var response = await _http.SendAsync(request);
            return response.IsSuccessStatusCode;
        }

        public async Task<bool> DeleteDeviceAsync(Guid deviceId, string token, string apiKey)
        {
            var request = new HttpRequestMessage(HttpMethod.Delete, $"api/v1/Admin/devices/{deviceId}");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            request.Headers.Add("Api-Key", apiKey);

            var response = await _http.SendAsync(request);
            return response.IsSuccessStatusCode;
        }

        public async Task<List<ErrorLogDto>> GetAppLogsAsync(Guid userId, string token, string apiKey)
        {
            var request = new HttpRequestMessage(HttpMethod.Get, $"api/v1/Admin/error-logs/app/{userId}");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            request.Headers.Add("Api-Key", apiKey);

            var response = await _http.SendAsync(request);
            if (!response.IsSuccessStatusCode) return new List<ErrorLogDto>();

            return await response.Content.ReadFromJsonAsync<List<ErrorLogDto>>(new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? new List<ErrorLogDto>();
        }

        public async Task<List<ErrorLogDto>> GetSoftLogsAsync(Guid userId, string token, string apiKey)
        {
            var request = new HttpRequestMessage(HttpMethod.Get, $"api/v1/Admin/error-logs/soft/{userId}");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            request.Headers.Add("Api-Key", apiKey);

            var response = await _http.SendAsync(request);
            if (!response.IsSuccessStatusCode) return new List<ErrorLogDto>();

            return await response.Content.ReadFromJsonAsync<List<ErrorLogDto>>(new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? new List<ErrorLogDto>();
        }

        public async Task<bool> UpdateDispositivoEspAsync(Guid id, UpdateDispositivoEspDto device, string token, string apiKey)
        {
            var request = new HttpRequestMessage(HttpMethod.Put, $"api/v1/DispositivosEsp/{id}");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            request.Headers.Add("Api-Key", apiKey);
            request.Content = JsonContent.Create(device);

            var response = await _http.SendAsync(request);
            return response.IsSuccessStatusCode;
        }

        public async Task<bool> DeleteDispositivoEspAsync(Guid id, string token, string apiKey)
        {
            var request = new HttpRequestMessage(HttpMethod.Delete, $"api/v1/DispositivosEsp/{id}");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            request.Headers.Add("Api-Key", apiKey);

            var response = await _http.SendAsync(request);
            return response.IsSuccessStatusCode;
        }

        public async Task<bool> PingDeviceAsync(Guid id, string token, string apiKey)
        {
            var request = new HttpRequestMessage(HttpMethod.Post, $"api/v1/DispositivosEsp/{id}/ping");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            request.Headers.Add("Api-Key", apiKey);

            var response = await _http.SendAsync(request);
            return response.IsSuccessStatusCode;
        }

        // ========== ADMIN CONFIG & MUSIC CACHE ==========
        public async Task<SystemConfigDto?> GetSystemConfigAsync(string token, string apiKey)
        {
            var request = new HttpRequestMessage(HttpMethod.Get, "api/v1/Admin/config");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            request.Headers.Add("Api-Key", apiKey);
            var response = await _http.SendAsync(request);
            if (!response.IsSuccessStatusCode) return null;
            return await response.Content.ReadFromJsonAsync<SystemConfigDto>(new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        }

        public async Task<bool> UpdateSystemConfigAsync(SystemConfigDto dto, string token, string apiKey)
        {
            var request = new HttpRequestMessage(HttpMethod.Put, "api/v1/Admin/config");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            request.Headers.Add("Api-Key", apiKey);
            request.Content = JsonContent.Create(dto);
            var response = await _http.SendAsync(request);
            return response.IsSuccessStatusCode;
        }

        public async Task<List<YouTubeMusicCacheDto>> GetMusicCacheAsync(string token, string apiKey)
        {
            var request = new HttpRequestMessage(HttpMethod.Get, "api/v1/Admin/music-cache");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            request.Headers.Add("Api-Key", apiKey);
            var response = await _http.SendAsync(request);
            if (!response.IsSuccessStatusCode) return new List<YouTubeMusicCacheDto>();
            return await response.Content.ReadFromJsonAsync<List<YouTubeMusicCacheDto>>(new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? new List<YouTubeMusicCacheDto>();
        }

        public async Task<bool> CreateMusicCacheAsync(YouTubeMusicCacheDto dto, string token, string apiKey)
        {
            var request = new HttpRequestMessage(HttpMethod.Post, "api/v1/Admin/music-cache");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            request.Headers.Add("Api-Key", apiKey);
            request.Content = JsonContent.Create(dto);
            var response = await _http.SendAsync(request);
            return response.IsSuccessStatusCode;
        }

        public async Task<bool> UpdateMusicCacheAsync(int id, YouTubeMusicCacheDto dto, string token, string apiKey)
        {
            var request = new HttpRequestMessage(HttpMethod.Put, $"api/v1/Admin/music-cache/{id}");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            request.Headers.Add("Api-Key", apiKey);
            request.Content = JsonContent.Create(dto);
            var response = await _http.SendAsync(request);
            return response.IsSuccessStatusCode;
        }

        public async Task<bool> DeleteMusicCacheAsync(int id, string token, string apiKey)
        {
            var request = new HttpRequestMessage(HttpMethod.Delete, $"api/v1/Admin/music-cache/{id}");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            request.Headers.Add("Api-Key", apiKey);
            var response = await _http.SendAsync(request);
            return response.IsSuccessStatusCode;
        }

        public async Task<List<MergeSuggestionDto>> GetMergeSuggestionsAsync(string token, string apiKey)
        {
            var request = new HttpRequestMessage(HttpMethod.Get, "api/v1/Admin/music-cache/merge-suggestions");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            request.Headers.Add("Api-Key", apiKey);
            var response = await _http.SendAsync(request);
            if (!response.IsSuccessStatusCode) return new List<MergeSuggestionDto>();
            return await response.Content.ReadFromJsonAsync<List<MergeSuggestionDto>>(new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? new List<MergeSuggestionDto>();
        }

        public async Task<bool> ExecuteMergeAsync(MergeExecutionRequest dto, string token, string apiKey)
        {
            var request = new HttpRequestMessage(HttpMethod.Post, "api/v1/Admin/music-cache/merge");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            request.Headers.Add("Api-Key", apiKey);
            request.Content = JsonContent.Create(dto);
            var response = await _http.SendAsync(request);
            return response.IsSuccessStatusCode;
        }
    }
}

