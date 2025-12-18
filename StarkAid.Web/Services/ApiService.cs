using StarkAid.Web.Dtos;
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
        try
        {
            var response = await _http.PostAsJsonAsync("api/v1/Auth/login", dto);

            if (!response.IsSuccessStatusCode)
                return null;

            return await response.Content.ReadFromJsonAsync<AuthResponseDto>(
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
            );
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine(ex);
            return null;
        }
    }

    public async Task<AuthResponseDto?> RegisterAsync(UserRegisterDto register)
    {
        var response = await _http.PostAsJsonAsync("api/v1/Auth/register", register);
        if (!response.IsSuccessStatusCode) return null;

        return await response.Content.ReadFromJsonAsync<AuthResponseDto>(
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true }  // Adicionado para consistência
        );
    }

    public async Task<UserMeDto?> GetMeAsync(string token, string apiKey)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, "api/v1/Users/me");

        request.Headers.Authorization =
            new AuthenticationHeaderValue("Bearer", token);

        request.Headers.Add("Api-Key", apiKey);

        var response = await _http.SendAsync(request);

        if (!response.IsSuccessStatusCode)
            return null;

        var options = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };

        return await response.Content.ReadFromJsonAsync<UserMeDto>(options);
    }

    public async Task<UserStatsDto?> GetStatsAsync(string token, string apiKey)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, "api/v1/Users/stats");

        request.Headers.Authorization =
            new AuthenticationHeaderValue("Bearer", token);

        request.Headers.Add("Api-Key", apiKey);

        var response = await _http.SendAsync(request);

        if (!response.IsSuccessStatusCode)
            return null;

        var options = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };

        return await response.Content.ReadFromJsonAsync<UserStatsDto>(options);
    }

    // Métodos não utilizados removidos para simplicidade; adicione de volta se necessário
}