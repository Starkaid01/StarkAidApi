using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StarkAid.Api.Data;
using StarkAid.Api.DTOs.Auth;
using StarkAid.Api.Services;
using StarkAid.Api.Services.Auth;

namespace StarkAid.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly AuthService _authService;
    private readonly RefreshTokenService _refreshTokenService;

    private readonly AppDbContext _context;

    public AuthController(AuthService authService, RefreshTokenService refreshTokenService, AppDbContext context)
    {
        _authService = authService;
        _refreshTokenService = refreshTokenService;
        _context = context;
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        if (request == null || string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
            return BadRequest("Email e senha são obrigatórios.");

        var user = await _authService.GetUserByEmailAsync(request.Email);

        if (user == null || !_authService.VerifyPasswordHash(request.Password, user.PasswordHash))
            return Unauthorized("Usuário ou senha inválidos.");

        // 🔑 Define se é login do App
        bool isFromApp = request.Origem?.ToLower() == "app";

        // Passa essa info para geração do token
        var token = _authService.GenerateJwtToken(user, isFromApp);
        var refreshToken = await _refreshTokenService.GenerateAndStoreRefreshToken(user, request.Origem);

        return Ok(new
        {
            token,
            refreshToken,
            id = user.Id,
            apiKey = user.ApiKey
        });
    }

    [HttpPost("test-password")]
    public async Task<IActionResult> TestPassword([FromBody] LoginRequest request)
    {
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == request.Email);
        if (user == null) return NotFound("Usuário não encontrado");

        var isValid = _authService.VerifyPasswordHash(request.Password, user.PasswordHash);
        return Ok(new { isValid });
    }

    [HttpPost("refresh-token")]
    public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.RefreshToken))
            return BadRequest("Refresh token é obrigatório.");

        var storedToken = await _refreshTokenService.GetValidRefreshToken(request.RefreshToken);

        if (storedToken == null)
            return Unauthorized("Refresh token inválido ou expirado.");

        await _refreshTokenService.RevokeToken(storedToken);

        bool isFromApp = storedToken.Origem == "app";
        var newJwtToken = _authService.GenerateJwtToken(storedToken.User, isFromApp);
        var newRefreshToken = await _refreshTokenService.GenerateAndStoreRefreshToken(storedToken.User, storedToken.Origem);

        return Ok(new { token = newJwtToken, refreshToken = newRefreshToken });
    }
}
