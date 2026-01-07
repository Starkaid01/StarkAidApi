using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using StarkAid.Api.Data;
using StarkAid.Api.DTOs.V1.SuperIA;
using StarkAid.Api.DTOs.V1.Users;
using StarkAid.Api.DTOs.V1.Admin;
using StarkAid.Api.Entities;
using StarkAid.Api.Services;
using StarkAid.Api.Services.V1.Auth;
using StarkAid.Api.Services.V1.Email;
using StarkAid.Api.Services.V1.SuperIA;
using StarkAid.Api.Services.V1.Payment.Stripe;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Linq;
using System.Text;
using System.Globalization;
using System.Text.RegularExpressions;
using System;
using StarkAid.Api.Helpers;
using StarkAid.Api.Services.V1.IA;
using StarkAid.Api.Services.Telemetry;
using StarkAid.Api.Options;
using StarkAid.Api.Services.CommandRouter;
using StarkAid.Api.DTOs.Commands;

namespace StarkAid.Api.Controllers.V1
{
    [ApiVersion("1.0")]
    [Authorize]
    [ApiController]
    [Route("api/v{version:apiVersion}/[controller]")]
    public class UsersController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly AuthService _authService;
        private readonly IEmailService _emailService;
        private readonly IaService _iaService;
    private readonly PlanoLimitesService _planoLimites;
    private readonly ITokenUsageService _tokenUsage;
        private readonly ILogger<UsersController> _logger;
        private readonly IAprendizadoService _aprendizadoService;
        private readonly ITelemetryService _telemetryService;
        private readonly AiTelemetryOptions _telemetryOptions;
        private readonly ICommandRouter _commandRouter;
        private readonly StarkAid.Api.Services.V1.Fun.IIntentDetector _intentDetector;

    public UsersController(
        AppDbContext context,
        AuthService authService,
        IEmailService emailService,
        IaService iaService,
        PlanoLimitesService planoLimites,
        ITokenUsageService tokenUsage,
        IAprendizadoService aprendizadoService,
        ITelemetryService telemetryService,
        IOptions<AiTelemetryOptions> telemetryOptions,
        ICommandRouter commandRouter,
        StarkAid.Api.Services.V1.Fun.IIntentDetector intentDetector,
        ILogger<UsersController> logger)
        {
            _context = context;
            _authService = authService;
            _emailService = emailService;
            _iaService = iaService;
            _planoLimites = planoLimites;
            _tokenUsage = tokenUsage;
            _aprendizadoService = aprendizadoService;
            _telemetryService = telemetryService;
            _telemetryOptions = telemetryOptions.Value;
            _commandRouter = commandRouter;
            _intentDetector = intentDetector;
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
        [EnableRateLimiting("IaEndpoint")]
        public async Task<IActionResult> SuperIA([FromBody] SuperIaDto request)
        {
            try
            {
                var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
            var user = await _context.Users.FindAsync(userId);
            if (user == null) return Unauthorized();

            if (string.IsNullOrWhiteSpace(request.Texto))
            {
                var limiteEmpty = _planoLimites.ObterLimiteTokensSemana(user);
                return Ok(new 
                { 
                    resultado = new IaResultado { Texto = "Mensagem vazia", HitResult = "InvalidInput" },
                    planType = user.PlanType.ToString(),
                    tokensConsumidosSemana = user.TokensConsumidosSemana,
                    tokensSemanaMax = limiteEmpty,
                    tokensRestantes = Math.Max(0, limiteEmpty - user.TokensConsumidosSemana),
                    starkCoinBalance = user.StarkCoins
                });
            }

            // Verificar se há assinatura Premium ativa e atualizar PlanType se necessário
            var hasActivePremium = await _context.Assinaturas
                .AnyAsync(a => a.UserId == userId && 
                              (a.Status == "ativa" || a.Status == "Ativa") && 
                              a.Valor == 10 && 
                              (!a.ExpiraEm.HasValue || a.ExpiraEm.Value > DateTimeOffset.UtcNow));
            
            if (hasActivePremium && user.PlanType != UserPlanType.Premium)
            {
                _logger.LogInformation("🔄 [SuperIA] Atualizando PlanType para Premium - UserId: {UserId}", userId);
                user.PlanType = UserPlanType.Premium;
                user.RemovalAds = "Ativo";
                if (user.Role == "UserNivel1")
                {
                    user.Role = "UserNivel2";
                }
                await _context.SaveChangesAsync();
            }

        // 0. Processar via CommandRouter (Math, Piadas, Dispositivos, etc.)
        var commandRequest = new CommandRequestDto
        {
            UserId = userId,
            Texto = request.Texto,
            Origem = "Android",
            Contexto = "privado"
        };

        var commandResult = await _commandRouter.RouteAsync(commandRequest);
        if (commandResult.IsSuccess)
        {
            // Consumir tokens/coins para comandos locais (100 tokens ou 1 StarkCoin)
            var consumeLocal = _tokenUsage.ConsumeTokens(user, 100, request.UseStarkCoins);
            if (!consumeLocal.Success)
            {
                return StatusCode(402, new { message = "Saldo insuficiente. Adicione StarkCoins.", requiredCoins = consumeLocal.RequiredCoins });
            }

            await _context.SaveChangesAsync();

            var limiteFun = _planoLimites.ObterLimiteTokensSemana(user);
            return Ok(new
            {
                resultado = new IaResultado { Texto = commandResult.Message, HitResult = "LocalCommand" },
                planType = user.PlanType.ToString(),
                tokensConsumidosSemana = user.TokensConsumidosSemana,
                tokensSemanaMax = limiteFun,
                tokensRestantes = Math.Max(0, limiteFun - user.TokensConsumidosSemana),
                starkCoinBalance = user.StarkCoins
            });
        }

        if (request.SkipAi)
        {
            var limiteSkip = _planoLimites.ObterLimiteTokensSemana(user);
            return Ok(new
            {
                resultado = new IaResultado { Texto = "", HitResult = "SkippedByRequest" },
                planType = user.PlanType.ToString(),
                tokensConsumidosSemana = user.TokensConsumidosSemana,
                tokensSemanaMax = limiteSkip,
                tokensRestantes = Math.Max(0, limiteSkip - user.TokensConsumidosSemana),
                starkCoinBalance = user.StarkCoins
            });
        }

        // BLOQUEIO ADICIONAL: Se for Piada ou Matemática e falhou no Router Local, 
        // NÃO deve cair na IA para evitar que ela tente dar respostas genéricas ou aprenda comandos básicos.
        var intentSafety = _intentDetector.DetectIntent(request.Texto);
        if (intentSafety != StarkAid.Api.Services.V1.Fun.FunIntent.None)
        {
            return Ok(new
            {
                resultado = new IaResultado { Texto = "Não consegui processar esse comando agora. Tente de outra forma.", HitResult = "FailureBlocked" },
                planType = user.PlanType.ToString(),
                starkCoinBalance = user.StarkCoins
            });
        }

    // 1. Normalizar o texto de entrada para busca (Semântica leve: remove stopwords curtas)
            var textoNormalizado = TextHelper.NormalizarParaBusca(request.Texto);
            var ehAutocontido = EhAutocontido(request.Texto);
            
            // 2. Gerenciar Contexto de Conversa (Herança)
            var conversaContext = await _context.UserConversaContexts.FirstOrDefaultAsync(c => c.UserId == userId);
            if (conversaContext == null)
            {
                conversaContext = new UserConversaContext { UserId = userId };
                _context.UserConversaContexts.Add(conversaContext);
            }

            string? contextoHerdado = null;
            if (!ehAutocontido)
            {
                // Follow-ups herdam o contexto anterior se ele for recente (últimos 10 minutos)
                if (conversaContext.LastUpdatedAt > DateTimeOffset.UtcNow.AddMinutes(-10) && !string.IsNullOrEmpty(conversaContext.ContextoAtual))
                {
                    contextoHerdado = conversaContext.ContextoAtual;
                }
            }

            // 3. Iniciar Rastreamento de Telemetria
            var sw = System.Diagnostics.Stopwatch.StartNew();
            var telemetria = new AiInteractionEvent
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                TextoOriginal = request.Texto,
                TextoNormalizado = textoNormalizado,
                Origem = "Android", // Default, pode ser expandido via DTO
                CreatedAt = DateTimeOffset.UtcNow
            };

            // 4. Verificar Aprendizado (Cache) - Usando Service Unificado
            var searchResult = await _aprendizadoService.BuscarAprendizadoAsync(userId, request.Texto, contextoHerdado);
            telemetria.Resultado = searchResult.Resultado;
            telemetria.SimilarityScore = searchResult.SimilarityScore;
            
            if (searchResult.Match != null)
            {
                telemetria.AprendizadoId = searchResult.Match.Id;
                telemetria.AprendizadoTipo = searchResult.Match.Tipo;

                var limiteSemanal = _planoLimites.ObterLimiteTokensSemana(user);
                var tokensDisponiveis = Math.Max(0, limiteSemanal - user.TokensConsumidosSemana);

                bool processadoPeloCache = false;

                // Prioridade 1: Usar tokens semanais (custo fixo de 100 tokens para cache)
                if (tokensDisponiveis >= 100)
                {
                    user.TokensConsumidosSemana += 100;
                    processadoPeloCache = true;
                }
                // Prioridade 2: Usar StarkCoins se o usuário autorizou (custo fixo de 1 StarkCoin para cache)
                else if (request.UseStarkCoins && user.StarkCoins >= 1)
                {
                    user.StarkCoins -= 1;
                    processadoPeloCache = true;
                }

                if (processadoPeloCache)
                {
                    // As métricas de qualidade (HitCount, Confidence) já foram atualizadas dentro do AprendizadoService

                    // Salvar no histórico
                    var historicoCache = new IaHistorico
                    {
                        Id = Guid.NewGuid(),
                        UserId = userId,
                        TextoUsuario = request.Texto,
                        TextoIa = searchResult.Resposta!,
                        CriadoEm = DateTimeOffset.UtcNow
                    };
                    _context.IaHistoricos.Add(historicoCache);

                    // Registrar Telemetria de Sucesso do Cache
                    sw.Stop();
                    telemetria.LatenciaMs = (int)sw.ElapsedMilliseconds;
                    telemetria.ChamouIaExterna = false;
                    telemetria.TokensEstimadosEvitados = _telemetryOptions.DefaultTokensPerInteraction;
                    telemetria.EconomiaUSD = (decimal)(telemetria.TokensEstimadosEvitados / 1000.0) * _telemetryOptions.CostPer1KTokens;
                    
                    _ = _telemetryService.RegistrarInteracaoIaAsync(telemetria); // Fire and forget
                    
                    await _context.SaveChangesAsync();

                    var limiteRestante = _planoLimites.ObterLimiteTokensSemana(user);
                    return Ok(new
                    {
                        resultado = new { 
                            texto = searchResult.Resposta, 
                            promptTokens = 0, 
                            modelo = "Aprendizado-Local",
                            hitResult = searchResult.Resultado,
                            similarityScore = searchResult.SimilarityScore,
                            aprendizadoTipo = searchResult.Match.Tipo,
                            aprendizadoId = searchResult.Match.Id
                        },
                        planType = user.PlanType.ToString(),
                        tokensConsumidosSemana = user.TokensConsumidosSemana,
                        tokensSemanaMax = limiteRestante,
                        tokensRestantes = Math.Max(0, limiteRestante - user.TokensConsumidosSemana),
                        starkCoinBalance = user.StarkCoins,
                        adsEnabled = _planoLimites.ExibeAnuncios(user),
                        agendamentosMax = _planoLimites.ObterLimiteAgendamentos(user),
                        rate = 100
                    });
                }
                else
                {
                    // Se está no cache mas o usuário não pode "pagar", retorna erro de limite
                    return StatusCode(402, new { 
                        message = "Saldo insuficiente para processar comando. Adicione StarkCoins ou aguarde o reset semanal.", 
                        requiredCoins = 1 
                    });
                }
            }

            // 5. Chamada à IA Externa (Cache Miss)
            telemetria.ChamouIaExterna = true;
            var resultado = await _iaService.ProcessarMensagem(request.ContextoUser, request.ContextoIA, request.Texto, request.Estilo);
            if (resultado != null) resultado.HitResult = "CacheMiss";
            
            sw.Stop();
            telemetria.LatenciaMs = (int)sw.ElapsedMilliseconds;
            _ = _telemetryService.RegistrarInteracaoIaAsync(telemetria); // Fire and forget (Miss)

            if (resultado == null) 
            {
                return StatusCode(503, new { message = "IA temporariamente indisponível. Tente novamente em alguns minutos." });
            }

            // 6. Atualizar Estado da Conversa e Salvar Aprendizado (Novo Conhecimento)
            if (ehAutocontido)
            {
                var resumo = GerarContextoResumo(request.Texto);
                conversaContext.ContextoAtual = (resumo.Length > 6) ? resumo : string.Empty;
                conversaContext.LastUpdatedAt = DateTimeOffset.UtcNow;

                if (!string.IsNullOrWhiteSpace(resultado.Texto) && !string.IsNullOrWhiteSpace(textoNormalizado) && textoNormalizado.Length >= 2)
                {
                    bool ehPessoal = EhConteudoPessoal(textoNormalizado);
                    bool ehAmbiguo = TextHelper.EhAmbiguo(request.Texto);
                    var tipoFinal = (ehPessoal || ehAmbiguo) ? "Usuario" : "Global";
                    var respostaFinal = (tipoFinal == "Global") ? TextHelper.LimparGirias(resultado.Texto) : resultado.Texto;

                    // Captura o ID para associar variações
                    var aprendizadoId = Guid.NewGuid();
                    _context.Aprendizados.Add(new Aprendizado
                    {
                        Id = aprendizadoId,
                        Texto = textoNormalizado,
                        Resposta = respostaFinal,
                        Tipo = tipoFinal,
                        UserId = userId,
                        Contexto = null,
                        CreatedAt = DateTimeOffset.UtcNow
                    });

                    // Se for Global, gera variações de resposta usando IA
                    if (tipoFinal == "Global")
                    {
                        _logger.LogInformation("SuperIA: Gerando variações para aprendizado Global {Id}", aprendizadoId);

                        // 1. SEMPRE adiciona a resposta original como variação base
                        _context.AprendizadoRespostas.Add(new AprendizadoResposta
                        {
                            Id = Guid.NewGuid(),
                            AprendizadoId = aprendizadoId,
                            Texto = respostaFinal,
                            UsoCount = 0,
                            CreatedAt = DateTimeOffset.UtcNow
                        });

                        try
                        {
                            var variacoes = await _iaService.GerarVariacoesParaGlobal(request.Texto, respostaFinal);
                            _logger.LogInformation("SuperIA: IA retornou {Count} variações.", variacoes.Count);

                            if (variacoes != null && variacoes.Any())
                            {
                                int adicionadas = 0;
                                foreach (var v in variacoes)
                                {
                                    // Evitar duplicatas (caso a IA retorne a mesma frase)
                                    if (!v.Trim().Equals(respostaFinal.Trim(), StringComparison.OrdinalIgnoreCase))
                                    {
                                        _context.AprendizadoRespostas.Add(new AprendizadoResposta
                                        {
                                            Id = Guid.NewGuid(),
                                            AprendizadoId = aprendizadoId,
                                            Texto = v,
                                            UsoCount = 0,
                                            CreatedAt = DateTimeOffset.UtcNow
                                        });
                                        adicionadas++;
                                    }
                                }
                                _logger.LogInformation("SuperIA: {Count} variações novas adicionadas ao contexto.", adicionadas);
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Erro ao gerar variações para aprendizado global. AprendizadoId: {AprendizadoId}", aprendizadoId);
                            // Continua sem as variações se houver um erro na IA
                        }
                    }
                }
            }
            else if (!string.IsNullOrEmpty(contextoHerdado))
            {
                if (!string.IsNullOrWhiteSpace(resultado.Texto))
                {
                    _context.Aprendizados.Add(new Aprendizado
                    {
                        Id = Guid.NewGuid(),
                        Texto = textoNormalizado,
                        Resposta = resultado.Texto,
                        Tipo = "Contextual",
                        UserId = userId,
                        Contexto = contextoHerdado,
                        CreatedAt = DateTimeOffset.UtcNow
                    });
                }
            }

            var tokensUsados = Math.Max(0, resultado.PromptTokens) + Math.Max(0, resultado.CompletionTokens);
            var limite = _planoLimites.ObterLimiteTokensSemana(user);

            var consumo = _tokenUsage.ConsumeTokens(user, tokensUsados, request.UseStarkCoins);
            if (!consumo.Success)
            {
                return StatusCode(402, new { message = "Saldo insuficiente para tokens excedentes. Adicione StarkCoins ou aguarde o reset semanal.", requiredCoins = consumo.RequiredCoins });
            }

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
            try
            {
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                var errorMsg = ex.InnerException != null ? ex.InnerException.Message : ex.Message;
                _logger.LogError(ex, "Erro ao salvar aprendizado/histórico no SuperIA");
                return StatusCode(500, new { message = "Erro interno ao salvar dados.", error = errorMsg });
            }

            return Ok(new
            {
                resultado,
                planType = user.PlanType.ToString(),
                tokensConsumidosSemana = user.TokensConsumidosSemana,
                tokensSemanaMax = limite,
                tokensRestantes = Math.Max(0, limite - user.TokensConsumidosSemana),
                starkCoinBalance = user.StarkCoins,
                adsEnabled = _planoLimites.ExibeAnuncios(user),
                agendamentosMax = _planoLimites.ObterLimiteAgendamentos(user),
                    rate = 100
                });
            }
            catch (Exception ex)
            {
                var errorMsg = ex.InnerException != null ? ex.InnerException.Message : ex.Message;
                _logger.LogError(ex, "Erro CRITICO HANDLED em SuperIA");
                return StatusCode(500, new { message = "Erro interno no servidor.", error = errorMsg, stackTrace = ex.StackTrace });
            }
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
            .Select(u => new { u.Id, u.RemovalAds, u.PlanType })
                .FirstOrDefaultAsync(u => u.Id == userId);

            if (user == null)
                return NotFound("Usuário não encontrado.");

        var adsOn = user.PlanType != UserPlanType.Premium;

        return Ok(new
        {
            adsAtiv = adsOn ? "Desativado" : "Ativo",
            exibeAnuncios = adsOn
        });
        }

        [HttpGet("me")]
        public async Task<IActionResult> GetCurrentUser()
        {
            var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId);

            if (user == null) return NotFound();

            // Verificar se há assinatura Premium ativa e atualizar PlanType se necessário
            var hasActivePremium = await _context.Assinaturas
                .AnyAsync(a => a.UserId == userId && 
                              (a.Status == "ativa" || a.Status == "Ativa") && 
                              a.Valor == 10 && 
                              (!a.ExpiraEm.HasValue || a.ExpiraEm.Value > DateTimeOffset.UtcNow));
            
            if (hasActivePremium && user.PlanType != UserPlanType.Premium)
            {
                _logger.LogInformation("🔄 [GetMe] Atualizando PlanType para Premium - UserId: {UserId}", userId);
                user.PlanType = UserPlanType.Premium;
                user.RemovalAds = "Ativo";
                if (user.Role == "UserNivel1")
                {
                    user.Role = "UserNivel2";
                }
                await _context.SaveChangesAsync();
            }
            else if (!hasActivePremium && user.PlanType == UserPlanType.Premium)
            {
                // Se não há assinatura Premium ativa mas PlanType está como Premium, verificar se deve rebaixar
                var hasAnyActivePremium = await _context.Assinaturas
                    .AnyAsync(a => a.UserId == userId && 
                                  (a.Status == "ativa" || a.Status == "Ativa") && 
                                  a.Valor == 10);
                
                if (!hasAnyActivePremium)
                {
                    _logger.LogInformation("🔄 [GetMe] Rebaixando PlanType para Free - UserId: {UserId}", userId);
                    user.PlanType = UserPlanType.Free;
                    user.RemovalAds = "Desativado";
                }
            }

        var limite = _planoLimites.ObterLimiteTokensSemana(user);
        var agendamentosRestantes = _planoLimites.CalcularAgendamentosRestantes(
            user,
            await _context.Agendamentos.CountAsync(a => a.UserId == userId));

        _logger.LogInformation("🔍 [GetMe] UserId: {UserId}, PlanType: {PlanType}, StarkCoinBalance: {Balance}, TokensConsumidosSemana: {Consumed}, Limite calculado: {Limit}", 
            userId, user.PlanType, user.StarkCoins, user.TokensConsumidosSemana, limite);

        var economy = new StarkAid.Api.DTOs.EconomicPayload(
            user.PlanType.ToString(),
            user.StarkCoins,
            user.TokensConsumidosSemana,
            limite,
            Math.Max(0, limite - user.TokensConsumidosSemana),
            _planoLimites.ExibeAnuncios(user),
            _planoLimites.ObterLimiteAgendamentos(user),
            agendamentosRestantes,
            100
        );
        
        _logger.LogInformation("🔍 [GetMe] Economy payload criado: planType={PlanType}, StarkCoinBalance={Balance}, tokensConsumidosSemana={Consumed}, tokensSemanaMax={Max}, tokensRestantes={Restantes}", 
            economy.planType, economy.StarkCoinBalance, economy.tokensConsumidosSemana, economy.tokensSemanaMax, economy.tokensRestantes);

        return Ok(new
        {
            id = user.Id,
            name = user.Name,
            email = user.Email,
            apiKey = user.ApiKey,
            role = user.Role,
            estado = user.Estado,
            cidade = user.Cidade,
            bairro = user.Bairro,
            removalAds = user.RemovalAds,
            createdAt = user.CreatedAt,
            isActive = user.IsActive,
            economy
        });
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
                var mqttService = HttpContext.RequestServices.GetService<Services.V1.Devices.IMqttClientService>();
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

            if (request.Coins <= 0)
                return BadRequest("Quantidade de StarkCoins deve ser maior que zero.");

            decimal valorBrl = request.Coins switch
            {
                5 => 4.90m,
                15 => 9.90m,
                50 => 19.90m,
                120 => 39.90m,
                _ => -1m
            };

            if (valorBrl < 0)
                return BadRequest("Pacote inválido. Use 5, 15, 50 ou 120 StarkCoins.");

            var stripeService = HttpContext.RequestServices.GetService<StripeService>();
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
                valorBrl,
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
                Valor = request.Coins, // guarda quantidade de coins; conversão para BRL feita acima
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
                _logger.LogInformation("📱 [SetUserOnline] Endpoint chamado - Origem: {Origem}, SessionName: {SessionName}", 
                    request?.Origem, request?.SessionName ?? "null");
                
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
                    _logger.LogInformation("🔄 [SetUserOnline] Atualizando sessão existente - SessionId: {SessionId}, SessionName atual: '{CurrentName}'", 
                        existingSession.Id, existingSession.SessionName);
                    // Atualizar sessão existente
                    existingSession.LastActivityAt = DateTime.UtcNow;
                    existingSession.Token = token;
                    // SEMPRE atualizar SessionName se fornecido (mesmo que não seja vazio)
                    if (!string.IsNullOrWhiteSpace(request.SessionName))
                    {
                        _logger.LogInformation("📝 [SetUserOnline] Atualizando SessionName de '{OldName}' para '{NewName}'", 
                            existingSession.SessionName, request.SessionName);
                        existingSession.SessionName = request.SessionName;
                    }
                    else
                    {
                        _logger.LogInformation("⚠️ [SetUserOnline] SessionName não fornecido ou vazio na requisição");
                    }
                }
                else
                {
                    _logger.LogInformation("✨ [SetUserOnline] Criando nova sessão - UserId: {UserId}, Origem: {Origem}", userId, origem);
                    // Criar nova sessão
                    var sessionName = !string.IsNullOrWhiteSpace(request.SessionName) 
                        ? request.SessionName 
                        : $"{user.Name} - {origem}";
                    
                    var session = new UserSession
                    {
                        Id = 0, // Será gerado pelo banco
                        UserId = userId,
                        SessionName = sessionName,
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

        [HttpPost("activity/update")]
        public async Task<IActionResult> UpdateUserActivity([FromBody] UpdateUltimoComandoRequest request)
        {
            try
            {
                _logger.LogInformation("📝 [UpdateUserActivity] Endpoint chamado");
                
                var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
                _logger.LogInformation("👤 [UpdateUserActivity] UserId: {UserId}", userId);
                _logger.LogInformation("📋 [UpdateUserActivity] Request - ESP: {Esp}, Ewelink: {Ewelink}, StarkSwitch: {StarkSwitch}, Social: {Social}, IA: {IA}", 
                    request?.UltimoComandoEsp, request?.UltimoComandoEwelink, request?.UltimoComandoStarkSwitch, 
                    request?.UltimoComandoSocial, request?.UltimoComandoIA);
                
                // Buscar ou criar atividade do usuário
                var activity = await _context.UserActivities
                    .FirstOrDefaultAsync(a => a.UserId == userId && a.Origem == "soft");
                
                if (activity == null)
                {
                    _logger.LogInformation("✨ [UpdateUserActivity] Criando nova atividade para usuário {UserId}", userId);
                    activity = new UserActivity
                    {
                        UserId = userId,
                        Origem = "soft"
                    };
                    _context.UserActivities.Add(activity);
                }
                else
                {
                    _logger.LogInformation("🔄 [UpdateUserActivity] Atualizando atividade existente (Id: {ActivityId})", activity.Id);
                }

                // Atualizar campos se fornecidos
                if (request?.UltimoComandoEsp != null)
                {
                    activity.UltimoComandoEsp = request.UltimoComandoEsp;
                    _logger.LogInformation("✅ [UpdateUserActivity] UltimoComandoEsp atualizado: {Comando}", request.UltimoComandoEsp);
                }
                if (request?.UltimoComandoEwelink != null)
                {
                    activity.UltimoComandoEwelink = request.UltimoComandoEwelink;
                    _logger.LogInformation("✅ [UpdateUserActivity] UltimoComandoEwelink atualizado: {Comando}", request.UltimoComandoEwelink);
                }
                if (request?.UltimoComandoStarkSwitch != null)
                {
                    activity.UltimoComandoStarkSwitch = request.UltimoComandoStarkSwitch;
                    _logger.LogInformation("✅ [UpdateUserActivity] UltimoComandoStarkSwitch atualizado: {Comando}", request.UltimoComandoStarkSwitch);
                }
                if (request?.UltimoComandoSocial != null)
                {
                    activity.UltimoComandoSocial = request.UltimoComandoSocial;
                    _logger.LogInformation("✅ [UpdateUserActivity] UltimoComandoSocial atualizado: {Comando}", request.UltimoComandoSocial);
                }
                if (request?.UltimaRespostaSocial != null)
                {
                    activity.UltimaRespostaSocial = request.UltimaRespostaSocial;
                    _logger.LogInformation("✅ [UpdateUserActivity] UltimaRespostaSocial atualizada: {Resposta}", request.UltimaRespostaSocial);
                }
                if (request?.UltimoComandoIA != null)
                {
                    activity.UltimoComandoIA = request.UltimoComandoIA;
                    _logger.LogInformation("✅ [UpdateUserActivity] UltimoComandoIA atualizado: {Comando}", request.UltimoComandoIA);
                }
                if (request?.UltimaRespostaIA != null)
                {
                    activity.UltimaRespostaIA = request.UltimaRespostaIA;
                    _logger.LogInformation("✅ [UpdateUserActivity] UltimaRespostaIA atualizada: {Resposta}", request.UltimaRespostaIA);
                }

                activity.LastUpdatedAt = DateTimeOffset.UtcNow;
                await _context.SaveChangesAsync();
                
                _logger.LogInformation("✅ [UpdateUserActivity] Atividade salva com sucesso. LastUpdatedAt: {LastUpdated}", activity.LastUpdatedAt);

                return Ok(new { message = "Atividade atualizada com sucesso." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ [UpdateUserActivity] Erro ao atualizar atividade");
                return StatusCode(500, new { message = "Erro ao atualizar atividade.", error = ex.Message });
            }
        }

        [HttpPost("activity/app/update")]
        public async Task<IActionResult> UpdateUserActivityApp([FromBody] UpdateUltimoComandoRequest request)
        {
            var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
            
            // Buscar ou criar atividade do usuário
            var activity = await _context.UserActivities
                .FirstOrDefaultAsync(a => a.UserId == userId && a.Origem == "app");
            
            if (activity == null)
            {
                activity = new UserActivity
                {
                    UserId = userId,
                    Origem = "app"
                };
                _context.UserActivities.Add(activity);
            }

            // Atualizar campos se fornecidos
            if (request.UltimoComandoEsp != null)
                activity.UltimoComandoEsp = request.UltimoComandoEsp;
            if (request.UltimoComandoEwelink != null)
                activity.UltimoComandoEwelink = request.UltimoComandoEwelink;
            if (request.UltimoComandoStarkSwitch != null)
                activity.UltimoComandoStarkSwitch = request.UltimoComandoStarkSwitch;
            if (request.UltimoComandoSocial != null)
                activity.UltimoComandoSocial = request.UltimoComandoSocial;
            if (request.UltimaRespostaSocial != null)
                activity.UltimaRespostaSocial = request.UltimaRespostaSocial;
            if (request.UltimoComandoIA != null)
                activity.UltimoComandoIA = request.UltimoComandoIA;
            if (request.UltimaRespostaIA != null)
                activity.UltimaRespostaIA = request.UltimaRespostaIA;

            activity.LastUpdatedAt = DateTimeOffset.UtcNow;
            await _context.SaveChangesAsync();

            return Ok(new { message = "Atividade atualizada com sucesso." });
        }

        [HttpPost("logs-falhas/soft")]
        public async Task<IActionResult> AddLogFalhaSoft([FromBody] LogFalhaSoftRequest request)
        {
            var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
            
            var logFalha = new LogFalhaSoft
            {
                UserId = userId,
                TipoFalha = request.TipoFalha,
                Descricao = request.Descricao,
                ComandoTentado = request.ComandoTentado,
                DispositivoNome = request.DispositivoNome,
                ErroDetalhado = request.ErroDetalhado,
                CreatedAt = DateTimeOffset.UtcNow
            };

            _context.LogsFalhasSoft.Add(logFalha);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Log de falha registrado com sucesso." });
        }

        private bool EhAutocontido(string input)
        {
            if (string.IsNullOrWhiteSpace(input)) return false;
            // Um comando é autocontido se NÃO for um follow-up e tiver tamanho mínimo.
            return !TextHelper.EhFollowUp(input) && input.Trim().Length >= 5;
        }

        private bool EhFollowUp(string input)
        {
            return TextHelper.EhFollowUp(input);
        }

        private bool EhConteudoPessoal(string input)
        {
            return TextHelper.EhConteudoPessoal(input);
        }

        private string GerarContextoResumo(string input)
        {
            // 1. Normalização básica (lowercase, acentos, pontuação)
            var texto = TextHelper.NormalizarTexto(input);
            
            // 2. Normalização forte para contexto (remover verbos de ação comuns e stopwords que não definem o tópico central)
            // Utiliza Regex \b para garantir que remove apenas palavras inteiras
            texto = Regex.Replace(texto, @"\b(posso|pode|podemos|usar|uso|utilizar|lavar|limpar|fazer|como|onde|quando|quem|qual|o que|que|tem|existe|ha)\b", "", RegexOptions.IgnoreCase);
            
            // 3. Limpeza final de espaços
            return Regex.Replace(texto, @"\s+", " ").Trim();
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
        public int Coins { get; set; }
    }

    public class SetUserOnlineRequest
    {
        public string Origem { get; set; } = string.Empty; // web, soft, app
        public string? SessionName { get; set; } // Nome do form ou activity (opcional)
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
