using StarkAid.Web.Dtos;
using StarkAid.Web.DTOs;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace StarkAid.Web.Services;

public class ApiService
{
    private readonly HttpClient _http;
    public ApiService(HttpClient http) => _http = http;

    public async Task<AuthResponseDto?> LoginAsync(LoginDto dto)
    {
        var json = JsonSerializer.Serialize(dto);
        Console.WriteLine($"[ApiService] Login request: {json}");
        
        var response = await _http.PostAsJsonAsync("api/v1/Auth/login", dto);
        Console.WriteLine($"[ApiService] Login response status: {response.StatusCode}");
        
        if (!response.IsSuccessStatusCode) 
        {
            var errorBody = await response.Content.ReadAsStringAsync();
            Console.WriteLine($"[ApiService] Login error body: {errorBody}");
            return null;
        }
        return await response.Content.ReadFromJsonAsync<AuthResponseDto>(new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
    }

    public async Task<AuthResponseDto?> RegisterAsync(UserRegisterDto register)
    {
        var json = JsonSerializer.Serialize(register);
        Console.WriteLine($"[ApiService] Register request: {json}");

        var response = await _http.PostAsJsonAsync("api/v1/Auth/register", register);
        Console.WriteLine($"[ApiService] Register response status: {response.StatusCode}");

        if (!response.IsSuccessStatusCode)
        {
            var errorBody = await response.Content.ReadAsStringAsync();
            Console.WriteLine($"[ApiService] Register error body: {errorBody}");
            return null;
        }
        return await response.Content.ReadFromJsonAsync<AuthResponseDto>(new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
    }

    public async Task<UserMeDto?> GetMeAsync(string token, string apiKey)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, "api/v1/Users/me");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        request.Headers.Add("Api-Key", apiKey);
        var response = await _http.SendAsync(request);
        if (!response.IsSuccessStatusCode) return null;
        return await response.Content.ReadFromJsonAsync<UserMeDto>(new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
    }

    public async Task<UserStatsDto?> GetStatsAsync(string token, string apiKey)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, "api/v1/Users/stats");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        request.Headers.Add("Api-Key", apiKey);
        var response = await _http.SendAsync(request);
        if (!response.IsSuccessStatusCode) return null;
        return await response.Content.ReadFromJsonAsync<UserStatsDto>(new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
    }

    // Nova: Atualizar perfil
    public async Task<string> UpdateProfileAsync(UserMeDto dto, string token, string apiKey)
    {
        var request = new HttpRequestMessage(HttpMethod.Put, "api/v1/Users/me");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        request.Headers.Add("Api-Key", apiKey);
        request.Content = JsonContent.Create(dto);
        var response = await _http.SendAsync(request);
        return await response.Content.ReadAsStringAsync();
    }

    // Nova: Status Assinatura (Agora buscando de /ativas que retorna a lista de assinaturas do usuário)
    public async Task<AssinaturaStatusDto?> GetAssinaturaStatusAsync(string token, string apiKey)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, "api/v1/Assinaturas/ativas");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        request.Headers.Add("Api-Key", apiKey);
        var response = await _http.SendAsync(request);
        if (!response.IsSuccessStatusCode) return null;
        
        var list = await response.Content.ReadFromJsonAsync<List<AssinaturaStatusDto>>(new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        return list?.FirstOrDefault();
    }

    // Nova: Checkout Assinatura
    public async Task<CheckoutDto?> CheckoutAssinaturaAsync(int nivel, string token, string apiKey)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, "api/v1/Assinaturas/checkout");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        request.Headers.Add("Api-Key", apiKey);
        request.Content = JsonContent.Create(new { Nivel = nivel });
        var response = await _http.SendAsync(request);
        if (!response.IsSuccessStatusCode) return null;
        return await response.Content.ReadFromJsonAsync<CheckoutDto>(new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
    }

    // Nova: Add Funds (Starkcoins)
    public async Task<CheckoutDto?> AddFundsAsync(int coins, string token, string apiKey)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, "api/v1/Users/add-funds");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        request.Headers.Add("Api-Key", apiKey);
        request.Content = JsonContent.Create(new { Coins = coins });
        var response = await _http.SendAsync(request);
        if (!response.IsSuccessStatusCode) return null;
        return await response.Content.ReadFromJsonAsync<CheckoutDto>(new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
    }

    // Nova: Licenças
    public async Task<List<LicenseDto>> GetLicencasAsync(string token, string apiKey)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, "api/v1/licenses");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        request.Headers.Add("Api-Key", apiKey);
        var response = await _http.SendAsync(request);
        if (!response.IsSuccessStatusCode) return new List<LicenseDto>();
        return await response.Content.ReadFromJsonAsync<List<LicenseDto>>(new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? new List<LicenseDto>();
    }

    // Nova: Checkout Licença
    public async Task<CheckoutDto?> CheckoutLicencaAsync(int maxMachines, string token, string apiKey)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, "api/v1/licenses/checkout");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        request.Headers.Add("Api-Key", apiKey);
        request.Content = JsonContent.Create(new { MaxMachines = maxMachines });
        var response = await _http.SendAsync(request);
        if (!response.IsSuccessStatusCode) return null;
        return await response.Content.ReadFromJsonAsync<CheckoutDto>(new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
    }

    // Nova: eWeLink Status
    public async Task<EwelinkStatusResponse?> GetEwelinkStatusAsync(string token, string apiKey)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, "api/v1/Ewelink/status");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        request.Headers.Add("Api-Key", apiKey);
        var response = await _http.SendAsync(request);
        if (!response.IsSuccessStatusCode) return null;
        return await response.Content.ReadFromJsonAsync<EwelinkStatusResponse>(new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
    }

    // Nova: eWeLink Dispositivos
    public async Task<List<DeviceDto>> GetEwelinkDispositivosAsync(string token, string apiKey)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, "api/v1/Ewelink/dispositivos");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        request.Headers.Add("Api-Key", apiKey);
        var response = await _http.SendAsync(request);
        if (!response.IsSuccessStatusCode) return new List<DeviceDto>();
        return await response.Content.ReadFromJsonAsync<List<DeviceDto>>(new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? new List<DeviceDto>();
    }

    // Nova: Toggle eWeLink Device
    public async Task<bool> ToggleEwelinkDeviceAsync(string deviceId, bool isOn, string token, string apiKey)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, $"api/v1/Ewelink/dispositivos/{deviceId}/controlar");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        request.Headers.Add("Api-Key", apiKey);
        request.Content = JsonContent.Create(new { Switch = isOn });
        var response = await _http.SendAsync(request);
        return response.IsSuccessStatusCode;
    }

    // Nova: Listar StarkSwitch (Devices)
    public async Task<List<DeviceDto>> GetStarkSwitchAsync(string token, string apiKey)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, "api/v1/Devices");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        request.Headers.Add("Api-Key", apiKey);
        var response = await _http.SendAsync(request);
        if (!response.IsSuccessStatusCode) return new List<DeviceDto>();
        return await response.Content.ReadFromJsonAsync<List<DeviceDto>>(new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? new List<DeviceDto>();
    }

    // Nova: Criar StarkSwitch
    public async Task<DeviceDto?> CreateStarkSwitchAsync(string name, string? comando, string token, string apiKey)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, "api/v1/Devices");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        request.Headers.Add("Api-Key", apiKey);
        request.Content = JsonContent.Create(new { Name = name, Comando = comando });
        var response = await _http.SendAsync(request);
        if (!response.IsSuccessStatusCode) return null;
        return await response.Content.ReadFromJsonAsync<DeviceDto>(new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
    }

    // Nova: Editar StarkSwitch
    public async Task<bool> EditStarkSwitchAsync(string id, string newName, string? newComando, string token, string apiKey)
    {
        var request = new HttpRequestMessage(HttpMethod.Put, $"api/v1/Devices/{id}");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        request.Headers.Add("Api-Key", apiKey);
        request.Content = JsonContent.Create(new { NewName = newName, NewComando = newComando });
        var response = await _http.SendAsync(request);
        return response.IsSuccessStatusCode;
    }

    // Nova: Excluir StarkSwitch
    public async Task<bool> DeleteStarkSwitchAsync(string id, string token, string apiKey)
    {
        var request = new HttpRequestMessage(HttpMethod.Delete, $"api/v1/Devices/{id}");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        request.Headers.Add("Api-Key", apiKey);
        var response = await _http.SendAsync(request);
        return response.IsSuccessStatusCode;
    }

    // Nova: Listar UDP (DispositivosEsp)
    public async Task<List<DispositivoEspDto>> GetUdpDispositivosAsync(string token, string apiKey)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, "api/v1/DispositivosEsp");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        request.Headers.Add("Api-Key", apiKey);
        var response = await _http.SendAsync(request);
        if (!response.IsSuccessStatusCode) return new List<DispositivoEspDto>();
        return await response.Content.ReadFromJsonAsync<List<DispositivoEspDto>>(new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? new List<DispositivoEspDto>();
    }

    // Nova: Criar UDP
    public async Task<DispositivoEspDto?> CreateUdpAsync(DispositivoEspCreateDto dto, string token, string apiKey)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, "api/v1/DispositivosEsp");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        request.Headers.Add("Api-Key", apiKey);
        request.Content = JsonContent.Create(dto);
        var response = await _http.SendAsync(request);
        if (!response.IsSuccessStatusCode) return null;
        return await response.Content.ReadFromJsonAsync<DispositivoEspDto>(new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
    }

    // Nova: Editar UDP
    public async Task<bool> EditUdpAsync(string id, DispositivoEspCreateDto dto, string token, string apiKey)
    {
        var request = new HttpRequestMessage(HttpMethod.Put, $"api/v1/DispositivosEsp/{id}");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        request.Headers.Add("Api-Key", apiKey);
        request.Content = JsonContent.Create(dto);
        var response = await _http.SendAsync(request);
        return response.IsSuccessStatusCode;
    }

    // Nova: Excluir UDP
    public async Task<bool> DeleteUdpAsync(string id, string token, string apiKey)
    {
        var request = new HttpRequestMessage(HttpMethod.Delete, $"api/v1/DispositivosEsp/{id}");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        request.Headers.Add("Api-Key", apiKey);
        var response = await _http.SendAsync(request);
        return response.IsSuccessStatusCode;
    }

    // Nova: Acionar UDP
    public async Task<bool> AcionarUdpAsync(string comando, string token, string apiKey)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, "api/v1/DispositivosEsp/enviar-comando");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        request.Headers.Add("Api-Key", apiKey);
        request.Content = JsonContent.Create(new { Comando = comando });
        var response = await _http.SendAsync(request);
        return response.IsSuccessStatusCode;
    }

    // Nova: Listar Agendamentos (geral, para separar por tipo nas páginas)
    public async Task<List<AgendamentoDto>> GetAgendamentosAsync(string token, string apiKey)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, "api/v1/Agendamentos");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        request.Headers.Add("Api-Key", apiKey);
        var response = await _http.SendAsync(request);
        if (!response.IsSuccessStatusCode) return new List<AgendamentoDto>();
        return await response.Content.ReadFromJsonAsync<List<AgendamentoDto>>(new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? new List<AgendamentoDto>();
    }

    // Nova: Criar Agendamento eWeLink
    public async Task<AgendamentoDto?> CreateAgendamentoEwelinkAsync(AgendamentoEwelinkCreateDto dto, string token, string apiKey)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, "api/v1/Agendamentos/ewelink");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        request.Headers.Add("Api-Key", apiKey);
        request.Content = JsonContent.Create(dto);
        var response = await _http.SendAsync(request);
        if (!response.IsSuccessStatusCode) return null;
        return await response.Content.ReadFromJsonAsync<AgendamentoDto>(new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
    }

    // Nova: Criar Agendamento StarkSwitch
    public async Task<AgendamentoDto?> CreateAgendamentoStarkSwitchAsync(AgendamentoStarkSwitchCreateDto dto, string token, string apiKey)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, "api/v1/Agendamentos/starkswitch");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        request.Headers.Add("Api-Key", apiKey);
        // Mapeando Comando (frontend) para Acao (backend)
        request.Content = JsonContent.Create(new { 
            DeviceId = dto.DeviceId, 
            Acao = dto.Comando, 
            Data = dto.Data, 
            Hora = dto.Hora, 
            Minuto = dto.Minuto, 
            Recorrencia = dto.Recorrencia 
        });
        var response = await _http.SendAsync(request);
        if (!response.IsSuccessStatusCode) return null;
        return await response.Content.ReadFromJsonAsync<AgendamentoDto>(new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
    }

    // Nova: Criar Agendamento UDP (ESP)
    public async Task<AgendamentoDto?> CreateAgendamentoUdpAsync(AgendamentoUdpCreateDto dto, string token, string apiKey)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, "api/v1/Agendamentos/esp");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        request.Headers.Add("Api-Key", apiKey);
        request.Content = JsonContent.Create(dto);
        var response = await _http.SendAsync(request);
        if (!response.IsSuccessStatusCode) return null;
        return await response.Content.ReadFromJsonAsync<AgendamentoDto>(new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
    }

    // Nova: Editar Agendamento
    public async Task<bool> EditAgendamentoAsync(string id, AgendamentoEditDto dto, string token, string apiKey)
    {
        var request = new HttpRequestMessage(HttpMethod.Put, $"api/v1/Agendamentos/{id}");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        request.Headers.Add("Api-Key", apiKey);
        request.Content = JsonContent.Create(dto);
        var response = await _http.SendAsync(request);
        return response.IsSuccessStatusCode;
    }

    // Nova: Excluir Agendamento
    public async Task<bool> DeleteAgendamentoAsync(string id, string token, string apiKey)
    {
        var request = new HttpRequestMessage(HttpMethod.Delete, $"api/v1/Agendamentos/{id}");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        request.Headers.Add("Api-Key", apiKey);
        var response = await _http.SendAsync(request);
        return response.IsSuccessStatusCode;
    }

    // Nova: Listar Comandos Sociais
    public async Task<List<ComandoSocialDto>> GetComandosSociaisAsync(string token, string apiKey)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, "api/v1/ComandosSociais");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        request.Headers.Add("Api-Key", apiKey);
        var response = await _http.SendAsync(request);
        if (!response.IsSuccessStatusCode) return new List<ComandoSocialDto>();
        var result = await response.Content.ReadFromJsonAsync<ComandosSociaisResponseDto>(new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        return result?.Comandos ?? new List<ComandoSocialDto>();
    }

    // Nova: Criar Comando Social
    public async Task<ComandoSocialDto?> CreateComandoSocialAsync(ComandoSocialCreateDto dto, string token, string apiKey)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, "api/v1/ComandosSociais");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        request.Headers.Add("Api-Key", apiKey);
        request.Content = JsonContent.Create(dto);
        var response = await _http.SendAsync(request);
        if (!response.IsSuccessStatusCode) return null;
        var result = await response.Content.ReadFromJsonAsync<ComandoSocialResponseDto>(new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        return result?.Comando;
    }

    // Para Aprendizado IA: Assumindo API /api/v1/AprendizadoIa, mas não fornecida, então simular com placeholder.
    // Se a API for diferente, ajustar.
    public async Task<List<AprendizadoIaDto>> GetAprendizadoIaAsync(string token, string apiKey)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, "api/v1/Admin/aprendizados");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        request.Headers.Add("Api-Key", apiKey);
        var response = await _http.SendAsync(request);
        if (!response.IsSuccessStatusCode) return new List<AprendizadoIaDto>();
        return await response.Content.ReadFromJsonAsync<List<AprendizadoIaDto>>(new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? new List<AprendizadoIaDto>();
    }

    public async Task<bool> CreateAprendizadoIaAsync(string texto, string resposta, string tipo, string? contexto, string token, string apiKey)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, "api/v1/Admin/aprendizados");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        request.Headers.Add("Api-Key", apiKey);
        request.Content = JsonContent.Create(new { Texto = texto, Resposta = resposta, Tipo = tipo, Contexto = contexto });
        var response = await _http.SendAsync(request);
        return response.IsSuccessStatusCode;
    }

    public async Task<bool> EditAprendizadoIaAsync(Guid id, string texto, string resposta, string tipo, string? contexto, bool ativo, bool emQuarentena, string token, string apiKey)
    {
        var request = new HttpRequestMessage(HttpMethod.Put, $"api/v1/Admin/aprendizados/{id}");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        request.Headers.Add("Api-Key", apiKey);
        request.Content = JsonContent.Create(new { Texto = texto, Resposta = resposta, Tipo = tipo, Contexto = contexto, Ativo = ativo, EmQuarentena = emQuarentena });
        var response = await _http.SendAsync(request);
        return response.IsSuccessStatusCode;
    }

    public async Task<bool> DeleteAprendizadoIaAsync(Guid id, string token, string apiKey)
    {
        var request = new HttpRequestMessage(HttpMethod.Delete, $"api/v1/Admin/aprendizados/{id}");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        request.Headers.Add("Api-Key", apiKey);
        var response = await _http.SendAsync(request);
        return response.IsSuccessStatusCode;
    }

    public async Task<bool> PromoverAprendizadoAsync(Guid id, string token, string apiKey)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, $"api/v1/Admin/aprendizados/{id}/promover");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        request.Headers.Add("Api-Key", apiKey);
        var response = await _http.SendAsync(request);
        return response.IsSuccessStatusCode;
    }

    public async Task<bool> RebaixarAprendizadoAsync(Guid id, string token, string apiKey)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, $"api/v1/Admin/aprendizados/{id}/rebaixar");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        request.Headers.Add("Api-Key", apiKey);
        var response = await _http.SendAsync(request);
        return response.IsSuccessStatusCode;
    }

    public async Task<bool> ToggleQuarentenaAsync(Guid id, string token, string apiKey)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, $"api/v1/Admin/aprendizados/{id}/quarentena");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        request.Headers.Add("Api-Key", apiKey);
        var response = await _http.SendAsync(request);
        return response.IsSuccessStatusCode;
    }

    public async Task<bool> AddAprendizadoRespostaAsync(Guid aprendizadoId, string texto, string token, string apiKey)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, $"api/v1/Admin/aprendizados/{aprendizadoId}/respostas");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        request.Headers.Add("Api-Key", apiKey);
        request.Content = JsonContent.Create(new { Texto = texto });
        var response = await _http.SendAsync(request);
        return response.IsSuccessStatusCode;
    }

    public async Task<bool> UpdateAprendizadoRespostaAsync(Guid respostaId, string texto, string token, string apiKey)
    {
        var request = new HttpRequestMessage(HttpMethod.Put, $"api/v1/Admin/aprendizados/respostas/{respostaId}");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        request.Headers.Add("Api-Key", apiKey);
        request.Content = JsonContent.Create(new { Texto = texto });
        var response = await _http.SendAsync(request);
        return response.IsSuccessStatusCode;
    }

    public async Task<bool> DeleteAprendizadoRespostaAsync(Guid respostaId, string token, string apiKey)
    {
        var request = new HttpRequestMessage(HttpMethod.Delete, $"api/v1/Admin/aprendizados/respostas/{respostaId}");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        request.Headers.Add("Api-Key", apiKey);
        var response = await _http.SendAsync(request);
        return response.IsSuccessStatusCode;
    }

    public async Task<object?> GetAprendizadoStatsAsync(string token, string apiKey)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, "api/v1/Admin/aprendizados/stats");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        request.Headers.Add("Api-Key", apiKey);
        var response = await _http.SendAsync(request);
        if (!response.IsSuccessStatusCode) return null;
        return await response.Content.ReadFromJsonAsync<object>(new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
    }

    public async Task<object?> GetTelemetryOverviewAsync(string token, string apiKey)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, "api/v1/Admin/ia/telemetry/overview");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        request.Headers.Add("Api-Key", apiKey);
        var response = await _http.SendAsync(request);
        if (!response.IsSuccessStatusCode) return null;
        return await response.Content.ReadFromJsonAsync<object>();
    }

    public async Task<object?> GetTelemetryQualityAsync(string token, string apiKey)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, "api/v1/Admin/ia/telemetry/quality");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        request.Headers.Add("Api-Key", apiKey);
        var response = await _http.SendAsync(request);
        if (!response.IsSuccessStatusCode) return null;
        return await response.Content.ReadFromJsonAsync<object>();
    }

    public async Task<object?> GetTopMissesAsync(string token, string apiKey)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, "api/v1/Admin/ia/telemetry/top-misses");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        request.Headers.Add("Api-Key", apiKey);
        var response = await _http.SendAsync(request);
        if (!response.IsSuccessStatusCode) return null;
        return await response.Content.ReadFromJsonAsync<object>();
    }

    public async Task<object?> GetRoiHistoryAsync(string token, string apiKey)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, "api/v1/Admin/ia/telemetry/roi-history");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        request.Headers.Add("Api-Key", apiKey);
        var response = await _http.SendAsync(request);
        if (!response.IsSuccessStatusCode) return null;
        return await response.Content.ReadFromJsonAsync<object>();
    }

    public async Task<object?> GetFuzzyAnalyticsAsync(string token, string apiKey)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, "api/v1/Admin/ia/telemetry/fuzzy-analytics");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        request.Headers.Add("Api-Key", apiKey);
        var response = await _http.SendAsync(request);
        if (!response.IsSuccessStatusCode) return null;
        return await response.Content.ReadFromJsonAsync<object>();
    }

    public async Task<bool> SincronizarEwelinkAsync(string token, string apiKey)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, "api/v1/ewelink/sincronizar");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        request.Headers.Add("Api-Key", apiKey);
        var response = await _http.SendAsync(request);
        return response.IsSuccessStatusCode;
    }

    // Nova: Editar Comando Social
    public async Task<bool> EditComandoSocialAsync(string id, ComandoSocialCreateDto dto, string token, string apiKey)
    {
        var request = new HttpRequestMessage(HttpMethod.Put, $"api/v1/ComandosSociais/{id}");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        request.Headers.Add("Api-Key", apiKey);
        request.Content = JsonContent.Create(dto);
        var response = await _http.SendAsync(request);
        return response.IsSuccessStatusCode;
    }

    // Nova: Excluir Comando Social
    public async Task<bool> DeleteComandoSocialAsync(string id, string token, string apiKey)
    {
        var request = new HttpRequestMessage(HttpMethod.Delete, $"api/v1/ComandosSociais/{id}");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        request.Headers.Add("Api-Key", apiKey);
        var response = await _http.SendAsync(request);
        return response.IsSuccessStatusCode;
    }

    // Nova: Publicar Comando Genérico (StarkSwitch)
    public async Task<bool> PublishGenericCommandAsync(string deviceId, string? command, string? customCommand, string token, string apiKey)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, "api/v1/Commands/publish");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        request.Headers.Add("Api-Key", apiKey);
        request.Content = JsonContent.Create(new { DeviceId = deviceId, Command = command, CustomCommand = customCommand });
        var response = await _http.SendAsync(request);
        return response.IsSuccessStatusCode;
    }

    public async Task<WeatherForecastDto?> GetWeatherForecastAsync(string token, string apiKey)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, "api/v1/Weather/forecast");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        request.Headers.Add("Api-Key", apiKey);
        var response = await _http.SendAsync(request);
        if (!response.IsSuccessStatusCode) return null;
        return await response.Content.ReadFromJsonAsync<WeatherForecastDto>(new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
    }
    public async Task<IaResponse?> ChamarSuperIAAsync(IaRequest dto, string token, string apiKey)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, "api/v1/Users/ia/super");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        request.Headers.Add("Api-Key", apiKey);
        request.Content = JsonContent.Create(dto);
        var response = await _http.SendAsync(request);
        if (!response.IsSuccessStatusCode) return null;
        return await response.Content.ReadFromJsonAsync<IaResponse>(new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
    }
}

public class IaRequest
{
    [System.Text.Json.Serialization.JsonPropertyName("texto")]
    public string Pergunta { get; set; } = string.Empty;

    [System.Text.Json.Serialization.JsonPropertyName("estilo")]
    public string Personalidade { get; set; } = string.Empty;

    [System.Text.Json.Serialization.JsonPropertyName("contextoUser")]
    public string UltimoContextoUser { get; set; } = string.Empty;

    [System.Text.Json.Serialization.JsonPropertyName("contextoIA")]
    public string UltimoContextoIA { get; set; } = string.Empty;

    [System.Text.Json.Serialization.JsonPropertyName("useStarkCoins")]
    public bool UsarStarkCoins { get; set; }
}

public class IaResponse
{
    public IaResultado? Resultado { get; set; }
    public string? PlanType { get; set; }
    public int? StarkCoinBalance { get; set; }
    public int? TokensConsumidosSemana { get; set; }
    public int? TokensSemanaMax { get; set; }
    public int? TokensRestantes { get; set; }
    public bool? AdsEnabled { get; set; }
    public int? AgendamentosMax { get; set; }
    public int? AgendamentosRestantes { get; set; }
    public int? Rate { get; set; }
    public int? NovoSaldo { get; set; }
    public EconomicPayload? Economy { get; set; }
}

public class IaResultado
{
    public string Texto { get; set; } = string.Empty;
    public string? HitResult { get; set; }
    public double? SimilarityScore { get; set; }
    public string? AprendizadoTipo { get; set; }
    public Guid? AprendizadoId { get; set; }
}

public class EconomicPayload
{
    public string PlanType { get; set; } = string.Empty;
    public int StarkCoinBalance { get; set; }
    public int TokensConsumidosSemana { get; set; }
    public int TokensSemanaMax { get; set; }
    public int TokensRestantes { get; set; }
    public bool AdsEnabled { get; set; }
    public int AgendamentosMax { get; set; }
    public int AgendamentosRestantes { get; set; }
    public int Rate { get; set; }

    public int Balance() => StarkCoinBalance;
}
