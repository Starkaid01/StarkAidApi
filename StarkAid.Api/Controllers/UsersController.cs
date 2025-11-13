using BCrypt.Net;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using StarkAid.Api.Data;
using StarkAid.Api.DTOs.Assinatura;
using StarkAid.Api.DTOs.Spotify;
using StarkAid.Api.DTOs.SuperIA;
using StarkAid.Api.DTOs.Users;
using StarkAid.Api.Entities;
using StarkAid.Api.Services.Assinatura;
using StarkAid.Api.Services.Auth;
using StarkAid.Api.Services.SuperIA;
using StarkAid.Api.Services.Users;
using Stripe;
using System.IO.Compression;
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
    private readonly IaService _iaService; // <-- adiciona isso
    private readonly IWebHostEnvironment _env;

    private const string ValidKey = "ad5478r45t785g468t41df561254r4e785s654s1t54t54g5h";

    public UsersController(AppDbContext context, AuthService authService, IEmailService emailService, StripeService stripeService, IOptions<EntityConfigurations.StripeSettings> stripeOptions, ILogger<StripeWebhookService> logger, IaService iaService, IWebHostEnvironment env)
    {
        _context = context;
        _authService = authService;
        _emailService = emailService;
        _stripeService = stripeService;
        _stripeSettings = stripeOptions.Value;
        _logger = logger;
        _iaService = iaService;
        _env = env;
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

        var isFromApp = dto.Origem?.ToLower() == "app";

        var token = _authService.GenerateJwtToken(user, isFromApp);



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

    [HttpGet("{id}/starkcoins")]
    public async Task<ActionResult<User>> GetStarkCoins(Guid id)
    {
        var user = await _context.Users.FindAsync(id);

        if (user == null)
            return NotFound();

        return Ok(new
        {
            user.StarkCoins,
        });
    }

    [Authorize]
    [HttpPatch("{id}/update-starkcoins-ia")]
    public async Task<IActionResult> UpdateStarkCoinsIa(Guid id)
    {
        var user = await _context.Users.FindAsync(id);
        if (user == null)
            return NotFound("Usuário não encontrado.");

        const decimal valorDebito = 0.1m;

        if (user.StarkCoins < valorDebito)
            return BadRequest("Saldo insuficiente para essa operação.");

        user.StarkCoins -= valorDebito;

        _context.Users.Update(user);
        await _context.SaveChangesAsync();

        return Ok(new
        {
            Message = $"Foram debitados {valorDebito} StarkCoins.",
            SaldoAtual = user.StarkCoins
        });
    }

    [Authorize]
    [HttpPatch("{id}/update-starkcoins-ads")]
    public async Task<IActionResult> UpdateStarkCoinsAds(Guid id)
    {
        var user = await _context.Users.FindAsync(id);
        if (user == null)
            return NotFound("Usuário não encontrado.");

        const decimal valorCredito = 0.01m;

        user.StarkCoins += valorCredito;

        _context.Users.Update(user);
        await _context.SaveChangesAsync();

        return Ok(new
        {
            Message = $"Foram creditados {valorCredito} StarkCoins.",
            SaldoAtual = user.StarkCoins
        });
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

    [Authorize]
    [HttpGet("ads")]
    public async Task<IActionResult> GetAds()
    {
       
        var userIdFromToken = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(userIdFromToken, out Guid userId))
            return BadRequest("Token inválido");

        var result = new { adsAtiv = "Desativado" };
        var user = await _context.Users.FindAsync(userId);
        if (user == null) return Unauthorized();
        if (user.RemovalAds == "Ativo")
            result = new { adsAtiv = "Ativo" };

        
        return Ok(result);
    }

    [Authorize]
    [HttpPost("musica/tocar")]
    public async Task<IActionResult> TocarMusica([FromBody] MusicaDto dto)
    {
        var userIdFromToken = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(userIdFromToken, out Guid userId))
            return BadRequest("Token inválido");

        if (string.IsNullOrWhiteSpace(dto.NomeMusica))
            return BadRequest(new { autorizado = false, message = "Nome da música não informado" });

        var user = await _context.Users.FindAsync(userId);

        if (user == null) return Unauthorized();

        if (user.StarkCoins < 0.2m)
            return BadRequest(new { autorizado = false, message = "Saldo insuficiente" });


        return Ok(new { autorizado = true, saldoAtual = user.StarkCoins });
    }

    // aceita GET (AdMob envia callback com query params). Mantive POST como fallback.
    [HttpGet("starkcoins/earning")]
    public async Task<IActionResult> StarkCoinsAdsEarning([FromQuery] string key, string userId)
    {
        try
        {
            Console.WriteLine($"[ADMOB-SSV] Callback recebido - Method: {Request.Method} Query: {Request.QueryString}");

            // 1) valida chave
            const string validKey = "ad5478r45t785g468t41df561254r4e785s654s1t54t54g5h";
            if (string.IsNullOrEmpty(key) || key != validKey)
            {
                Console.WriteLine("[ADMOB-SSV] Key inválida");
                return BadRequest("Invalid key");
            }

            // 2) lê user_id da query string (prioridade)
            string customData = Request.Query["custom_data"].FirstOrDefault();

            // fallback: POST form-data (caso algum teste envie assim)
            if (string.IsNullOrEmpty(userId) && Request.HasFormContentType)
            {
                userId = Request.Form["user_id"].FirstOrDefault();
                customData = Request.Form["custom_data"].FirstOrDefault();
            }

            Console.WriteLine($"[ADMOB-SSV] user_id={userId}, custom_data={customData}");

            if (string.IsNullOrEmpty(userId))
            {
                Console.WriteLine("[ADMOB-SSV] UserId não fornecido");
                return BadRequest("User ID required");
            }

            // 3) Processa recompensa em background
            _ = Task.Run(async () => await ProcessarRecompensaAsync(userId));

            // 4) Resposta 200 OK sem corpo (o AdMob aceita assim)
            return Ok();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ADMOB-SSV] Erro: {ex}");
            return StatusCode(500);
        }
    }

    private async Task ProcessarRecompensaAsync(string userId)
    {
        try
        {
            await Task.Delay(300); // pequeno buffer

            if (Guid.TryParse(userId, out Guid userGuid))
            {
                var user = await _context.Users.FindAsync(userGuid);
                if (user != null)
                {
                    user.StarkCoins += 0.01m;
                    await _context.SaveChangesAsync();
                    Console.WriteLine($"[ADMOB-SSV] ✅ StarkCoins creditados para: {userId}");
                }
                else
                {
                    Console.WriteLine($"[ADMOB-SSV] ❌ Usuário não encontrado: {userId}");
                }
            }
            else
            {
                Console.WriteLine($"[ADMOB-SSV] ❌ UserId em formato inválido: {userId}");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ADMOB-SSV] ❌ Erro no processamento: {ex}");
        }
    }


    [Authorize]
    [HttpPost("ia/super")]
    public async Task<IActionResult> SuperIA([FromBody] SuperIaDto dto)
    {
        var userIdFromToken = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(userIdFromToken, out Guid userId))
            return BadRequest("Token inválido");

        if (string.IsNullOrWhiteSpace(dto.Texto))
            return BadRequest("Texto não informado");

        var user = await _context.Users.FindAsync(userId);
        if (user == null) return Unauthorized();

        // 🔹 Busca últimas 3 interações do usuário
        var historico = await _context.IaHistoricos
            .Where(h => h.UserId == userId)
            .OrderByDescending(h => h.CriadoEm)
            .Take(3)
            .ToListAsync();

        // 🔹 Prepara contexto para IA (mais antigo -> mais recente)
        var contextoUser = string.Join("\n", historico.OrderBy(h => h.CriadoEm).Select(h => h.TextoUsuario));
        var contextoIA = string.Join("\n", historico.OrderBy(h => h.CriadoEm).Select(h => h.TextoIa));

        // 🔹 Estimativa de tokens (1 token ≈ 4 caracteres)
        int estimativaTokens = (contextoUser.Length + contextoIA.Length) / 4;
        const int maxTokens = 500; // limite de tokens do histórico

        if (estimativaTokens > maxTokens)
        {
            contextoUser = await _iaService.ResumirTexto(contextoUser, dto.Estilo);
            contextoIA = await _iaService.ResumirTexto(contextoIA, dto.Estilo);
        }

        var resultado = await _iaService.ProcessarMensagem(contextoUser, contextoIA, dto.Texto, dto.Estilo);
        if (resultado == null || string.IsNullOrWhiteSpace(resultado.Texto))
            return Ok("Não foi possível gerar resposta no momento.");

        // 🔹 Calcula custo
        var custoUsd = _iaService.CalcularCustoUSD(resultado);
        var custoSc1 = custoUsd / 0.02m;
        var custoSC = custoSc1 + 0.01m;
        if (user.StarkCoins < custoSC)
            return Ok("Saldo insuficiente");

        user.StarkCoins -= custoSC;

        // 🔹 Cria nova interação
        var novaInteracao = new IaHistorico
        {
            UserId = userId,
            TextoUsuario = dto.Texto,
            TextoIa = resultado.Texto,
            CriadoEm = DateTimeOffset.UtcNow
        };

        _context.IaHistoricos.Add(novaInteracao);

        // 🔹 Remove interações antigas caso já existam 3
        if (historico.Count >= 3)
        {
            var paraRemover = historico.OrderBy(h => h.CriadoEm).First();
            _context.IaHistoricos.Remove(paraRemover);
        }

        await _context.SaveChangesAsync();

        return Ok(new
        {
            Resposta = resultado.Texto,
            NovoSaldo = user.StarkCoins
        });
    }

        
    // Adicione este endpoint na sua API
    [HttpGet("test-connection")]
    public IActionResult TestConnection()
    {
        return Ok(new
        {
            status = "online",
            message = "API funcionando",
            timestamp = DateTime.UtcNow
        });
    }

    private bool IsValidPhoneNumber(string numero)
    {
        // Checa se começa com "+" e contém apenas dígitos depois
        return !string.IsNullOrEmpty(numero) && System.Text.RegularExpressions.Regex.IsMatch(numero, @"^\+\d{10,15}$");
    }
}