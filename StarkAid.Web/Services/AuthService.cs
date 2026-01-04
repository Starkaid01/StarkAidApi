using Microsoft.JSInterop;
using StarkAid.Web.DTOs;
using StarkAid.Web.Services;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components.Authorization;

namespace StarkAid.Web.Services;

public class AuthService : IAuthService
{
    private readonly HttpClient _http;
    private readonly IJSRuntime _js;
    private readonly AuthenticationStateProvider _authStateProvider;

    public AuthService(HttpClient http, IJSRuntime js, AuthenticationStateProvider authStateProvider)
    {
        _http = http;
        _js = js;
        _authStateProvider = authStateProvider;
    }

    public async Task<LoginResponseDto?> LoginAsync(LoginRequestDto request)
    {
        var response = await _http.PostAsJsonAsync("api/v1/Auth/login", request);

        if (!response.IsSuccessStatusCode)
            return null;

        var auth = await response.Content.ReadFromJsonAsync<LoginResponseDto>(
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
        );

        if (auth is not null)
        {
            await _js.InvokeVoidAsync("localStorage.setItem", "token", auth.AccessToken);
            await _js.InvokeVoidAsync("localStorage.setItem", "apiKey", auth.ApiKey);
            
            ((CustomAuthenticationStateProvider)_authStateProvider).NotifyUserAuthentication(auth.AccessToken);
        }

        return auth;
    }

    public async Task LogoutAsync()
    {
        await _js.InvokeVoidAsync("localStorage.removeItem", "token");
        await _js.InvokeVoidAsync("localStorage.removeItem", "apiKey");
        
        ((CustomAuthenticationStateProvider)_authStateProvider).NotifyUserLogout();
    }

    public Task<string?> GetAccessTokenAsync()
        => _js.InvokeAsync<string?>("localStorage.getItem", "token").AsTask();

    public Task<string?> GetApiKeyAsync()
        => _js.InvokeAsync<string?>("localStorage.getItem", "apiKey").AsTask();

    public Task<bool> RefreshTokenAsync()
        => Task.FromResult(false);

    public Task<RegisterResponse?> RegisterAsync(RegisterRequest request)
        => throw new NotImplementedException();
}