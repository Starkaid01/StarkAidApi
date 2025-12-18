using Microsoft.JSInterop;
using StarkAid.Web.DTOs;
using StarkAid.Web.Services;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
public class AuthService : IAuthService
{
    private readonly HttpClient _http;
    private readonly IJSRuntime _js;

    public AuthService(HttpClient http, IJSRuntime js)
    {
        _http = http;
        _js = js;
    }

    public async Task<LoginResponseDto?> LoginAsync(LoginRequestDto request)
    {
        var response = await _http.PostAsJsonAsync("api/v1/Auth/login", request);  // Corrigido endpoint para combinar com ApiService

        if (!response.IsSuccessStatusCode)
            return null;

        var auth = await response.Content.ReadFromJsonAsync<LoginResponseDto>(
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
        );

        if (auth is not null)
        {
            await _js.InvokeVoidAsync("localStorage.setItem", "token", auth.AccessToken);
            await _js.InvokeVoidAsync("localStorage.setItem", "apiKey", auth.ApiKey);
        }

        return auth;
    }

    public async Task LogoutAsync()
    {
        await _js.InvokeVoidAsync("localStorage.removeItem", "token");
        await _js.InvokeVoidAsync("localStorage.removeItem", "apiKey");
    }

    public Task<string?> GetAccessTokenAsync()
        => _js.InvokeAsync<string?>("localStorage.getItem", "token").AsTask();

    public Task<bool> RefreshTokenAsync()
        => Task.FromResult(false);

    public Task<RegisterResponse?> RegisterAsync(RegisterRequest request)
        => throw new NotImplementedException();
}