using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StarkAid.Api.Data;
using StarkAid.Api.DTOs.V1.Auth;
using StarkAid.Api.Entities;
using StarkAid.Api.Services.V1.Auth;
using StarkAid.Api.Services.V1;
using System.Security.Cryptography;

namespace StarkAid.Api.Controllers.V1;

[ApiVersion("1.0")]
[ApiController]
[Route("api/v{version:apiVersion}/[controller]")]
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
    [AllowAnonymous]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        if (request == null || string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
            return BadRequest("Email e senha são obrigatórios.");

        var user = await _authService.GetUserByEmailAsync(request.Email);
        if (user == null || !_authService.VerifyPasswordHash(request.Password, user.PasswordHash))
            return Unauthorized("Credenciais inválidas.");

        var isFromApp = request.Origem?.ToLower() == "app";
        var token = _authService.GenerateJwtToken(user, isFromApp);
        var refreshToken = await _refreshTokenService.GenerateAndStoreRefreshToken(user, request.Origem ?? "web");

        return Ok(new
        {
            token,
            refreshToken,
            user = new
            {
                user.Id,
                user.Name,
                user.Email,
                user.ApiKey,
                StarkCoinBalance = user.StarkCoins,
                PlanType = user.PlanType
            }
        });
    }

    [HttpPost("refresh-token")]
    [AllowAnonymous]
    public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.RefreshToken))
            return BadRequest("Refresh token é obrigatório.");

        var storedToken = await _refreshTokenService.GetValidRefreshToken(request.RefreshToken);
        if (storedToken == null) return Unauthorized("Refresh token inválido ou expirado.");

        await _refreshTokenService.RevokeToken(storedToken);

        var newJwtToken = _authService.GenerateJwtToken(storedToken.User, storedToken.Origem == "app");
        var newRefreshToken = await _refreshTokenService.GenerateAndStoreRefreshToken(storedToken.User, storedToken.Origem);

        return Ok(new { token = newJwtToken, refreshToken = newRefreshToken });
    }

    [HttpPost("register")]
    [AllowAnonymous]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request)
    {
        if (request == null || string.IsNullOrWhiteSpace(request.Email) || 
            string.IsNullOrWhiteSpace(request.Password) || string.IsNullOrWhiteSpace(request.Name))
            return BadRequest("Nome, email e senha são obrigatórios.");

        if (request.Password.Length < 6)
            return BadRequest("A senha deve ter no mínimo 6 caracteres.");

        var existingUser = await _context.Users.FirstOrDefaultAsync(u => u.Email == request.Email);
        if (existingUser != null)
            return BadRequest("Email já cadastrado.");

        var apiKey = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32)).Replace("+", "-").Replace("/", "_").Replace("=", "");

        var user = new User
        {
            Id = Guid.NewGuid(),
            Name = request.Name,
            Email = request.Email,
            PasswordHash = _authService.HashPassword(request.Password),
            ApiKey = apiKey,
            StarkCoins = 0,
            CreatedAt = DateTimeOffset.UtcNow,
            IsActive = true,
            Role = "UserNivel1",
            RemovalAds = "Desativado",
            PlanType = UserPlanType.Free,
            Estado = request.Estado,
            Cidade = request.Cidade,
            Bairro = request.Bairro
        };

        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        var isFromApp = request.Origem?.ToLower() == "app";
        var token = _authService.GenerateJwtToken(user, isFromApp);
        var refreshToken = await _refreshTokenService.GenerateAndStoreRefreshToken(user, request.Origem ?? "web");

        return Ok(new
        {
            token,
            refreshToken,
            user = new
            {
                user.Id,
                user.Name,
                user.Email,
                user.ApiKey,
                StarkCoins = 0,
                PlanType = UserPlanType.Free
            }
        });
    }
}

public class RegisterRequest
{
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string? Origem { get; set; }
    public string? Estado { get; set; }
    public string? Cidade { get; set; }
    public string? Bairro { get; set; }
}
