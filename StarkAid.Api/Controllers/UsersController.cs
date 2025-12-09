using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using StarkAid.Api.Data;
using StarkAid.Api.DTOs.SuperIA;
using StarkAid.Api.DTOs.Users;
using StarkAid.Api.Entities;
using StarkAid.Api.Services.Auth;
using StarkAid.Api.Services.Email;
using StarkAid.Api.Services.SuperIA;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Linq;
using System;

namespace StarkAid.Api.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class UsersController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly AuthService _authService;
        private readonly IEmailService _emailService;
        private readonly IaService _iaService;
        private readonly ILogger<UsersController> _logger;

        public UsersController(AppDbContext context, AuthService authService, IEmailService emailService, IaService iaService, ILogger<UsersController> logger)
        {
            _context = context;
            _authService = authService;
            _emailService = emailService;
            _iaService = iaService;
            _logger = logger;
        }

        [HttpGet("nivel")]
        public async Task<IActionResult> GetUserNivel()
        {
            try
            {
                var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
                var user = await _context.Users
                    .Select(u => new { u.Id, u.Role }) // Substituí "Nivel" por "Role", assumindo que "Role" é o campo correto
                    .FirstOrDefaultAsync(u => u.Id == userId);

                if (user == null)
                    return NotFound("Usuário não encontrado");

                return Ok(new { userId = user.Id, nivel = user.Role }); // Substituí "Nivel" por "Role"
            }
            catch (Exception ex)
            {
                return BadRequest($"Erro ao obter nível do usuário: {ex.Message}");
            }
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetUser(Guid id)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null) return NotFound();
            return Ok(user);
        }

        [HttpGet("by-email/{email}")]
        [Authorize(Policy = "AdministradorOnly")]
        public async Task<IActionResult> GetUserByEmail(string email)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == email);
            if (user == null) return NotFound();
            return Ok(new { id = user.Id, userId = user.Id, email = user.Email, name = user.Name });
        }

        [HttpPost("ia/super")]
        public async Task<IActionResult> SuperIA([FromBody] SuperIaDto request)
        {
            var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
            var user = await _context.Users.FindAsync(userId);
            if (user == null) return Unauthorized();

            var resultado = await _iaService.ProcessarMensagem(request.ContextoUser, request.ContextoIA, request.Texto, request.Estilo);
            if (resultado == null) return StatusCode(500, "Erro ao processar mensagem.");

            // Cálculo de custo
            var custoUsd = _iaService.CalcularCustoUSD(resultado);
            var custoSC = custoUsd / 0.03m;
            if (user.StarkCoins < custoSC)
                throw new InvalidOperationException("Saldo insuficiente para gerar variações.");

            // Debita saldo e salva
            user.StarkCoins -= custoSC;
            await _context.SaveChangesAsync();

            // Salvar no histórico
            var historico = new IaHistorico
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                TextoUsuario = request.Texto,
                TextoIa = resultado.Texto,
                CriadoEm = DateTimeOffset.UtcNow
            };
            _context.IaHistoricos.Add(historico);
            await _context.SaveChangesAsync();

            return Ok(resultado);
        }

        [HttpPost("request-password-reset")]
        [AllowAnonymous]
        public async Task<IActionResult> RequestPasswordReset([FromBody] RequestPasswordResetDto request)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == request.Email);
            if (user == null) return Ok("Instruções enviadas por e‑mail."); // Não revelar que o email não existe

            var token = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
            var expiration = DateTime.UtcNow.AddHours(1);

            var resetToken = new PasswordResetToken
            {
                Id = Guid.NewGuid(),
                UserId = user.Id,
                Token = token,
                Expiration = expiration
            };

            _context.PasswordResetTokens.Add(resetToken);
            await _context.SaveChangesAsync();

            var resetLink = $"https://starkaid.runasp.net/password/reset-password.html?token={Uri.EscapeDataString(token)}";
            var emailBody = $"Clique para redefinir sua senha: {resetLink}";

            await _emailService.SendAsync(user.Email, "Redefinição de Senha", emailBody);

            return Ok("Instruções enviadas por e‑mail.");
        }

        [HttpPost("reset-password")]
        [AllowAnonymous]
        public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordDto request)
        {
            if (request.NewPassword != request.RepeatNewPassword)
                return BadRequest("As senhas não coincidem.");

            var resetToken = await _context.PasswordResetTokens
                .Include(rt => rt.User)
                .FirstOrDefaultAsync(rt => rt.Token == request.Token && rt.Expiration > DateTime.UtcNow);

            if (resetToken == null) return BadRequest("Token inválido ou expirado.");

            resetToken.User.PasswordHash = _authService.HashPassword(request.NewPassword);
            _context.PasswordResetTokens.Remove(resetToken);
            await _context.SaveChangesAsync();

            return Ok("Senha redefinida com sucesso.");
        }

        [HttpPut("change-password")]
        public async Task<IActionResult> ChangePassword([FromBody] PasswordChangeDto request)
        {
            var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
            var user = await _context.Users.FindAsync(userId);
            if (user == null) return Unauthorized();

            if (!_authService.VerifyPasswordHash(request.CurrentPassword, user.PasswordHash))
                return BadRequest("Senha atual incorreta.");

            user.PasswordHash = _authService.HashPassword(request.NewPassword);
            await _context.SaveChangesAsync();

            return Ok("Senha alterada com sucesso.");
        }

        [HttpGet("ads")]
        public async Task<IActionResult> GetAdsStatus()
        {
            var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
            var user = await _context.Users
                .Select(u => new { u.Id, u.RemovalAds })
                .FirstOrDefaultAsync(u => u.Id == userId);

            if (user == null)
                return NotFound("Usuário não encontrado.");

            // Retorna "Ativo" se o usuário tem o plano Remove Ads ativo, "Desativado" caso contrário
            // O app verifica: adsReturn.set(ads == "Desativado") - só carrega anúncios se for "Desativado"
            return Ok(new { adsAtiv = user.RemovalAds ?? "Desativado" });
        }

        [HttpGet("me")]
        public async Task<IActionResult> GetCurrentUser()
        {
            var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
            var user = await _context.Users
                .Select(u => new
                {
                    u.Id,
                    u.Name,
                    u.Email,
                    u.Role,
                    u.StarkCoins,
                    u.ApiKey,
                    u.RemovalAds,
                    u.IsActive,
                    u.CreatedAt,
                    u.Estado,
                    u.Cidade,
                    u.Bairro
                })
                .FirstOrDefaultAsync(u => u.Id == userId);

            if (user == null) return NotFound();

            return Ok(user);
        }

        [HttpPut("me")]
        public async Task<IActionResult> UpdateProfile([FromBody] UpdateProfileRequest request)
        {
            var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
            var user = await _context.Users.FindAsync(userId);
            if (user == null) return Unauthorized();

            if (!string.IsNullOrWhiteSpace(request.Name))
                user.Name = request.Name;

            if (!string.IsNullOrWhiteSpace(request.Email))
            {
                // Verificar se email já está em uso por outro usuário
                var existingUser = await _context.Users.FirstOrDefaultAsync(u => u.Email == request.Email && u.Id != userId);
                if (existingUser != null)
                    return BadRequest("Email já está em uso por outro usuário.");

                user.Email = request.Email;
            }

            if (request.Estado != null)
                user.Estado = request.Estado;

            if (request.Cidade != null)
                user.Cidade = request.Cidade;

            if (request.Bairro != null)
                user.Bairro = request.Bairro;

            user.LastUpdatedAt = DateTimeOffset.UtcNow;
            await _context.SaveChangesAsync();

            return Ok(new { message = "Perfil atualizado com sucesso." });
        }

        [HttpDelete("me")]
        public async Task<IActionResult> DeleteAccount([FromBody] DeleteAccountRequest request)
        {
            var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
            var user = await _context.Users.FindAsync(userId);
            if (user == null) return Unauthorized();

            // Verificar senha antes de deletar
            if (!_authService.VerifyPasswordHash(request.Password, user.PasswordHash))
                return BadRequest("Senha incorreta.");

            // Deletar usuário e dados relacionados (cascade)
            _context.Users.Remove(user);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Conta deletada com sucesso." });
        }

        [HttpGet("stats")]
        public async Task<IActionResult> GetUserStats()
        {
            var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));

            var totalDevicesStarkswitch = await _context.Devices.CountAsync(d => d.UserId == userId);
            var totalDevicesEsp = await _context.DispositivosEsp.CountAsync(d => d.UserId == userId);
            var totalDevices = totalDevicesStarkswitch + totalDevicesEsp;
            
            var totalComandosSociais = await _context.ComandosSociais.CountAsync(c => c.UserId == userId);
            var totalAgendamentos = await _context.Agendamentos.CountAsync(a => a.UserId == userId);

            // Status API e MQTT
            var apiStatus = "OK";
            var mqttStatus = "Desconectado";
            var mqttConnected = false;

            try
            {
                var mqttService = HttpContext.RequestServices.GetService<Services.Devices.IMqttClientService>();
                if (mqttService != null)
                {
                    mqttConnected = mqttService.IsConnected;
                    mqttStatus = mqttConnected ? "Conectado" : "Desconectado";
                }
            }
            catch
            {
                // Ignora erro se MQTT não estiver disponível
            }

            return Ok(new
            {
                totalDevices,
                totalComandosSociais,
                totalAgendamentos,
                apiStatus,
                mqttStatus,
                mqttConnected
            });
        }

        [HttpPost("add-funds")]
        public async Task<IActionResult> CreateAddFundsSession([FromBody] AddFundsRequest request)
        {
            var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
            var user = await _context.Users.FindAsync(userId);
            if (user == null) return Unauthorized();

            if (request.Amount <= 0)
                return BadRequest("Valor deve ser maior que zero.");

            var stripeService = HttpContext.RequestServices.GetService<Services.Assinatura.StripeService>();
            var stripeSettings = HttpContext.RequestServices.GetService<IOptions<Options.StripeSettings>>();

            if (stripeService == null || stripeSettings == null)
                return StatusCode(500, "Serviço de pagamento não disponível.");

            // Detectar origem da requisição (app, software ou web)
            var isFromAppClaim = User.FindFirstValue("IsFromApp");
            var isFromApp = isFromAppClaim?.ToLower() == "true";
            var isFromSoftware = false;
            
            // Verificar header X-From-Software (Windows Forms)
            if (Request.Headers.ContainsKey("X-From-Software"))
            {
                var fromSoftwareHeader = Request.Headers["X-From-Software"].ToString();
                isFromSoftware = fromSoftwareHeader?.ToLower() == "true";
                _logger.LogInformation("💻 Detectado via header X-From-Software: {Header}", fromSoftwareHeader);
            }
            
            // Fallback: verificar header X-From-App caso o claim não esteja presente
            if (!isFromApp && !isFromSoftware && Request.Headers.ContainsKey("X-From-App"))
            {
                var fromAppHeader = Request.Headers["X-From-App"].ToString();
                isFromApp = fromAppHeader?.ToLower() == "true";
                _logger.LogInformation("📱 Detectado via header X-From-App: {Header}", fromAppHeader);
            }
            
            // Log para debug
            _logger.LogInformation("💰 Criando sessão de pagamento - IsFromApp: {IsFromApp}, IsFromSoftware: {IsFromSoftware}, User-Agent: {UserAgent}", 
                isFromApp, isFromSoftware, Request.Headers["User-Agent"].ToString());
            
            string successUrl, cancelUrl;
            if (isFromSoftware)
            {
                // Quando chamado do Windows Forms, usar SoftwareDeepLink
                var softwareDeepLink = stripeSettings.Value.SoftwareDeepLink ?? "http://localhost:8765/payment";
                successUrl = $"{softwareDeepLink}?funds=success";
                cancelUrl = $"{softwareDeepLink}?funds=cancel";
                _logger.LogInformation("💻 Usando deep link para software: {SuccessUrl}", successUrl);
            }
            else if (isFromApp)
            {
                // Quando chamado do app, usar deep link
                var appDeepLink = stripeSettings.Value.AppDeepLink ?? "starkaid://payment";
                successUrl = $"{appDeepLink}?funds=success";
                cancelUrl = $"{appDeepLink}?funds=cancel";
                _logger.LogInformation("📱 Usando deep link para app: {SuccessUrl}", successUrl);
            }
            else
            {
                // Quando chamado do HTML, usar URL da página
                var baseUrl = $"{Request.Scheme}://{Request.Host}";
                successUrl = $"{baseUrl}/automacao.html?funds=success";
                cancelUrl = $"{baseUrl}/automacao.html?funds=cancel";
                _logger.LogInformation("🌐 Usando URL HTML: {SuccessUrl}", successUrl);
            }

            var paymentResult = await stripeService.CreateOneTimePaymentSessionAsync(
                user,
                request.Amount,
                successUrl,
                cancelUrl
            );

            var session = paymentResult.session;
            var customer = paymentResult.customer;

            // Salvar referência do pagamento
            var pagamento = new PagamentoAvulso
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                Valor = request.Amount,
                StripeSessionId = session.Id,
                StripeCustomerId = customer.Id,
                Status = "pendente",
                DataCriacao = DateTimeOffset.UtcNow
            };

            _context.PagamentosAvulsos.Add(pagamento);
            await _context.SaveChangesAsync();

            return Ok(new
            {
                checkoutUrl = session.Url,
                sessionId = session.Id
            });
        }

        [HttpPost("online")]
        public async Task<IActionResult> SetUserOnline([FromBody] SetUserOnlineRequest request)
        {
            try
            {
                _logger.LogInformation("📱 [SetUserOnline] Endpoint chamado - Origem: {Origem}", request?.Origem);
                
                var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
                var user = await _context.Users.FindAsync(userId);
                if (user == null)
                {
                    _logger.LogWarning("⚠️ [SetUserOnline] Usuário não encontrado - UserId: {UserId}", userId);
                    return Unauthorized();
                }

                _logger.LogInformation("👤 [SetUserOnline] Usuário encontrado - Nome: {Name}, Email: {Email}", user.Name, user.Email);

                // Validar origem
                if (string.IsNullOrWhiteSpace(request?.Origem) || 
                    !new[] { "web", "soft", "app" }.Contains(request.Origem.ToLower()))
                {
                    _logger.LogWarning("⚠️ [SetUserOnline] Origem inválida: {Origem}", request?.Origem);
                    return BadRequest("Origem deve ser 'web', 'soft' ou 'app'.");
                }

                var origem = request.Origem.ToLower();
                // Obter token do header Authorization
                var authHeader = Request.Headers["Authorization"].ToString();
                var token = authHeader.Replace("Bearer ", "").Trim();

                _logger.LogInformation("🔑 [SetUserOnline] Token obtido - Primeiros 20 chars: {TokenPrefix}", 
                    token.Length > 20 ? token.Substring(0, 20) + "..." : token);

                // Verificar se já existe uma sessão ativa para este usuário e origem
                var existingSession = await _context.UserSessions
                    .FirstOrDefaultAsync(s => s.UserId == userId && 
                                             s.Origem == origem && 
                                             s.IsActive);

                if (existingSession != null)
                {
                    _logger.LogInformation("🔄 [SetUserOnline] Atualizando sessão existente - SessionId: {SessionId}", existingSession.Id);
                    // Atualizar sessão existente
                    existingSession.LastActivityAt = DateTime.UtcNow;
                    existingSession.Token = token;
                }
                else
                {
                    _logger.LogInformation("✨ [SetUserOnline] Criando nova sessão - UserId: {UserId}, Origem: {Origem}", userId, origem);
                    // Criar nova sessão
                    var session = new UserSession
                    {
                        Id = 0, // Será gerado pelo banco
                        UserId = userId,
                        SessionName = $"{user.Name} - {origem}",
                        Token = token,
                        Origem = origem,
                        IsActive = true,
                        CreatedAt = DateTime.UtcNow,
                        LastActivityAt = DateTime.UtcNow
                    };

                    _context.UserSessions.Add(session);
                }

                await _context.SaveChangesAsync();
                _logger.LogInformation("✅ [SetUserOnline] Usuário marcado como online com sucesso - UserId: {UserId}, Origem: {Origem}", userId, origem);
                return Ok(new { message = "Usuário marcado como online." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ [SetUserOnline] Erro ao processar requisição");
                return StatusCode(500, new { message = "Erro ao processar requisição.", error = ex.Message });
            }
        }

        [HttpPost("offline")]
        public async Task<IActionResult> SetUserOffline([FromBody] SetUserOfflineRequest request)
        {
            try
            {
                _logger.LogInformation("📱 [SetUserOffline] Endpoint chamado - Origem: {Origem}", request?.Origem);
                
                var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
                var user = await _context.Users.FindAsync(userId);
                if (user == null)
                {
                    _logger.LogWarning("⚠️ [SetUserOffline] Usuário não encontrado - UserId: {UserId}", userId);
                    return Unauthorized();
                }

                _logger.LogInformation("👤 [SetUserOffline] Usuário encontrado - Nome: {Name}, Email: {Email}", user.Name, user.Email);

                // Validar origem
                if (string.IsNullOrWhiteSpace(request?.Origem) || 
                    !new[] { "web", "soft", "app" }.Contains(request.Origem.ToLower()))
                {
                    _logger.LogWarning("⚠️ [SetUserOffline] Origem inválida: {Origem}", request?.Origem);
                    return BadRequest("Origem deve ser 'web', 'soft' ou 'app'.");
                }

                var origem = request.Origem.ToLower();

                // Desativar todas as sessões ativas para este usuário e origem
                var activeSessions = await _context.UserSessions
                    .Where(s => s.UserId == userId && 
                               s.Origem == origem && 
                               s.IsActive)
                    .ToListAsync();

                _logger.LogInformation("🔍 [SetUserOffline] Encontradas {Count} sessões ativas para desativar", activeSessions.Count);

                foreach (var session in activeSessions)
                {
                    session.IsActive = false;
                }

                await _context.SaveChangesAsync();
                _logger.LogInformation("✅ [SetUserOffline] Usuário marcado como offline com sucesso - UserId: {UserId}, Origem: {Origem}", userId, origem);
                return Ok(new { message = "Usuário marcado como offline." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ [SetUserOffline] Erro ao processar requisição");
                return StatusCode(500, new { message = "Erro ao processar requisição.", error = ex.Message });
            }
        }

        [HttpPost("error-logs/soft/sync")]
        public async Task<IActionResult> SyncErrorLogsSoft([FromBody] SyncErrorLogsSoftRequest request)
        {
            try
            {
                var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));

                // Validar que o userId da requisição corresponde ao usuário autenticado
                if (request.UserId != userId)
                {
                    return Forbid("Você só pode sincronizar seus próprios logs.");
                }

                // Deletar todos os logs existentes do usuário
                var existingLogs = await _context.ErrorLogsSoft
                    .Where(e => e.UserId == userId)
                    .ToListAsync();
                
                _context.ErrorLogsSoft.RemoveRange(existingLogs);

                // Adicionar os novos logs
                var newLogs = request.Logs.Select(log => new ErrorLogSoft
                {
                    UserId = userId,
                    UltimoComando = log.UltimoComando,
                    UltimaResposta = log.UltimaResposta,
                    UltimoDispositivoAcionado = log.UltimoDispositivoAcionado,
                    ErroCompleto = log.ErroCompleto,
                    CodigoDeErro = log.CodigoDeErro,
                    DataErro = log.DataErro,
                    HoraErro = log.HoraErro,
                    AcaoErro = log.AcaoErro,
                    CreatedAt = DateTimeOffset.UtcNow
                }).ToList();

                _context.ErrorLogsSoft.AddRange(newLogs);
                await _context.SaveChangesAsync();

                _logger.LogInformation("✅ Sincronizados {Count} logs de erro para usuário {UserId}", newLogs.Count, userId);
                return Ok(new { message = $"Sincronizados {newLogs.Count} logs de erro.", count = newLogs.Count });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Erro ao sincronizar logs de erro");
                return StatusCode(500, new { message = "Erro ao processar requisição.", error = ex.Message });
            }
        }

        [HttpPost("error-logs/app/sync")]
        public async Task<IActionResult> SyncErrorLogsApp([FromBody] SyncErrorLogsAppRequest request)
        {
            try
            {
                _logger.LogInformation("📱 [SyncErrorLogsApp] Endpoint chamado - UserId recebido: {UserId}, Logs: {Count}", request?.UserId, request?.Logs?.Count ?? 0);
                
                var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
                
                // Converter userId da requisição (String) para Guid
                Guid requestUserId;
                if (!Guid.TryParse(request.UserId, out requestUserId))
                {
                    _logger.LogWarning("⚠️ [SyncErrorLogsApp] UserId inválido na requisição: {UserId}", request?.UserId);
                    return BadRequest("UserId inválido.");
                }

                // Validar que o userId da requisição corresponde ao usuário autenticado
                if (requestUserId != userId)
                {
                    _logger.LogWarning("⚠️ [SyncErrorLogsApp] Tentativa de sincronizar logs de outro usuário. Autenticado: {AuthUserId}, Requisição: {RequestUserId}", userId, requestUserId);
                    return Forbid("Você só pode sincronizar seus próprios logs.");
                }
                
                _logger.LogInformation("✅ [SyncErrorLogsApp] Validação OK - UserId: {UserId}, Logs para sincronizar: {Count}", userId, request?.Logs?.Count ?? 0);

                // Deletar todos os logs existentes do usuário
                var existingLogs = await _context.ErrorLogsApp
                    .Where(e => e.UserId == userId)
                    .ToListAsync();
                
                _context.ErrorLogsApp.RemoveRange(existingLogs);

                // Adicionar os novos logs
                var newLogs = request.Logs.Select(log => new ErrorLogApp
                {
                    UserId = userId,
                    UltimoComando = log.UltimoComando,
                    UltimaResposta = log.UltimaResposta,
                    UltimoDispositivoAcionado = log.UltimoDispositivoAcionado,
                    ErroCompleto = log.ErroCompleto,
                    CodigoDeErro = log.CodigoDeErro,
                    DataErro = log.DataErro,
                    HoraErro = log.HoraErro,
                    AcaoErro = log.AcaoErro,
                    CreatedAt = DateTimeOffset.UtcNow
                }).ToList();

                _context.ErrorLogsApp.AddRange(newLogs);
                await _context.SaveChangesAsync();

                _logger.LogInformation("✅ [SyncErrorLogsApp] Sincronizados {Count} logs de erro do app para usuário {UserId}", newLogs.Count, userId);
                return Ok(new { message = $"Sincronizados {newLogs.Count} logs de erro.", count = newLogs.Count });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Erro ao sincronizar logs de erro do app");
                return StatusCode(500, new { message = "Erro ao processar requisição.", error = ex.Message });
            }
        }

        [HttpGet("online")]
        [Authorize(Roles = "Administrador,userAdmin")]
        public async Task<IActionResult> GetOnlineUsers()
        {
            try
            {
                _logger.LogInformation("🔍 [GetOnlineUsers] Buscando usuários online...");
                
                // Buscar todas as sessões ativas
                var onlineSessions = await _context.UserSessions
                    .Include(s => s.User)
                    .Where(s => s.IsActive)
                    .GroupBy(s => s.UserId)
                    .Select(g => new
                    {
                        UserId = g.Key,
                        User = g.First().User,
                        Origens = g.Select(s => s.Origem).ToList(),
                        LastActivity = g.Max(s => s.LastActivityAt ?? s.CreatedAt)
                    })
                    .ToListAsync();

                _logger.LogInformation("📊 [GetOnlineUsers] Encontradas {Count} sessões ativas", onlineSessions.Count);

                var result = onlineSessions.Select(s => new
                {
                    id = s.User.Id,
                    name = s.User.Name,
                    email = s.User.Email,
                    role = s.User.Role,
                    starkCoins = s.User.StarkCoins,
                    origem = string.Join(", ", s.Origens.Distinct())
                }).ToList();

                _logger.LogInformation("✅ [GetOnlineUsers] Retornando {Count} usuários online", result.Count);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ [GetOnlineUsers] Erro ao buscar usuários online");
                return StatusCode(500, new { message = "Erro ao buscar usuários online", error = ex.Message });
            }
        }
    }

    public class UpdateProfileRequest
    {
        public string? Name { get; set; }
        public string? Email { get; set; }
        public string? Estado { get; set; }
        public string? Cidade { get; set; }
        public string? Bairro { get; set; }
    }

    public class DeleteAccountRequest
    {
        public string Password { get; set; } = string.Empty;
    }

    public class AddFundsRequest
    {
        public decimal Amount { get; set; }
    }

    public class SetUserOnlineRequest
    {
        public string Origem { get; set; } = string.Empty; // web, soft, app
    }

    public class SetUserOfflineRequest
    {
        public string Origem { get; set; } = string.Empty; // web, soft, app
    }

    public class SyncErrorLogsSoftRequest
    {
        public Guid UserId { get; set; }
        public List<ErrorLogSoftDto> Logs { get; set; } = new();
    }

    public class ErrorLogSoftDto
    {
        public string? UltimoComando { get; set; }
        public string? UltimaResposta { get; set; }
        public string? UltimoDispositivoAcionado { get; set; }
        public string? ErroCompleto { get; set; }
        public string? CodigoDeErro { get; set; }
        public string DataErro { get; set; } = string.Empty;
        public string HoraErro { get; set; } = string.Empty;
        public string AcaoErro { get; set; } = string.Empty;
    }

    public class SyncErrorLogsAppRequest
    {
        public string UserId { get; set; } = string.Empty; // String para compatibilidade com app Kotlin
        public List<ErrorLogAppDto> Logs { get; set; } = new();
    }

    public class ErrorLogAppDto
    {
        public string? UltimoComando { get; set; }
        public string? UltimaResposta { get; set; }
        public string? UltimoDispositivoAcionado { get; set; }
        public string? ErroCompleto { get; set; }
        public string? CodigoDeErro { get; set; }
        public string DataErro { get; set; } = string.Empty;
        public string HoraErro { get; set; } = string.Empty;
        public string AcaoErro { get; set; } = string.Empty;
    }
}
