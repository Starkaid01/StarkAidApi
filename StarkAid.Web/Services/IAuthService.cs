using StarkAid.Web.DTOs;

namespace StarkAid.Web.Services
{
    public interface IAuthService
    {
        Task<LoginResponseDto?> LoginAsync(LoginRequestDto request);
        Task<RegisterResponse?> RegisterAsync(RegisterRequest request);
        Task<bool> RefreshTokenAsync();
        Task LogoutAsync();
        Task<string?> GetAccessTokenAsync();
        Task<string?> GetApiKeyAsync();
    }
}
