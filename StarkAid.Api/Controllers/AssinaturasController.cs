using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using StarkAid.Api.Data;
using StarkAid.Api.Entities;
using StarkAid.Api.Options;
using StarkAid.Api.Services.Assinatura;
using Stripe;
using System.Security.Claims;

namespace StarkAid.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AssinaturasController : ControllerBase
    {
        private readonly AppDbContext _db;
        private readonly StripeService _stripeService;
        private readonly StripeSettings _stripeSettings;
        private readonly ILogger<AssinaturasController> _logger;

        public AssinaturasController(
            AppDbContext db,
            StripeService stripeService,
            IOptions<StripeSettings> stripeOptions,
            ILogger<AssinaturasController> logger)
        {
            _db = db;
            _stripeService = stripeService;
            _stripeSettings = stripeOptions.Value;
            _logger = logger;
        }

        [HttpPost("create/{nivel:int}")]
        public async Task<IActionResult> CreateSubscription([FromRoute] int nivel, [FromQuery] Guid userId)
        {
            var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == userId);
            if (user == null)
                return NotFound("Usuário não encontrado.");

            _logger.LogInformation("🔍 Verificando assinaturas ativas para o usuário {UserId}", user.Id);

            // ⚠️ Lógica de múltiplas assinaturas:
            // - Usuário pode ter Remove Ads (nível 2, R$ 10) + outro plano ao mesmo tempo
            // - Quando cria Remove Ads: mantém outros planos ativos (mas só permite um Remove Ads)
            // - Quando cria plano de StarkCoins (níveis 3-7): cancela outros planos de StarkCoins, mas mantém Remove Ads

            var assinaturasAtivas = await _db.Assinaturas
                .Where(a => a.UserId == user.Id && a.Status == "ativa")
                .ToListAsync();

            if (nivel == 2)
            {
                // Criando Remove Ads: verificar se já existe (permitir apenas um Remove Ads)
                var removeAdsAtiva = assinaturasAtivas.FirstOrDefault(a => a.Valor == 10);
                if (removeAdsAtiva != null)
                {
                    return BadRequest("Você já possui um plano Remove Ads ativo. Cancele o plano atual antes de criar um novo.");
                }
                // Remove Ads pode coexistir com outros planos, então não cancela nada
                _logger.LogInformation("✅ Criando Remove Ads - outros planos serão mantidos");
            }
            else
            {
                // Criando plano de StarkCoins (níveis 3-7): cancelar outros planos de StarkCoins, mas manter Remove Ads (nível 2)
            foreach (var assinaturaAntiga in assinaturasAtivas)
                {
                    // Manter Remove Ads (nível 2, valor 10) sempre - pode coexistir
                    if (assinaturaAntiga.Valor == 10m)
                    {
                        _logger.LogInformation("⏩ Mantendo assinatura Remove Ads (nível 2) ativa - pode coexistir com outros planos.");
                    continue;
                }

                    // Cancelar outros planos de StarkCoins (qualquer plano que não seja nível 2)
                try
                {
                    if (!string.IsNullOrEmpty(assinaturaAntiga.StripeSubscriptionId))
                    {
                        await _stripeService.CancelSubscriptionAsync(assinaturaAntiga.StripeSubscriptionId);
                            _logger.LogInformation("🛑 Assinatura anterior {SubId} (valor {Valor}) cancelada no Stripe - substituída por nível {Nivel}.", 
                                assinaturaAntiga.StripeSubscriptionId, assinaturaAntiga.Valor, nivel);
                    }

                    assinaturaAntiga.Status = "cancelada";
                    assinaturaAntiga.CanceladaEm = DateTimeOffset.UtcNow;
                    _db.Assinaturas.Update(assinaturaAntiga);
                        _logger.LogInformation("✅ Assinatura {Id} (valor {Valor}) cancelada localmente.", assinaturaAntiga.Id, assinaturaAntiga.Valor);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Erro ao cancelar assinatura anterior {Id}", assinaturaAntiga.Id);
                    }
                }
            }

            await _db.SaveChangesAsync();

            string priceId = nivel switch
            {
                2 => _stripeSettings.PriceIdNivel2,
                3 => _stripeSettings.PriceIdNivel3,
                4 => _stripeSettings.PriceIdNivel4,
                5 => _stripeSettings.PriceIdNivel5,
                6 => _stripeSettings.PriceIdNivel6,
                7 => _stripeSettings.PriceIdNivel7,
                _ => throw new ArgumentException("Nível de assinatura inválido.")
            };

            try
            {
            // 🔥 CORREÇÃO: Sem desestruturação - usando tupla diretamente
            var checkoutResult = await _stripeService.CreateCheckoutSessionAsync(
                user,
                priceId,
                successUrl: $"{_stripeSettings.AppDeepLink}?success=true&nivel={nivel}",
                cancelUrl: $"{_stripeSettings.AppDeepLink}?success=false"
            );

            var session = checkoutResult.session;
            var customer = checkoutResult.customer;

            var valor = nivel switch
            {
                2 => 10m,
                3 => 5m,
                4 => 15m,
                5 => 25m,
                6 => 50m,
                7 => 100m,
                _ => 0m
            };

            var assinatura = new Assinatura
            {
                Id = Guid.NewGuid(),
                UserId = user.Id,
                StripeCustomerId = customer.Id,
                Valor = valor,
                Status = "pendente",
                DataCriacao = DateTimeOffset.UtcNow,
                StripePriceId = priceId
            };

            _db.Assinaturas.Add(assinatura);
            await _db.SaveChangesAsync();

            _logger.LogInformation("🧾 Nova assinatura criada (Id={Id}, Valor={Valor}, Nivel={Nivel})", assinatura.Id, assinatura.Valor, nivel);

            return Ok(new
            {
                checkoutUrl = session.Url,
                sessionId = session.Id,
                customerId = customer.Id
            });
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogError(ex, "Erro ao criar checkout no Stripe para nível {Nivel}, Price ID: {PriceId}", nivel, priceId);
                
                // Se tiver inner exception do Stripe, usar mensagem mais específica
                if (ex.InnerException is StripeException stripeEx)
                {
                    string errorMessage;
                    if (stripeEx.Message.Contains("No such price"))
                    {
                        errorMessage = $"O Price ID '{priceId}' não existe no Stripe. Verifique:\n" +
                                       "1. Se o Price ID está correto no appsettings.json\n" +
                                       "2. Se o Price ID foi criado no mesmo ambiente (Test/Production) das suas chaves da API\n" +
                                       "3. Se o Price ID não foi deletado no Stripe Dashboard";
                    }
                    else if (stripeEx.Message.Contains("not active") || stripeEx.Message.Contains("not available"))
                    {
                        errorMessage = "O produto deste plano não está ativo no Stripe. Por favor, ative o produto no Stripe Dashboard.";
                    }
                    else
                    {
                        errorMessage = "Erro ao processar o pagamento. Por favor, tente novamente.";
                    }

                    return StatusCode(500, new { 
                        error = errorMessage, 
                        priceId = priceId,
                        nivel = nivel,
                        details = stripeEx.Message 
                    });
                }
                
                // InvalidOperationException sem inner exception (ex: da validação do Price ID)
                return StatusCode(500, new { 
                    error = ex.Message,
                    priceId = priceId,
                    nivel = nivel
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro inesperado ao criar checkout para nível {Nivel}", nivel);
                return StatusCode(500, new { error = "Erro ao criar checkout. Por favor, tente novamente.", details = ex.Message });
            }
        }

        [Authorize]
        [HttpPost("checkout")]
        public async Task<IActionResult> Checkout([FromBody] CheckoutRequest request)
        {
            var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
            var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == userId);
            if (user == null)
                return Unauthorized("Usuário não encontrado.");

            var nivel = request.Nivel;
            if (nivel < 2 || nivel > 7)
                return BadRequest("Nível de assinatura inválido. Deve ser entre 2 e 7.");

            _logger.LogInformation("🔍 Criando checkout para usuário {UserId}, nível {Nivel}", userId, nivel);

            // ⚠️ Lógica de múltiplas assinaturas:
            // - Usuário pode ter Remove Ads (nível 2, R$ 10) + outro plano ao mesmo tempo
            // - Quando cria Remove Ads: mantém outros planos ativos
            // - Quando cria plano de StarkCoins (níveis 3-7): cancela outros planos de StarkCoins, mas mantém Remove Ads
            // - Remove Ads nunca é cancelado automaticamente (exceto se o próprio usuário cancelar)
            
            var assinaturasAtivas = await _db.Assinaturas
                .Where(a => a.UserId == user.Id && a.Status == "ativa")
                .ToListAsync();

            if (nivel == 2)
            {
                // Criando Remove Ads: verificar se já existe (permitir apenas um Remove Ads)
                var removeAdsAtiva = assinaturasAtivas.FirstOrDefault(a => a.Valor == 10);
                if (removeAdsAtiva != null)
                {
                    return BadRequest("Você já possui um plano Remove Ads ativo. Cancele o plano atual antes de criar um novo.");
                }
                // Remove Ads pode coexistir com outros planos, então não cancela nada
                _logger.LogInformation("✅ Criando Remove Ads - outros planos serão mantidos");
            }
            else
            {
                // Criando plano de StarkCoins (níveis 3-7): cancelar outros planos de StarkCoins, mas manter Remove Ads (nível 2)
                foreach (var assinaturaAntiga in assinaturasAtivas)
                {
                    // Manter Remove Ads (nível 2, valor 10) sempre - pode coexistir
                    if (assinaturaAntiga.Valor == 10m)
                    {
                        _logger.LogInformation("⏩ Mantendo assinatura Remove Ads (nível 2) ativa - pode coexistir com outros planos.");
                        continue;
                    }

                    // Cancelar outros planos de StarkCoins (qualquer plano que não seja nível 2)
                    try
                    {
                        if (!string.IsNullOrEmpty(assinaturaAntiga.StripeSubscriptionId))
                        {
                            await _stripeService.CancelSubscriptionAsync(assinaturaAntiga.StripeSubscriptionId);
                            _logger.LogInformation("🛑 Assinatura anterior {SubId} (valor {Valor}) cancelada no Stripe - substituída por nível {Nivel}.", 
                                assinaturaAntiga.StripeSubscriptionId, assinaturaAntiga.Valor, nivel);
                        }

                        assinaturaAntiga.Status = "cancelada";
                        assinaturaAntiga.CanceladaEm = DateTimeOffset.UtcNow;
                        _db.Assinaturas.Update(assinaturaAntiga);
                        _logger.LogInformation("✅ Assinatura {Id} (valor {Valor}) cancelada localmente.", assinaturaAntiga.Id, assinaturaAntiga.Valor);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Erro ao cancelar assinatura anterior {Id}", assinaturaAntiga.Id);
                    }
                }
            }

            await _db.SaveChangesAsync();

            string priceId = nivel switch
            {
                2 => _stripeSettings.PriceIdNivel2,
                3 => _stripeSettings.PriceIdNivel3,
                4 => _stripeSettings.PriceIdNivel4,
                5 => _stripeSettings.PriceIdNivel5,
                6 => _stripeSettings.PriceIdNivel6,
                7 => _stripeSettings.PriceIdNivel7,
                _ => throw new ArgumentException("Nível de assinatura inválido.")
            };

            // Log do Price ID que será usado
            _logger.LogInformation("🔑 Usando Price ID: {PriceId} para nível {Nivel}", priceId, nivel);

            if (string.IsNullOrEmpty(priceId))
            {
                _logger.LogError("❌ Price ID para nível {Nivel} não configurado!", nivel);
                return StatusCode(500, new { error = $"Price ID para nível {nivel} não está configurado. Verifique o appsettings.json." });
            }

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
            
            string successUrl, cancelUrl;
            if (isFromSoftware)
            {
                // Quando chamado do Windows Forms, usar SoftwareDeepLink
                var softwareDeepLink = _stripeSettings.SoftwareDeepLink ?? "http://localhost:8765/payment";
                successUrl = $"{softwareDeepLink}?plano=success&nivel={nivel}";
                cancelUrl = $"{softwareDeepLink}?plano=cancel";
                _logger.LogInformation("💻 Usando deep link para software: {SuccessUrl}", successUrl);
            }
            else if (isFromApp)
            {
                // Quando chamado do app, usar deep link
                var appDeepLink = _stripeSettings.AppDeepLink ?? "starkaid://payment";
                successUrl = $"{appDeepLink}?success=true&nivel={nivel}";
                cancelUrl = $"{appDeepLink}?success=false";
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

            try
            {
                var checkoutResult = await _stripeService.CreateCheckoutSessionAsync(
                    user,
                    priceId,
                    successUrl: successUrl,
                    cancelUrl: cancelUrl
                );

                var session = checkoutResult.session;
                var customer = checkoutResult.customer;

                var valor = nivel switch
                {
                    2 => 10m,
                    3 => 5m,
                    4 => 15m,
                    5 => 25m,
                    6 => 50m,
                    7 => 100m,
                    _ => 0m
                };

                var assinatura = new Assinatura
                {
                    Id = Guid.NewGuid(),
                    UserId = user.Id,
                    StripeCustomerId = customer.Id,
                    Valor = valor,
                    Status = "pendente",
                    DataCriacao = DateTimeOffset.UtcNow,
                    StripePriceId = priceId
                };

                _db.Assinaturas.Add(assinatura);
                await _db.SaveChangesAsync();

                _logger.LogInformation("🧾 Nova assinatura criada via checkout (Id={Id}, Valor={Valor}, Nivel={Nivel})", assinatura.Id, assinatura.Valor, nivel);

                return Ok(new
                {
                    checkoutUrl = session.Url,
                    sessionId = session.Id
                });
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogError(ex, "Erro ao criar checkout no Stripe para nível {Nivel}, Price ID: {PriceId}", nivel, priceId);
                
                // Se tiver inner exception do Stripe, usar mensagem mais específica
                if (ex.InnerException is StripeException stripeEx)
                {
                    string errorMessage;
                    if (stripeEx.Message.Contains("No such price"))
                    {
                        errorMessage = $"O Price ID '{priceId}' não existe no Stripe.\n" +
                                       "Verifique:\n" +
                                       "1. Se o Price ID está correto no appsettings.json\n" +
                                       "2. Se está no ambiente correto (Test mode usa chaves sk_test_ e pk_test_, Live mode usa sk_live_ e pk_live_)\n" +
                                       "3. Se o produto/preço foi criado no mesmo ambiente das suas chaves de API";
                    }
                    else if (stripeEx.Message.Contains("not active") || stripeEx.Message.Contains("not available"))
                    {
                        errorMessage = "O produto deste plano não está ativo no Stripe. Por favor, ative o produto no Stripe Dashboard.";
                    }
                    else
                    {
                        errorMessage = "Erro ao processar o pagamento. Por favor, tente novamente.";
                    }

                    return StatusCode(500, new { 
                        error = errorMessage, 
                        priceId = priceId,
                        nivel = nivel,
                        details = stripeEx.Message 
                    });
                }
                
                // InvalidOperationException sem inner exception (ex: da validação do Price ID)
                return StatusCode(500, new { 
                    error = ex.Message,
                    priceId = priceId,
                    nivel = nivel
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro inesperado ao criar checkout para nível {Nivel}", nivel);
                return StatusCode(500, new { error = "Erro ao criar checkout. Por favor, tente novamente.", details = ex.Message });
            }
        }

        [Authorize]
        [HttpPost("cancelar/{assinaturaId}")]
        public async Task<IActionResult> CancelarAssinatura([FromRoute] Guid assinaturaId)
        {
            var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
            _logger.LogInformation("🔍 Cancelando assinatura {AssinaturaId} para usuário {UserId}", assinaturaId, userId);

            var assinatura = await _db.Assinaturas
                .FirstOrDefaultAsync(a => a.Id == assinaturaId && a.UserId == userId);

            if (assinatura == null)
                return NotFound("Assinatura não encontrada ou não pertence a este usuário.");

            if (assinatura.Status != "ativa" && assinatura.Status != "Ativa")
                return BadRequest("Esta assinatura já está cancelada ou não está ativa.");

            try
            {
                // Cancelar no Stripe se tiver subscription ID
                if (!string.IsNullOrEmpty(assinatura.StripeSubscriptionId))
                {
                    var stripeResult = await _stripeService.CancelSubscriptionAsync(assinatura.StripeSubscriptionId);
                    _logger.LogInformation("🛑 Assinatura {AssinaturaId} cancelada no Stripe: {Status}", assinaturaId, stripeResult?.Status ?? "Erro");
                }

                // Atualizar status local
                assinatura.Status = "cancelada";
                assinatura.CanceladaEm = DateTimeOffset.UtcNow;
                _db.Assinaturas.Update(assinatura);

                // ⚠️ Apenas rebaixar Role se for o plano Remove Ads (valor 10)
                // Planos de StarkCoins (níveis 3-7) não alteram o Role ao cancelar
                var user = await _db.Users.FindAsync(userId);
                if (user != null && assinatura.Valor == 10m)
                {
                    user.Role = "UserNivel1";
                    user.RemovalAds = "Desativado";
                    _logger.LogInformation("⬇ Usuário {UserId} rebaixado para UserNivel1 (plano Remove Ads cancelado)", userId);
                    _db.Users.Update(user);
                }

                await _db.SaveChangesAsync();

                return Ok(new
                {
                    Message = "Assinatura cancelada com sucesso",
                    AssinaturaId = assinatura.Id,
                    Status = "cancelada"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao cancelar assinatura {AssinaturaId}", assinaturaId);
                return StatusCode(500, new { error = "Erro ao cancelar assinatura", message = ex.Message });
            }
        }

        [Authorize]
        [HttpPost("cancelar")]
        public async Task<IActionResult> Cancelar()
        {
            var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
            _logger.LogInformation($"usuário id encontrado: {userId}");

            var assinaturasAtivas = await _db.Assinaturas
                .Where(a => a.UserId == userId && a.Status == "ativa")
                .ToListAsync();

            if (!assinaturasAtivas.Any())
                return BadRequest("Nenhuma assinatura ativa encontrada.");

            var results = new List<SubscriptionCancelResult>();

            foreach (var assinatura in assinaturasAtivas)
            {
                var stripeResult = await _stripeService.CancelSubscriptionAsync(assinatura.StripeSubscriptionId);

                assinatura.Status = "cancelada";
                assinatura.CanceladaEm = DateTimeOffset.UtcNow;
                _db.Assinaturas.Update(assinatura);

                results.Add(new SubscriptionCancelResult(
                    assinatura.Id,
                    stripeResult != null ? "Cancelada" : "Falha no cancelamento",
                    stripeResult?.Status ?? "Erro"
                ));
            }

            // ⚠️ Apenas rebaixar Role se alguma assinatura cancelada for o plano Remove Ads (valor 10)
            // Planos de StarkCoins (níveis 3-7) não alteram o Role ao cancelar
            var user = await _db.Users.FindAsync(userId);
            if (user != null)
            {
                var cancelouRemoveAds = assinaturasAtivas.Any(a => a.Valor == 10);
                if (cancelouRemoveAds)
            {
                user.Role = "UserNivel1";
                user.RemovalAds = "Desativado";
                    _logger.LogInformation("⬇ Usuário {UserId} rebaixado para UserNivel1 (plano Remove Ads cancelado)", userId);
                }
                // Se cancelou apenas planos de StarkCoins, não altera Role nem RemovalAds
                _db.Users.Update(user);
            }

            await _db.SaveChangesAsync();

            return Ok(new
            {
                Message = "Solicitação de cancelamento processada",
                Results = results
            });
        }

        [HttpPost("avulso/{valor:decimal}")]
        public async Task<IActionResult> CriarPagamentoAvulso([FromRoute] decimal valor, [FromQuery] Guid userId)
        {
            if (valor <= 0)
                return BadRequest("O valor deve ser maior que zero.");

            var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == userId);
            if (user == null) return NotFound("Usuário não encontrado.");

            // 🔥 CORREÇÃO: Sem desestruturação
            var paymentResult = await _stripeService.CreateOneTimePaymentSessionAsync(
                user,
                valor,
                successUrl: $"{_stripeSettings.AppDeepLink}?pagamento=sucesso&valor={valor}",
                cancelUrl: $"{_stripeSettings.AppDeepLink}?pagamento=cancelado"
            );

            var session = paymentResult.session;
            var customer = paymentResult.customer;

            var pagamento = new PagamentoAvulso
            {
                Id = Guid.NewGuid(),
                UserId = user.Id,
                Valor = valor,
                Status = "pendente",
                StripeSessionId = session.Id,
                StripeCustomerId = customer.Id,
                DataCriacao = DateTimeOffset.UtcNow
            };

            _db.Add(pagamento);
            await _db.SaveChangesAsync();

            return Ok(new
            {
                checkoutUrl = session.Url,
                sessionId = session.Id
            });
        }

        [Authorize]
        [HttpGet("test-config")]
        public IActionResult TestConfig()
        {
            // Endpoint para testar configurações do Stripe (apenas desenvolvimento)
            return Ok(new
            {
                secretKeyPrefix = _stripeSettings.SecretKey?.Substring(0, Math.Min(20, _stripeSettings.SecretKey?.Length ?? 0)) + "...",
                publishableKeyPrefix = _stripeSettings.PublishableKey?.Substring(0, Math.Min(20, _stripeSettings.PublishableKey?.Length ?? 0)) + "...",
                isTestMode = _stripeSettings.SecretKey?.StartsWith("sk_test_") ?? false,
                priceIds = new
                {
                    nivel2 = _stripeSettings.PriceIdNivel2,
                    nivel3 = _stripeSettings.PriceIdNivel3,
                    nivel4 = _stripeSettings.PriceIdNivel4,
                    nivel5 = _stripeSettings.PriceIdNivel5,
                    nivel6 = _stripeSettings.PriceIdNivel6,
                    nivel7 = _stripeSettings.PriceIdNivel7
                }
            });
        }

        [Authorize]
        [HttpGet("status")]
        public async Task<IActionResult> Status()
        {
            var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
            var assinatura = await _db.Assinaturas
                .OrderByDescending(a => a.DataCriacao)
                .FirstOrDefaultAsync(a => a.UserId == userId);

            if (assinatura == null)
                return NotFound("Nenhuma assinatura encontrada.");

            return Ok(new
            {
                assinatura.Status,
                assinatura.IniciadaEm,
                assinatura.CanceladaEm,
                assinatura.ExpiraEm,
                assinatura.PagamentoConfirmadoEm,
                assinatura.StripeCustomerId,
                assinatura.StripeSubscriptionId,
                assinatura.StripePriceId,
                assinatura.Valor
            });
        }

        [Authorize]
        [HttpGet("ativas")]
        public async Task<IActionResult> ListarAtivas()
        {
            try
            {
                var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
                var now = DateTimeOffset.UtcNow;

                // Buscar todas as assinaturas ativas do usuário
                // Verificar diferentes variações de status: "ativa", "Ativa", etc.
                var assinaturasAtivas = await _db.Assinaturas
                    .Where(a => a.UserId == userId && 
                               (a.Status == "ativa" || a.Status == "Ativa" || a.Status == "ATIVA") &&
                               (!a.ExpiraEm.HasValue || a.ExpiraEm.Value > now))
                    .OrderByDescending(a => a.DataCriacao)
                    .ToListAsync();

                _logger.LogInformation("🔍 Encontradas {Count} assinaturas ativas para usuário {UserId}", assinaturasAtivas.Count, userId);

                // Mapear para o formato de resposta (fazendo em memória para evitar problemas com switch em expressões LINQ)
                var resultado = assinaturasAtivas.Select(a =>
                {
                    int nivel = 0;
                    string nomePlano = "Plano Desconhecido";

                    if (a.Valor == 10m)
                    {
                        nivel = 2;
                        nomePlano = "Nível 2 - Remove Ads";
                    }
                    else if (a.Valor == 5m)
                    {
                        nivel = 3;
                        nomePlano = "Nível 3 - 5 StarkCoins/mês";
                    }
                    else if (a.Valor == 15m)
                    {
                        nivel = 4;
                        nomePlano = "Nível 4 - 15 StarkCoins/mês";
                    }
                    else if (a.Valor == 25m)
                    {
                        nivel = 5;
                        nomePlano = "Nível 5 - 25 StarkCoins/mês";
                    }
                    else if (a.Valor == 50m)
                    {
                        nivel = 6;
                        nomePlano = "Nível 6 - 50 StarkCoins/mês";
                    }
                    else if (a.Valor == 100m)
                    {
                        nivel = 7;
                        nomePlano = "Nível 7 - 100 StarkCoins/mês";
                    }

                    return new
                    {
                        a.Id,
                        a.Valor,
                        Nivel = nivel,
                        NomePlano = nomePlano,
                        a.Status,
                        a.IniciadaEm,
                        a.ExpiraEm,
                        a.DataCriacao,
                        a.StripeSubscriptionId
                    };
                }).ToList();

                return Ok(resultado);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao listar assinaturas ativas");
                return StatusCode(500, new { error = "Erro ao listar assinaturas ativas", message = ex.Message });
            }
        }

        [Authorize]
        [HttpGet("ads/assinatura/status")]
        public async Task<IActionResult> GetAdsAssinaturaStatus()
        {
            var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
            var user = await _db.Users.FindAsync(userId);
            
            if (user == null)
                return NotFound("Usuário não encontrado.");

            // Buscar assinatura Remove Ads (valor 10) que esteja ATIVA e não expirada
            var now = DateTimeOffset.UtcNow;
            var removeAdsAssinatura = await _db.Assinaturas
                .Where(a => a.UserId == userId && a.Valor == 10 && 
                           (a.Status == "ativa" || a.Status == "Ativa"))
                .OrderByDescending(a => a.DataCriacao)
                .FirstOrDefaultAsync();

            // Verificar se a assinatura está realmente ativa (não expirada)
            bool isActive = false;
            if (removeAdsAssinatura != null)
            {
                // Verificar se a assinatura não expirou
                bool notExpired = !removeAdsAssinatura.ExpiraEm.HasValue || removeAdsAssinatura.ExpiraEm.Value > now;
                isActive = notExpired;
            }

            // Retorna status baseado na verificação
            return Ok(new { status = isActive ? "Assinatura Ativa" : "Sem Assinatura Ativa" });
        }

        public record SubscriptionCancelResult(
            Guid SubscriptionId,
            string LocalStatus,
            string StripeStatus
        );

        public record CheckoutRequest(int Nivel);
    }
}
