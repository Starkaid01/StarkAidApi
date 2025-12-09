using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using StarkAid.WindowsForms.Models;

namespace StarkAid.WindowsForms.Services;

public class ApiService
{
    private readonly HttpClient _httpClient;
    private string _baseUrl = "https://starkaid.runasp.net/api/";
    private string? _token;
    private bool _configLoaded = false;
    private readonly object _configLock = new object();

    public ApiService()
    {
        _httpClient = new HttpClient();
        // Não definir BaseAddress para ter controle total sobre as URLs
        _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        _httpClient.Timeout = TimeSpan.FromSeconds(30);
        
        // Carregar configuração em background (não bloqueia inicialização)
        _ = LoadConfigAsync();
    }

    private async Task LoadConfigAsync()
    {
        try
        {
            // URL padrão apenas para buscar configuração inicial
            var configUrl = "https://starkaid.runasp.net/api/Config/app-config";
            var response = await _httpClient.GetAsync(configUrl);
            
            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                var config = JsonConvert.DeserializeObject<AppConfig>(content);
                
                if (config != null && !string.IsNullOrEmpty(config.ApiBaseUrl))
                {
                    lock (_configLock)
                    {
                        // Atualizar base URL com a retornada pela API
                        var newBaseUrl = config.ApiBaseUrl.TrimEnd('/');
                        if (!newBaseUrl.EndsWith("/api"))
                        {
                            newBaseUrl += "/api";
                        }
                        _baseUrl = newBaseUrl + "/";
                        _configLoaded = true;
                        System.Diagnostics.Debug.WriteLine($"[ApiService] Base URL atualizada para: {_baseUrl}");
                    }
                }
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[ApiService] Erro ao carregar configuração: {ex.Message}. Usando URL padrão.");
        }
    }

    public async Task EnsureConfigLoadedAsync()
    {
        if (!_configLoaded)
        {
            await LoadConfigAsync();
        }
    }

    public void SetToken(string token)
    {
        _token = token;
        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
    }

    public void ClearToken()
    {
        _token = null;
        _httpClient.DefaultRequestHeaders.Authorization = null;
    }

    public string? GetToken() => _token;
    
    public string? GetAuthToken() => _token;
    
    public string GetBaseUrl() => _baseUrl.Replace("/api/", "");

    public async Task<T?> GetAsync<T>(string endpoint)
    {
        try
        {
            var url = BuildUrl(endpoint);
            var response = await _httpClient.GetAsync(url);
            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                return JsonConvert.DeserializeObject<T>(content);
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Erro ao fazer GET: {ex.Message}");
        }
        return default(T);
    }

    public async Task<T?> PostAsync<T>(string endpoint, object? data = null)
    {
        try
        {
            var url = BuildUrl(endpoint);
            HttpContent? content = null;
            
            if (data != null)
            {
                var json = JsonConvert.SerializeObject(data);
                content = new StringContent(json, Encoding.UTF8, "application/json");
            }

            var response = await _httpClient.PostAsync(url, content);
            if (response.IsSuccessStatusCode)
            {
                var responseContent = await response.Content.ReadAsStringAsync();
                return JsonConvert.DeserializeObject<T>(responseContent);
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Erro ao fazer POST: {ex.Message}");
        }
        return default(T);
    }

    private string BuildUrl(string endpoint)
    {
        // Remove barra inicial se existir
        if (endpoint.StartsWith("/"))
            endpoint = endpoint.Substring(1);
        
        // Garante que a base URL não termina com barra
        var baseUrl = _baseUrl.TrimEnd('/');
        
        return $"{baseUrl}/{endpoint}";
    }

    // Auth
    public async Task<LoginResponse?> LoginAsync(LoginRequest request)
    {
        try
        {
            // Construir URL completa para garantir que está correta
            var url = BuildUrl("Auth/login");
            System.Diagnostics.Debug.WriteLine($"Tentando login em: {url}");
            
            var json = JsonConvert.SerializeObject(request);
            System.Diagnostics.Debug.WriteLine($"Request body: {json}");
            
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            var response = await _httpClient.PostAsync(url, content);
            
            System.Diagnostics.Debug.WriteLine($"Status code: {response.StatusCode}");
            
            if (response.IsSuccessStatusCode)
            {
                var responseContent = await response.Content.ReadAsStringAsync();
                System.Diagnostics.Debug.WriteLine($"Response: {responseContent}");
                
                // Usar JObject para evitar problemas com dynamic
                var result = Newtonsoft.Json.Linq.JObject.Parse(responseContent);
                
                if (result != null)
                {
                    var userObj = result["user"];
                    return new LoginResponse
                    {
                        Token = result["token"]?.ToString() ?? "",
                        RefreshToken = result["refreshToken"]?.ToString() ?? "",
                        User = new User
                        {
                            Id = Guid.Parse(userObj?["id"]?.ToString() ?? Guid.Empty.ToString()),
                            Name = userObj?["name"]?.ToString() ?? "",
                            Email = userObj?["email"]?.ToString() ?? "",
                            ApiKey = userObj?["apiKey"]?.ToString() ?? "",
                            StarkCoins = userObj?["starkCoins"] != null ? Convert.ToDecimal(userObj["starkCoins"]) : 0
                        }
                    };
                }
            }
            else
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                System.Diagnostics.Debug.WriteLine($"Erro no login: {response.StatusCode} - {errorContent}");
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Erro no login: {ex.Message}");
            System.Diagnostics.Debug.WriteLine($"Stack trace: {ex.StackTrace}");
        }
        return null;
    }

    // Comandos Sociais
    public async Task<List<ComandoSocial>> GetComandosSociaisAsync()
    {
        try
        {
            var response = await _httpClient.GetAsync(BuildUrl("ComandosSociais"));
            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                return JsonConvert.DeserializeObject<List<ComandoSocial>>(content) ?? new List<ComandoSocial>();
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Erro ao buscar comandos sociais: {ex.Message}");
        }
        return new List<ComandoSocial>();
    }

    public async Task<ComandoSocial?> CreateComandoSocialAsync(ComandoSocial comando)
    {
        try
        {
            var request = new { comando.Comando, comando.Resposta, Estilo = "" };
            var json = JsonConvert.SerializeObject(request);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            var response = await _httpClient.PostAsync(BuildUrl("ComandosSociais"), content);
            
            if (response.IsSuccessStatusCode)
            {
                var responseContent = await response.Content.ReadAsStringAsync();
                return JsonConvert.DeserializeObject<ComandoSocial>(responseContent);
            }
            else
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                System.Diagnostics.Debug.WriteLine($"Erro ao criar comando: {response.StatusCode} - {errorContent}");
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Erro ao criar comando social: {ex.Message}");
        }
        return null;
    }

    public async Task<bool> UpdateComandoSocialAsync(Guid id, ComandoSocial comando)
    {
        try
        {
            var request = new { comando.Comando, comando.Resposta, Estilo = "" };
            var json = JsonConvert.SerializeObject(request);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            var response = await _httpClient.PutAsync(BuildUrl($"ComandosSociais/{id}"), content);
            return response.IsSuccessStatusCode || response.StatusCode == System.Net.HttpStatusCode.NoContent;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Erro ao atualizar comando social: {ex.Message}");
        }
        return false;
    }

    public async Task<bool> DeleteComandoSocialAsync(Guid id)
    {
        try
        {
            var response = await _httpClient.DeleteAsync(BuildUrl($"ComandosSociais/{id}"));
            return response.IsSuccessStatusCode || response.StatusCode == System.Net.HttpStatusCode.NoContent;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Erro ao deletar comando social: {ex.Message}");
        }
        return false;
    }

    // Dispositivos (StarkSwitch)
    public async Task<List<Device>> GetDevicesAsync()
    {
        try
        {
            var response = await _httpClient.GetAsync(BuildUrl("Devices"));
            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                return JsonConvert.DeserializeObject<List<Device>>(content) ?? new List<Device>();
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Erro ao buscar dispositivos: {ex.Message}");
        }
        return new List<Device>();
    }

    public async Task<Device?> CreateDeviceAsync(string name, string comando)
    {
        try
        {
            var request = new { Name = name, Comando = comando };
            var json = JsonConvert.SerializeObject(request);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            var response = await _httpClient.PostAsync(BuildUrl("Devices"), content);
            
            if (response.IsSuccessStatusCode)
            {
                var responseContent = await response.Content.ReadAsStringAsync();
                return JsonConvert.DeserializeObject<Device>(responseContent);
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Erro ao criar dispositivo: {ex.Message}");
        }
        return null;
    }

    public async Task<bool> UpdateDeviceAsync(Guid id, string name, string comando)
    {
        try
        {
            var request = new { NewName = name, NewComando = comando };
            var json = JsonConvert.SerializeObject(request);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            var response = await _httpClient.PutAsync(BuildUrl($"Devices/{id}"), content);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Erro ao atualizar dispositivo: {ex.Message}");
        }
        return false;
    }

    public async Task<bool> DeleteDeviceAsync(Guid id)
    {
        try
        {
            var response = await _httpClient.DeleteAsync(BuildUrl($"Devices/{id}"));
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Erro ao deletar dispositivo: {ex.Message}");
        }
        return false;
    }

    // Dispositivos ESP
    public async Task<List<DispositivoEsp>> GetDispositivosEspAsync()
    {
        try
        {
            var response = await _httpClient.GetAsync(BuildUrl("DispositivosEsp"));
            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                return JsonConvert.DeserializeObject<List<DispositivoEsp>>(content) ?? new List<DispositivoEsp>();
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Erro ao buscar dispositivos ESP: {ex.Message}");
        }
        return new List<DispositivoEsp>();
    }

    public async Task<DispositivoEsp?> CreateDispositivoEspAsync(DispositivoEsp dispositivo)
    {
        try
        {
            var request = new { dispositivo.Nome, dispositivo.Ip, dispositivo.Porta, dispositivo.Comando, dispositivo.ComandToEsp };
            var json = JsonConvert.SerializeObject(request);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            var response = await _httpClient.PostAsync(BuildUrl("DispositivosEsp"), content);
            
            if (response.IsSuccessStatusCode)
            {
                var responseContent = await response.Content.ReadAsStringAsync();
                return JsonConvert.DeserializeObject<DispositivoEsp>(responseContent);
            }
            else
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                System.Diagnostics.Debug.WriteLine($"Erro ao criar dispositivo ESP: {response.StatusCode} - {errorContent}");
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Erro ao criar dispositivo ESP: {ex.Message}");
        }
        return null;
    }

    public async Task<bool> UpdateDispositivoEspAsync(Guid id, DispositivoEsp dispositivo)
    {
        try
        {
            var request = new 
            { 
                Nome = dispositivo.Nome, 
                Ip = dispositivo.Ip, 
                Porta = dispositivo.Porta, 
                Comando = dispositivo.Comando,
                ComandToEsp = dispositivo.ComandToEsp,
                Status = dispositivo.Status,
                LigadoDesligado = dispositivo.LigadoDesligado
            };
            var json = JsonConvert.SerializeObject(request);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            var response = await _httpClient.PutAsync(BuildUrl($"DispositivosEsp/{id}"), content);
            return response.IsSuccessStatusCode || response.StatusCode == System.Net.HttpStatusCode.NoContent;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Erro ao atualizar dispositivo ESP: {ex.Message}");
        }
        return false;
    }

    public async Task<bool> DeleteDispositivoEspAsync(Guid id)
    {
        try
        {
            var response = await _httpClient.DeleteAsync(BuildUrl($"DispositivosEsp/{id}"));
            return response.IsSuccessStatusCode || response.StatusCode == System.Net.HttpStatusCode.NoContent;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Erro ao deletar dispositivo ESP: {ex.Message}");
        }
        return false;
    }

    // User
    public async Task<User?> GetCurrentUserAsync()
    {
        try
        {
            // Tentar usar query string para evitar conflito com rota {id}
            var url = BuildUrl("Users/me");
            System.Diagnostics.Debug.WriteLine($"GetCurrentUserAsync - URL: {url}");
            
            // Usar HttpMethod.Get com URL completa
            var request = new HttpRequestMessage(HttpMethod.Get, url);
            var response = await _httpClient.SendAsync(request);
            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                System.Diagnostics.Debug.WriteLine($"Resposta GetCurrentUserAsync: {content}");
                var user = JsonConvert.DeserializeObject<User>(content);
                if (user != null)
                {
                    System.Diagnostics.Debug.WriteLine($"User deserializado: Name={user.Name}, Email={user.Email}, StarkCoins={user.StarkCoins}");
                }
                return user;
            }
            else
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                System.Diagnostics.Debug.WriteLine($"Erro ao buscar usuário: {response.StatusCode} - {errorContent}");
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Erro ao buscar usuário: {ex.Message}");
            System.Diagnostics.Debug.WriteLine($"Stack trace: {ex.StackTrace}");
        }
        return null;
    }

    public async Task<bool> UpdateUserAsync(string name, string email, string? estado = null, string? cidade = null, string? bairro = null)
    {
        try
        {
            var request = new { Name = name, Email = email, Estado = estado, Cidade = cidade, Bairro = bairro };
            var json = JsonConvert.SerializeObject(request);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            var response = await _httpClient.PutAsync(BuildUrl("Users/me"), content);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Erro ao atualizar usuário: {ex.Message}");
        }
        return false;
    }

    public async Task<UserStats?> GetUserStatsAsync()
    {
        try
        {
            // Tentar usar query string para evitar conflito com rota {id}
            var url = BuildUrl("Users/stats");
            System.Diagnostics.Debug.WriteLine($"GetUserStatsAsync - URL: {url}");
            
            // Usar HttpMethod.Get com URL completa
            var request = new HttpRequestMessage(HttpMethod.Get, url);
            var response = await _httpClient.SendAsync(request);
            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                System.Diagnostics.Debug.WriteLine($"Resposta GetUserStatsAsync: {content}");
                var stats = JsonConvert.DeserializeObject<UserStats>(content);
                if (stats != null)
                {
                    System.Diagnostics.Debug.WriteLine($"Stats deserializado: Devices={stats.TotalDevices}, Comandos={stats.TotalComandosSociais}, API={stats.ApiStatus}, MQTT={stats.MqttStatus}");
                }
                return stats;
            }
            else
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                System.Diagnostics.Debug.WriteLine($"Erro ao buscar estatísticas: {response.StatusCode} - {errorContent}");
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Erro ao buscar estatísticas: {ex.Message}");
            System.Diagnostics.Debug.WriteLine($"Stack trace: {ex.StackTrace}");
        }
        return null;
    }

    public async Task<bool> ChangePasswordAsync(string currentPassword, string newPassword)
    {
        try
        {
            var request = new { CurrentPassword = currentPassword, NewPassword = newPassword };
            var json = JsonConvert.SerializeObject(request);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            var response = await _httpClient.PutAsync(BuildUrl("Users/change-password"), content);
            
            if (response.IsSuccessStatusCode)
            {
                return true;
            }
            else
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                System.Diagnostics.Debug.WriteLine($"Erro ao alterar senha: {response.StatusCode} - {errorContent}");
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Erro ao alterar senha: {ex.Message}");
        }
        return false;
    }

    public async Task<bool> RequestPasswordResetAsync(string email)
    {
        try
        {
            var request = new { Email = email };
            var json = JsonConvert.SerializeObject(request);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            var response = await _httpClient.PostAsync(BuildUrl("Users/request-password-reset"), content);
            
            if (response.IsSuccessStatusCode)
            {
                return true;
            }
            else
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                System.Diagnostics.Debug.WriteLine($"Erro ao solicitar reset de senha: {response.StatusCode} - {errorContent}");
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Erro ao solicitar reset de senha: {ex.Message}");
        }
        return false;
    }

    public async Task<bool> DeleteAccountAsync(string password)
    {
        try
        {
            var request = new { Password = password };
            var json = JsonConvert.SerializeObject(request);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            var url = BuildUrl("Users/me");
            var httpRequest = new HttpRequestMessage(HttpMethod.Delete, url)
            {
                Content = content
            };
            var response = await _httpClient.SendAsync(httpRequest);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Erro ao excluir conta: {ex.Message}");
        }
        return false;
    }

    // Commands - MQTT para dispositivos StarkSwitch
    public async Task<bool> PublishCommandAsync(Guid deviceId, string comando)
    {
        try
        {
            var request = new { deviceId, customCommand = comando };
            var json = JsonConvert.SerializeObject(request);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            var response = await _httpClient.PostAsync(BuildUrl("Commands/publish"), content);
            
            if (response.IsSuccessStatusCode)
            {
                return true;
            }
            else
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                System.Diagnostics.Debug.WriteLine($"Erro ao publicar comando: {response.StatusCode} - {errorContent}");
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Erro ao publicar comando MQTT: {ex.Message}");
        }
        return false;
    }

    // Super IA
    public async Task<SuperIaResponse?> CallSuperIaAsync(SuperIaRequest request)
    {
        try
        {
            var json = JsonConvert.SerializeObject(request);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            var response = await _httpClient.PostAsync(BuildUrl("Users/ia/super"), content);
            
            if (response.IsSuccessStatusCode)
            {
                var responseContent = await response.Content.ReadAsStringAsync();
                return JsonConvert.DeserializeObject<SuperIaResponse>(responseContent);
            }
            else
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                System.Diagnostics.Debug.WriteLine($"Erro ao chamar Super IA: {response.StatusCode} - {errorContent}");
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Erro ao chamar Super IA: {ex.Message}");
        }
        return null;
    }

    // Licenças
    public async Task<List<License>> GetLicensesAsync()
    {
        try
        {
            var response = await _httpClient.GetAsync(BuildUrl("licenses"));
            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                return JsonConvert.DeserializeObject<List<License>>(content) ?? new List<License>();
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Erro ao buscar licenças: {ex.Message}");
        }
        return new List<License>();
    }

    public async Task<LicenseActivation?> ActivateLicenseAsync(string licenseKey, string? machineName = null)
    {
        try
        {
            // Verificar se há token
            if (string.IsNullOrEmpty(_token))
            {
                System.Diagnostics.Debug.WriteLine("[ActivateLicenseAsync] ERRO: Token não está presente!");
                throw new UnauthorizedAccessException("Você precisa estar autenticado para ativar uma licença. Por favor, faça login novamente.");
            }

            System.Diagnostics.Debug.WriteLine($"[ActivateLicenseAsync] Token presente: {_token.Substring(0, Math.Min(20, _token.Length))}...");
            
            var request = new ActivateLicenseRequest { LicenseKey = licenseKey, MachineName = machineName };
            var json = JsonConvert.SerializeObject(request);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            
            // Adicionar header com MachineId
            var machineId = GetMachineId();
            var httpRequest = new HttpRequestMessage(HttpMethod.Post, BuildUrl("licenses/activate"))
            {
                Content = content
            };
            httpRequest.Headers.Add("X-Machine-Id", machineId);
            
            // Garantir que o token está no header
            if (!string.IsNullOrEmpty(_token))
            {
                httpRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _token);
            }
            
            System.Diagnostics.Debug.WriteLine($"[ActivateLicenseAsync] Enviando requisição para: {BuildUrl("licenses/activate")}");
            System.Diagnostics.Debug.WriteLine($"[ActivateLicenseAsync] Headers: Authorization={httpRequest.Headers.Authorization != null}, X-Machine-Id={machineId}");
            
            var response = await _httpClient.SendAsync(httpRequest);
            
            System.Diagnostics.Debug.WriteLine($"[ActivateLicenseAsync] Status Code: {response.StatusCode}");
            
            if (response.IsSuccessStatusCode)
            {
                var responseContent = await response.Content.ReadAsStringAsync();
                System.Diagnostics.Debug.WriteLine($"Licença ativada com sucesso: {responseContent}");
                return JsonConvert.DeserializeObject<LicenseActivation>(responseContent);
            }
            else if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                System.Diagnostics.Debug.WriteLine($"Erro 401 (Unauthorized) ao ativar licença: {errorContent}");
                throw new UnauthorizedAccessException("Sessão expirada. Por favor, faça login novamente.");
            }
            else
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                System.Diagnostics.Debug.WriteLine($"Erro ao ativar licença: {response.StatusCode} - {errorContent}");
                
                // Tentar extrair mensagem de erro do JSON
                try
                {
                    var errorObj = JsonConvert.DeserializeObject<JObject>(errorContent);
                    var errorMessage = errorObj?["message"]?.ToString() ?? "Erro desconhecido";
                    System.Diagnostics.Debug.WriteLine($"Mensagem de erro: {errorMessage}");
                    throw new Exception(errorMessage);
                }
                catch (Exception ex) when (!(ex is UnauthorizedAccessException))
                {
                    // Se não conseguir parsear, lançar erro genérico
                    throw new Exception($"Erro ao ativar licença: {response.StatusCode}");
                }
            }
        }
        catch (UnauthorizedAccessException)
        {
            throw; // Re-lançar exceções de autorização
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Erro ao ativar licença: {ex.Message}");
            throw;
        }
    }

    public async Task<bool> VerifyLicenseAsync(string licenseKey)
    {
        try
        {
            // Verificar se há token
            if (string.IsNullOrEmpty(_token))
            {
                System.Diagnostics.Debug.WriteLine("[VerifyLicenseAsync] ERRO: Token não está presente!");
                return false;
            }

            var request = new VerifyLicenseRequest { LicenseKey = licenseKey };
            var json = JsonConvert.SerializeObject(request);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            
            // Adicionar header com MachineId
            var machineId = GetMachineId();
            var httpRequest = new HttpRequestMessage(HttpMethod.Post, BuildUrl("licenses/verify"))
            {
                Content = content
            };
            httpRequest.Headers.Add("X-Machine-Id", machineId);
            
            // Garantir que o token está no header
            if (!string.IsNullOrEmpty(_token))
            {
                httpRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _token);
            }
            
            System.Diagnostics.Debug.WriteLine($"[VerifyLicenseAsync] Enviando requisição para: {BuildUrl("licenses/verify")}");
            System.Diagnostics.Debug.WriteLine($"[VerifyLicenseAsync] Headers: Authorization={httpRequest.Headers.Authorization != null}, X-Machine-Id={machineId}");
            
            var response = await _httpClient.SendAsync(httpRequest);
            
            System.Diagnostics.Debug.WriteLine($"[VerifyLicenseAsync] Status Code: {response.StatusCode}");
            
            if (response.IsSuccessStatusCode)
            {
                var responseContent = await response.Content.ReadAsStringAsync();
                System.Diagnostics.Debug.WriteLine($"[VerifyLicenseAsync] Resposta: {responseContent}");
                
                // Tentar deserializar como objeto dinâmico primeiro para ver o formato
                try
                {
                    var jsonObj = JsonConvert.DeserializeObject<dynamic>(responseContent);
                    if (jsonObj != null)
                    {
                        // A API retorna { isValid: true/false }
                        var isValid = jsonObj.isValid?.ToObject<bool>() ?? jsonObj.IsValid?.ToObject<bool>() ?? false;
                        System.Diagnostics.Debug.WriteLine($"[VerifyLicenseAsync] isValid extraído: {isValid}");
                        return isValid;
                    }
                }
                catch
                {
                    // Fallback para deserialização direta
                    var result = JsonConvert.DeserializeObject<VerifyLicenseResponse>(responseContent);
                    return result?.IsValid ?? false;
                }
            }
            else
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                System.Diagnostics.Debug.WriteLine($"[VerifyLicenseAsync] Erro na resposta: {response.StatusCode} - {errorContent}");
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Erro ao verificar licença: {ex.Message}");
            System.Diagnostics.Debug.WriteLine($"Stack trace: {ex.StackTrace}");
        }
        return false;
    }

    // Agendamentos
    public async Task<List<Agendamento>> GetAgendamentosAsync()
    {
        try
        {
            var response = await _httpClient.GetAsync(BuildUrl("Agendamentos"));
            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                return JsonConvert.DeserializeObject<List<Agendamento>>(content) ?? new List<Agendamento>();
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Erro ao buscar agendamentos: {ex.Message}");
        }
        return new List<Agendamento>();
    }

    public async Task<Agendamento?> CreateAgendamentoEspAsync(Guid dispositivoEspId, DateTime data, int hora, int minuto, string recorrencia)
    {
        try
        {
            var request = new
            {
                dispositivoEspId = dispositivoEspId,
                data = data,
                hora = hora,
                minuto = minuto,
                recorrencia = recorrencia
            };
            var json = JsonConvert.SerializeObject(request);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            var response = await _httpClient.PostAsync(BuildUrl("Agendamentos/esp"), content);
            
            if (response.IsSuccessStatusCode)
            {
                var responseContent = await response.Content.ReadAsStringAsync();
                return JsonConvert.DeserializeObject<Agendamento>(responseContent);
            }
            else
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                System.Diagnostics.Debug.WriteLine($"Erro ao criar agendamento ESP: {response.StatusCode} - {errorContent}");
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Erro ao criar agendamento ESP: {ex.Message}");
        }
        return null;
    }

    public async Task<Agendamento?> CreateAgendamentoStarkswitchAsync(Guid deviceId, string acao, DateTime data, int hora, int minuto, string recorrencia)
    {
        try
        {
            var request = new
            {
                deviceId = deviceId,
                acao = acao,
                data = data,
                hora = hora,
                minuto = minuto,
                recorrencia = recorrencia
            };
            var json = JsonConvert.SerializeObject(request);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            var response = await _httpClient.PostAsync(BuildUrl("Agendamentos/starkswitch"), content);
            
            if (response.IsSuccessStatusCode)
            {
                var responseContent = await response.Content.ReadAsStringAsync();
                return JsonConvert.DeserializeObject<Agendamento>(responseContent);
            }
            else
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                System.Diagnostics.Debug.WriteLine($"Erro ao criar agendamento Starkswitch: {response.StatusCode} - {errorContent}");
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Erro ao criar agendamento Starkswitch: {ex.Message}");
        }
        return null;
    }

    public async Task<Agendamento?> CreateAgendamentoEwelinkAsync(string ewelinkDeviceId, string acao, DateTime data, int hora, int minuto, string recorrencia)
    {
        try
        {
            var request = new
            {
                ewelinkDeviceId = ewelinkDeviceId,
                acao = acao,
                data = data,
                hora = hora,
                minuto = minuto,
                recorrencia = recorrencia
            };
            var json = JsonConvert.SerializeObject(request);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            var response = await _httpClient.PostAsync(BuildUrl("Agendamentos/ewelink"), content);
            
            if (response.IsSuccessStatusCode)
            {
                var responseContent = await response.Content.ReadAsStringAsync();
                return JsonConvert.DeserializeObject<Agendamento>(responseContent);
            }
            else
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                System.Diagnostics.Debug.WriteLine($"Erro ao criar agendamento Ewelink: {response.StatusCode} - {errorContent}");
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Erro ao criar agendamento Ewelink: {ex.Message}");
        }
        return null;
    }

    public async Task<bool> DeleteAgendamentoAsync(Guid id)
    {
        try
        {
            var response = await _httpClient.DeleteAsync(BuildUrl($"Agendamentos/{id}"));
            return response.IsSuccessStatusCode || response.StatusCode == System.Net.HttpStatusCode.NoContent;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Erro ao deletar agendamento: {ex.Message}");
        }
        return false;
    }

    // Assinaturas - Planos Ativos
    public async Task<List<PlanoAtivo>?> GetPlanosAtivosAsync()
    {
        try
        {
            var url = BuildUrl("Assinaturas/ativas");
            System.Diagnostics.Debug.WriteLine($"GetPlanosAtivosAsync - URL: {url}");
            
            var request = new HttpRequestMessage(HttpMethod.Get, url);
            var response = await _httpClient.SendAsync(request);
            
            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                System.Diagnostics.Debug.WriteLine($"Resposta GetPlanosAtivosAsync: {content}");
                var planos = JsonConvert.DeserializeObject<List<PlanoAtivo>>(content);
                return planos ?? new List<PlanoAtivo>();
            }
            else
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                System.Diagnostics.Debug.WriteLine($"Erro ao buscar planos ativos: {response.StatusCode} - {errorContent}");
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Erro ao buscar planos ativos: {ex.Message}");
        }
        return null;
    }

    public async Task<bool> CancelarPlanoAsync(Guid assinaturaId)
    {
        try
        {
            var url = BuildUrl($"Assinaturas/cancelar/{assinaturaId}");
            System.Diagnostics.Debug.WriteLine($"CancelarPlanoAsync - URL: {url}");
            
            var request = new HttpRequestMessage(HttpMethod.Post, url);
            var response = await _httpClient.SendAsync(request);
            
            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                System.Diagnostics.Debug.WriteLine($"Resposta CancelarPlanoAsync: {content}");
                return true;
            }
            else
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                System.Diagnostics.Debug.WriteLine($"Erro ao cancelar plano: {response.StatusCode} - {errorContent}");
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Erro ao cancelar plano: {ex.Message}");
        }
        return false;
    }

    // Pagamentos - Adicionar Fundos
    public async Task<string?> CreateAddFundsCheckoutAsync(decimal amount)
    {
        try
        {
            var request = new { Amount = amount };
            var json = JsonConvert.SerializeObject(request);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            var httpRequest = new HttpRequestMessage(HttpMethod.Post, BuildUrl("Users/add-funds"))
            {
                Content = content
            };
            // Indicar que a requisição vem do software Windows Forms
            httpRequest.Headers.Add("X-From-Software", "true");
            var response = await _httpClient.SendAsync(httpRequest);
            
            if (response.IsSuccessStatusCode)
            {
                var responseContent = await response.Content.ReadAsStringAsync();
                var result = JsonConvert.DeserializeObject<JObject>(responseContent);
                return result?["checkoutUrl"]?.ToString();
            }
            else
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                System.Diagnostics.Debug.WriteLine($"Erro ao criar checkout de fundos: {response.StatusCode} - {errorContent}");
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Erro ao criar checkout de fundos: {ex.Message}");
        }
        return null;
    }

    // Pagamentos - Contratar Plano
    public async Task<string?> CreatePlanoCheckoutAsync(int nivel)
    {
        try
        {
            var request = new { Nivel = nivel };
            var json = JsonConvert.SerializeObject(request);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            var httpRequest = new HttpRequestMessage(HttpMethod.Post, BuildUrl("assinaturas/checkout"))
            {
                Content = content
            };
            // Indicar que a requisição vem do software Windows Forms
            httpRequest.Headers.Add("X-From-Software", "true");
            var response = await _httpClient.SendAsync(httpRequest);
            
            if (response.IsSuccessStatusCode)
            {
                var responseContent = await response.Content.ReadAsStringAsync();
                var result = JsonConvert.DeserializeObject<JObject>(responseContent);
                return result?["checkoutUrl"]?.ToString();
            }
            else
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                System.Diagnostics.Debug.WriteLine($"Erro ao criar checkout de plano: {response.StatusCode} - {errorContent}");
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Erro ao criar checkout de plano: {ex.Message}");
        }
        return null;
    }

    // Ewelink
    public async Task<EwelinkStatusResponse?> GetEwelinkStatusAsync()
    {
        try
        {
            var response = await _httpClient.GetAsync(BuildUrl("Ewelink/status"));
            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                var result = JsonConvert.DeserializeObject<JObject>(content);
                if (result != null)
                {
                    return new Models.EwelinkStatusResponse
                    {
                        IsLoggedIn = result["isLoggedIn"]?.ToObject<bool>() ?? false
                    };
                }
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Erro ao verificar status Ewelink: {ex.Message}");
        }
        return new Models.EwelinkStatusResponse { IsLoggedIn = false };
    }

    public async Task<List<EwelinkDevice>> GetEwelinkDevicesAsync()
    {
        try
        {
            var response = await _httpClient.GetAsync(BuildUrl("Ewelink/dispositivos"));
            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                return JsonConvert.DeserializeObject<List<EwelinkDevice>>(content) ?? new List<EwelinkDevice>();
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Erro ao buscar dispositivos Ewelink: {ex.Message}");
        }
        return new List<EwelinkDevice>();
    }

    public async Task<EwelinkDevice?> GetEwelinkDeviceStatusAsync(string deviceId)
    {
        try
        {
            var response = await _httpClient.GetAsync(BuildUrl($"Ewelink/dispositivos/{deviceId}/status"));
            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                return JsonConvert.DeserializeObject<EwelinkDevice>(content);
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Erro ao buscar status do dispositivo Ewelink: {ex.Message}");
        }
        return null;
    }

    public async Task<bool> ControlEwelinkDeviceAsync(string deviceId, bool switchOn)
    {
        try
        {
            var request = new { Switch = switchOn };
            var json = JsonConvert.SerializeObject(request);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            var response = await _httpClient.PostAsync(BuildUrl($"Ewelink/dispositivos/{deviceId}/controlar"), content);
            
            if (response.IsSuccessStatusCode)
            {
                return true;
            }
            else
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                System.Diagnostics.Debug.WriteLine($"Erro ao controlar dispositivo Ewelink: {response.StatusCode} - {errorContent}");
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Erro ao controlar dispositivo Ewelink: {ex.Message}");
        }
        return false;
    }

    private string GetMachineId()
    {
        // Gerar um identificador único da máquina baseado em hardware
        try
        {
            var machineName = Environment.MachineName;
            var userName = Environment.UserName;
            var processorId = Environment.ProcessorCount.ToString();
            
            // Combinar informações para criar um ID único
            var combined = $"{machineName}-{userName}-{processorId}";
            
            // Gerar hash MD5
            using var md5 = System.Security.Cryptography.MD5.Create();
            var hash = md5.ComputeHash(Encoding.UTF8.GetBytes(combined));
            return Convert.ToBase64String(hash);
        }
        catch
        {
            // Fallback: usar GUID
            return Guid.NewGuid().ToString();
        }
    }

    // Weather
    public async Task<WeatherForecastDto?> GetWeatherForecastAsync()
    {
        try
        {
            var url = BuildUrl("weather/forecast");
            var response = await _httpClient.GetAsync(url);
            
            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync();
                return JsonConvert.DeserializeObject<WeatherForecastDto>(json);
            }
            
            return null;
        }
        catch
        {
            return null;
        }
    }

    // User Online/Offline
    public async Task<bool> SetUserOnlineAsync()
    {
        try
        {
            // Verificar se há token
            if (string.IsNullOrEmpty(_token))
            {
                System.Diagnostics.Debug.WriteLine("[SetUserOnlineAsync] ERRO: Token não está presente!");
                return false;
            }

            System.Diagnostics.Debug.WriteLine($"[SetUserOnlineAsync] Token presente: {_token.Substring(0, Math.Min(20, _token.Length))}...");
            
            var request = new { Origem = "soft" };
            var json = JsonConvert.SerializeObject(request);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            
            var url = BuildUrl("Users/online");
            System.Diagnostics.Debug.WriteLine($"[SetUserOnlineAsync] Enviando requisição para: {url}");
            System.Diagnostics.Debug.WriteLine($"[SetUserOnlineAsync] Request body: {json}");
            
            var response = await _httpClient.PostAsync(url, content);
            
            System.Diagnostics.Debug.WriteLine($"[SetUserOnlineAsync] Status Code: {response.StatusCode}");
            
            if (response.IsSuccessStatusCode)
            {
                var responseContent = await response.Content.ReadAsStringAsync();
                System.Diagnostics.Debug.WriteLine($"[SetUserOnlineAsync] Resposta: {responseContent}");
                return true;
            }
            else
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                System.Diagnostics.Debug.WriteLine($"[SetUserOnlineAsync] Erro na resposta: {response.StatusCode} - {errorContent}");
                return false;
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[SetUserOnlineAsync] Exceção: {ex.Message}");
            System.Diagnostics.Debug.WriteLine($"[SetUserOnlineAsync] Stack trace: {ex.StackTrace}");
            return false;
        }
    }

    public async Task<bool> SetUserOfflineAsync()
    {
        try
        {
            // Verificar se há token
            if (string.IsNullOrEmpty(_token))
            {
                System.Diagnostics.Debug.WriteLine("[SetUserOfflineAsync] ERRO: Token não está presente!");
                return false;
            }

            System.Diagnostics.Debug.WriteLine($"[SetUserOfflineAsync] Token presente: {_token.Substring(0, Math.Min(20, _token.Length))}...");
            
            var request = new { Origem = "soft" };
            var json = JsonConvert.SerializeObject(request);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            
            var url = BuildUrl("Users/offline");
            System.Diagnostics.Debug.WriteLine($"[SetUserOfflineAsync] Enviando requisição para: {url}");
            
            var response = await _httpClient.PostAsync(url, content);
            
            System.Diagnostics.Debug.WriteLine($"[SetUserOfflineAsync] Status Code: {response.StatusCode}");
            
            if (response.IsSuccessStatusCode)
            {
                var responseContent = await response.Content.ReadAsStringAsync();
                System.Diagnostics.Debug.WriteLine($"[SetUserOfflineAsync] Resposta: {responseContent}");
                return true;
            }
            else
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                System.Diagnostics.Debug.WriteLine($"[SetUserOfflineAsync] Erro na resposta: {response.StatusCode} - {errorContent}");
                return false;
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[SetUserOfflineAsync] Exceção: {ex.Message}");
            System.Diagnostics.Debug.WriteLine($"[SetUserOfflineAsync] Stack trace: {ex.StackTrace}");
            return false;
        }
    }

    public async Task<bool> SyncErrorLogsSoftAsync(Guid userId, List<LogToSuporte> logs)
    {
        try
        {
            if (string.IsNullOrEmpty(_token))
            {
                System.Diagnostics.Debug.WriteLine("[SyncErrorLogsSoftAsync] ERRO: Token não está presente!");
                return false;
            }

            var request = new
            {
                UserId = userId,
                Logs = logs.Select(log => new
                {
                    log.UltimoComando,
                    log.UltimaResposta,
                    log.UltimoDispositivoAcionado,
                    log.ErroCompleto,
                    log.CodigoDeErro,
                    log.DataErro,
                    log.HoraErro,
                    log.AcaoErro
                }).ToList()
            };

            var json = JsonConvert.SerializeObject(request);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            var response = await _httpClient.PostAsync(BuildUrl("Users/error-logs/soft/sync"), content);

            if (response.IsSuccessStatusCode)
            {
                var responseContent = await response.Content.ReadAsStringAsync();
                System.Diagnostics.Debug.WriteLine($"[SyncErrorLogsSoftAsync] Logs sincronizados com sucesso: {responseContent}");
                return true;
            }
            else
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                System.Diagnostics.Debug.WriteLine($"[SyncErrorLogsSoftAsync] Erro ao sincronizar logs: {response.StatusCode} - {errorContent}");
                return false;
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[SyncErrorLogsSoftAsync] Exceção: {ex.Message}");
            System.Diagnostics.Debug.WriteLine($"[SyncErrorLogsSoftAsync] Stack trace: {ex.StackTrace}");
            return false;
        }
    }

    // Health Check - Verificar status da API
    public async Task<bool> CheckApiStatusAsync()
    {
        try
        {
            var url = BuildUrl("HealthCheck/api");
            var response = await _httpClient.GetAsync(url);
            
            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                var result = JsonConvert.DeserializeObject<JObject>(content);
                var status = result?["status"]?.ToString();
                return status == "OK";
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Erro ao verificar status da API: {ex.Message}");
        }
        return false;
    }
}


