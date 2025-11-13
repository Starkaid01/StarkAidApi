using Microsoft.EntityFrameworkCore;
using StarkAid.Api.Data;
using StarkAid.Api.Entities;
using System.Security.Cryptography;

namespace StarkAid.Api.Services;

public class RefreshTokenService
{
    private readonly AppDbContext _context;

    public RefreshTokenService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<string> GenerateAndStoreRefreshToken(User user, string origem = "web")
    {
        var token = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));
        var expiration = DateTime.UtcNow.AddDays(7);

        var refreshToken = new RefreshToken
        {
            Token = token,
            UserId = user.Id,
            Expiration = expiration,
            IsRevoked = false,
            Origem = origem.ToLower() // salva "app" ou "web"
        };

        _context.RefreshTokens.Add(refreshToken);
        await _context.SaveChangesAsync();

        return token;
    }

    public async Task<RefreshToken?> GetValidRefreshToken(string token)
    {
        return await _context.RefreshTokens
            .Include(rt => rt.User)
            .FirstOrDefaultAsync(rt =>
                rt.Token == token &&
                !rt.IsRevoked &&
                rt.Expiration > DateTime.UtcNow);
    }

    public async Task RevokeToken(RefreshToken token)
    {
        token.IsRevoked = true;
        await _context.SaveChangesAsync();
    }
}
