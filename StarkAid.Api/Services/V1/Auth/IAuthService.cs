using StarkAid.Api.Entities;

namespace StarkAid.Api.Services.V1.Auth;

public interface IAuthService
{
    string GenerateJwtToken(User user, bool isFromApp);
    bool VerifyPassword(string password, string hash);
    string HashPassword(string password);
    Task<User?> GetUserByEmailAsync(string email);
}
