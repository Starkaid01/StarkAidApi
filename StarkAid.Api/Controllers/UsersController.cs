using BCrypt.Net;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StarkAid.Api.Data;
using StarkAid.Api.DTOs;
using StarkAid.Api.Entities;
using StarkAid.Api.Services;
using System.Security.Claims;

namespace StarkAid.Api.Controllers;

[Route("api/[controller]")]
[ApiController]
public class UsersController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly AuthService _authService;
    private readonly IEmailService _emailService;

    public UsersController(AppDbContext context, AuthService authService, IEmailService emailService)
    {
        _context = context;
        _authService = authService;
        _emailService = emailService;
    }

    // POST: api/Users
    [HttpPost]
    public async Task<IActionResult> CreateUser(UserCreateDto dto)
    {
        var apiKey = Guid.NewGuid().ToString("N");

        var user = new User
        {
            Name = dto.Name,
            Email = dto.Email,
            PasswordHash = _authService.HashPassword(dto.Password), // agora usando o AuthService
            ApiKey = apiKey,
            StarkCoins = 0,
            CreatedAt = DateTime.UtcNow,
            IsActive = true,
            Role = "UserNivel1"
        };

        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        var token = _authService.GenerateJwtToken(user);

        return CreatedAtAction(nameof(GetUserById), new { id = user.Id }, new
        {
            user.Id,
            user.Name,
            user.Email,
            user.ApiKey,
            user.StarkCoins,
            user.CreatedAt,
            Token = token
        });
    }

    [Authorize]
    [HttpPost("{id}/change-password")]
    public async Task<IActionResult> ChangePassword(Guid id, [FromBody] PasswordChangeDto dto)
    {
        var user = await _context.Users.FindAsync(id);
        if (user == null)
            return NotFound();

        // Valida se o usuário logado é o mesmo do token
        var userIdFromToken = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userIdFromToken != user.Id.ToString())
            return Forbid();

        if (!_authService.VerifyPasswordHash(dto.CurrentPassword, user.PasswordHash))
            return BadRequest("Senha atual incorreta.");

        user.PasswordHash = _authService.HashPassword(dto.NewPassword);
        await _context.SaveChangesAsync();

        return Ok("Senha alterada com sucesso.");
    }

    [HttpPost("request-password-reset")]
    public async Task<IActionResult> RequestPasswordReset([FromBody] RequestPasswordResetDto dto)
    {
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == dto.Email);
        if (user == null)
            return NotFound("Usuário não encontrado.");

        var token = Convert.ToBase64String(Guid.NewGuid().ToByteArray());
        var expiration = DateTime.UtcNow.AddMinutes(30);

        _context.PasswordResetTokens.Add(new PasswordResetToken
        {
            UserId = user.Id,
            Token = token,
            Expiration = expiration
        });

        await _context.SaveChangesAsync();

        var resetLink = $"http://starkaid.com/password/reset-password.html?token={Uri.EscapeDataString(token)}";

        await _emailService.SendAsync(user.Email, "Redefinição de senha", $"Use o link para redefinir sua senha: {resetLink}");

        return Ok("Instruções enviadas para o e-mail.");
    }


    [HttpPost("reset-password")]
    public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordDto dto)
    {
        var resetToken = await _context.PasswordResetTokens
            .FirstOrDefaultAsync(t => t.Token == dto.Token && t.Expiration > DateTime.UtcNow);

        if (resetToken == null)
            return BadRequest("Token inválido ou expirado.");

        var user = await _context.Users.FindAsync(resetToken.UserId);
        if (user == null)
            return NotFound("Usuário não encontrado.");

        if (dto.NewPassword != dto.RepeatNewPassword)
            return BadRequest("As senhas não conferem.");

        user.PasswordHash = _authService.HashPassword(dto.NewPassword);

        _context.PasswordResetTokens.Remove(resetToken); // Consome o token
        await _context.SaveChangesAsync();

        return Ok("Senha redefinida com sucesso.");
    }



    [HttpGet("{id}")]
    public async Task<ActionResult<User>> GetUserById(Guid id)
    {
        var user = await _context.Users.FindAsync(id);

        if (user == null)
            return NotFound();

        return Ok(new
        {
            user.Id,
            user.Name,
            user.Email,
            user.StarkCoins,
            user.CreatedAt,
            user.IsActive
        });
    }

    [Authorize(Policy = "UserNivel1Only")]
    [HttpGet("nivel1-only")]
    public IActionResult Nivel1Only()
    {
        return Ok("Acesso exclusivo para UserNivel1.");
    }

    [Authorize(Policy = "UserNivel2Only")]
    [HttpGet("nivel2-only")]
    public IActionResult Nivel2Only()
    {
        return Ok("Acesso exclusivo para UserNivel2.");
    }

    [Authorize(Policy = "UserNivel3Only")]
    [HttpGet("nivel3-only")]
    public IActionResult Nivel3Only()
    {
        return Ok("Acesso exclusivo para UserNivel3.");
    }

    [Authorize(Policy = "AdministradorOnly")]
    [HttpPatch("{id}/upgrade-to-nivel2")]
    public async Task<IActionResult> UpgradeToNivel2(Guid id)
    {
        var user = await _context.Users.FindAsync(id);
        if (user == null)
            return NotFound("Usuário não encontrado.");

        user.Role = "UserNivel2";
        await _context.SaveChangesAsync();

        return Ok($"Usuário {user.Name} promovido para UserNivel2.");
    }

    [Authorize(Policy = "AdministradorOnly")]
    [HttpPatch("{id}/upgrade-to-nivel3")]
    public async Task<IActionResult> UpgradeToNivel3(Guid id)
    {
        var user = await _context.Users.FindAsync(id);
        if (user == null)
            return NotFound("Usuário não encontrado.");

        user.Role = "UserNivel3";
        await _context.SaveChangesAsync();

        return Ok($"Usuário {user.Name} promovido para UserNivel3.");
    }
}