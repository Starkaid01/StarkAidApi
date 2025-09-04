using BCrypt.Net;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using StarkAid.Api.Data;
using StarkAid.Api.DTOs;
using StarkAid.Api.Entities;
using StarkAid.Api.Services;
using Stripe;
using System.Security.Claims;

namespace StarkAid.Api.Controllers;

[Route("api/[controller]")]
[ApiController]
public class UsersController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly AuthService _authService;
    private readonly IEmailService _emailService;
    private readonly StripeService _stripeService;
    private readonly EntityConfigurations.StripeSettings _stripeSettings;
    private readonly ILogger<StripeWebhookService> _logger;

    public UsersController(AppDbContext context, AuthService authService, IEmailService emailService, StripeService stripeService, IOptions<EntityConfigurations.StripeSettings> stripeOptions, ILogger<StripeWebhookService> logger)
    {
        _context = context;
        _authService = authService;
        _emailService = emailService;
        _stripeService = stripeService;
        _stripeSettings = stripeOptions.Value;
        _logger = logger;
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

        var resetLink = $"http://starkaid.vbweb.com.br/password/reset-password.html?token={Uri.EscapeDataString(token)}";

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

    [HttpGet("nivel")]
    public async Task<IActionResult> GetNivelUsuario()
    {
        // Obtém o ID do usuário a partir do token JWT
        var userIdFromToken = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userIdFromToken))
            return Unauthorized("Token inválido ou ausente.");

        // Busca apenas o campo Role no banco para evitar carregar dados desnecessários
        var role = await _context.Users
            .Where(u => u.Id.ToString() == userIdFromToken)
            .Select(u => u.Role)
            .FirstOrDefaultAsync();

        if (role == null)
            return NotFound("Usuário não encontrado.");

        return Ok(new { Nivel = role });
    }

    [Authorize(Policy = "UserNivel3Only")]
    [HttpGet("nivel3-only")]
    public IActionResult Nivel3Only()
    {
        return Ok("Acesso exclusivo para UserNivel3.");
    }

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

    [Authorize]
    [HttpDelete("delete-account")]
    public async Task<IActionResult> DeleteAccount()
    {
        var userIdFromToken = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(userIdFromToken, out Guid userId))
            return BadRequest("Token inválido");

        // Buscar o usuário e suas assinaturas com tracking
        var user = await _context.Users
            .Include(u => u.Assinaturas)
            .AsTracking() // IMPORTANTE: Permite atualizações
            .FirstOrDefaultAsync(u => u.Id == userId);

        if (user == null)
            return NotFound("Usuário não encontrado");

        var canceledSubscriptions = new List<Guid>();
        var stripeErrors = new List<string>();

        /////////////////////////////////////////////
        _logger.LogInformation($"usuário id encontrado: {userId}");

        // Buscar todas as assinaturas ativas do usuário
        var assinaturasAtivas = await _context.Assinaturas
            .Where(a => a.UserId == userId &&
                       a.Status == "Ativa")
            .ToListAsync();

        if (!assinaturasAtivas.Any())
        {
            _logger.LogInformation("Nenhuma assinatura ativa encontrada para cancelar.");
            return BadRequest("Nenhuma assinatura ativa encontrada para cancelar.");
        }

        var results = new List<SubscriptionCancelResult>();

        foreach (var assinatura in assinaturasAtivas)
        {
            // Tentar cancelar no Stripe
            var stripeResult = await _stripeService.CancelSubscriptionAsync(assinatura.StripeSubscriptionId!);
            _logger.LogInformation($"Cancelando assinatura no Stripe: {assinatura.StripeSubscriptionId}");

            // Atualizar status localmente
            assinatura.Status = "Cancelada";
            _logger.LogInformation($"Cancelada {assinatura.Id} ");
            assinatura.CanceladaEm = DateTimeOffset.UtcNow;
            _context.Assinaturas.Update(assinatura);

            results.Add(new SubscriptionCancelResult(
                assinatura.Id,
                stripeResult != null ? "Cancelada" : "Falha no cancelamento",
                stripeResult?.Status ?? "Erro"

            ));
            _logger.LogInformation($"Resultado do cancelamento: {stripeResult?.Status}");
        }

        // Rebaixar usuário para nível 1        
        if (user != null)
        {
            user.Role = "UserNivel1";
            _context.Users.Update(user);
        }

        await _context.SaveChangesAsync();

        _logger.LogInformation($"Total de assinaturas canceladas: {results.Count}");

        // Deletar o usuário e dependências
        await DeleteUserAndDependencies(userId);

        string message = "Conta deletada com sucesso.";

        if (canceledSubscriptions.Any())
        {
            message += $"\n{canceledSubscriptions.Count} assinatura(s) cancelada(s).";
            Console.WriteLine($"{canceledSubscriptions.Count} assinatura(s) cancelada(s).");
        }

        if (stripeErrors.Any())
        {
            message += $"\nErros: {string.Join(", ", stripeErrors)}";
            Console.WriteLine("Erros ao cancelar assinaturas: ", string.Join(", ", stripeErrors));
        }

        return Ok(new { Message = message });
    }

    private async Task DeleteUserAndDependencies(Guid userId)
    {
        // 1. Deletar dispositivos vinculados
        var dispositivos = await _context.Devices
            .Where(d => d.UserId == userId)
            .ToListAsync();

        _context.Devices.RemoveRange(dispositivos);

        // 2. Deletar comandos sociais
        var comandos = await _context.ComandosSociais
            .Where(c => c.UserId == userId)
            .ToListAsync();

        _context.ComandosSociais.RemoveRange(comandos);

        // 3. Deletar tokens de reset
        var resetTokens = await _context.PasswordResetTokens
            .Where(t => t.UserId == userId)
            .ToListAsync();

        _context.PasswordResetTokens.RemoveRange(resetTokens);

        
        // 5. Finalmente deletar o usuário
        var user = await _context.Users.FindAsync(userId);
        if (user != null)
        {
            _context.Users.Remove(user);
        }

        await _context.SaveChangesAsync();
    }
}