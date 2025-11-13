using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using StarkAid.Api.Data;
using StarkAid.Api.Entities;
using Stripe.Checkout;
using Stripe;
using System.Security.Claims;
using StarkAid.Api.DTOs.Assinatura;
using StarkAid.Api.Services.Assinatura;

namespace StarkAid.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AssinaturasController : ControllerBase
    {
        private readonly AppDbContext _db;
        private readonly StripeService _stripeService;
        private readonly EntityConfigurations.StripeSettings _stripeSettings;
        private readonly ILogger<AssinaturasController> _logger;

        public AssinaturasController(
            AppDbContext db,
            StripeService stripeService,
            IOptions<EntityConfigurations.StripeSettings> stripeOptions,
            ILogger<AssinaturasController> logger)
        {
            _db = db;
            _stripeService = stripeService;
            _stripeSettings = stripeOptions.Value;
            _logger = logger;
        }

        // ✅ Corrigido: usar _stripeSettings
        [HttpPost("create/{nivel:int}")]
        public async Task<IActionResult> CreateSubscription([FromRoute] int nivel, [FromQuery] Guid userId)
        {
            var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == userId);
            if (user == null)
                return NotFound("Usuário não encontrado.");

            _logger.LogInformation("🔍 Verificando assinaturas ativas para o usuário {UserId}", user.Id);

            // ✅ Cancelar apenas assinaturas ativas que NÃO sejam do nível 2
            var assinaturasAtivas = await _db.Assinaturas
                .Where(a => a.UserId == user.Id && a.Status == "Ativa")
                .ToListAsync();

            foreach (var assinaturaAntiga in assinaturasAtivas)
            {
                // se for nível 2 (sem anúncios), não cancela
                if (assinaturaAntiga.Valor == 10)
                {
                    _logger.LogInformation("⏩ Mantendo assinatura nível 2 ativa (não cancelada).");
                    continue;
                }

                try
                {
                    if (!string.IsNullOrEmpty(assinaturaAntiga.StripeSubscriptionId))
                    {
                        await _stripeService.CancelSubscriptionAsync(assinaturaAntiga.StripeSubscriptionId);
                        _logger.LogInformation("🛑 Assinatura anterior {SubId} cancelada no Stripe.", assinaturaAntiga.StripeSubscriptionId);
                    }

                    assinaturaAntiga.Status = "Cancelada";
                    assinaturaAntiga.CanceladaEm = DateTimeOffset.UtcNow;
                    _db.Assinaturas.Update(assinaturaAntiga);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Erro ao cancelar assinatura anterior {Id}", assinaturaAntiga.Id);
                }
            }

            await _db.SaveChangesAsync();

            // ✅ Definir preço conforme o nível
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

            (Session session, Customer customer) = await _stripeService.CreateCheckoutSessionAsync(
                user,
                priceId,
                successUrl: $"{_stripeSettings.AppDeepLink}?success=true&nivel={nivel}",
                cancelUrl: $"{_stripeSettings.AppDeepLink}?success=false"
            );

            // ✅ Definir valor da assinatura
            var valor = nivel switch
            {
                2 => 10m,  // sem anúncios, sem StarkCoins
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


        [Authorize]
        [HttpPost("cancelar")]
        public async Task<IActionResult> Cancelar()
        {
            var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
            _logger.LogInformation($"usuário id encontrado: {userId}");

            var assinaturasAtivas = await _db.Assinaturas
                .Where(a => a.UserId == userId && a.Status == "Ativa")
                .ToListAsync();

            if (!assinaturasAtivas.Any())
                return BadRequest("Nenhuma assinatura ativa encontrada.");

            var results = new List<SubscriptionCancelResult>();

            foreach (var assinatura in assinaturasAtivas)
            {
                var stripeResult = await _stripeService.CancelSubscriptionAsync(assinatura.StripeSubscriptionId!);

                assinatura.Status = "Cancelada";
                assinatura.CanceladaEm = DateTimeOffset.UtcNow;
                _db.Assinaturas.Update(assinatura);

                results.Add(new SubscriptionCancelResult(
                    assinatura.Id,
                    stripeResult != null ? "Cancelada" : "Falha no cancelamento",
                    stripeResult?.Status ?? "Erro"
                ));
            }

            var user = await _db.Users.FindAsync(userId);
            if (user != null)
            {
                user.Role = "UserNivel1";
                user.RemovalAds = "Desativado";

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

            var (session, customer) = await _stripeService.CreateOneTimePaymentSessionAsync(
                user,
                valor,
                successUrl: $"{_stripeSettings.AppDeepLink}?pagamento=sucesso&valor={valor}",
                cancelUrl: $"{_stripeSettings.AppDeepLink}?pagamento=cancelado"
            );

            // Cria registro local (pode ser em tabela PagamentoAvulso)
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
        [HttpGet("ads/assinatura/status")]
        public async Task<IActionResult> AdsAssinaturaStatus()
        {
            var userIdFromToken = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!Guid.TryParse(userIdFromToken, out Guid userId))
                return BadRequest("Token inválido");

            var user = await _db.Users.FindAsync(userId);
            if (user == null)
                return NotFound("Usuário não encontrado.");

            var assinaturaUserAds = user.RemovalAds;

            if (assinaturaUserAds == "Ativo")
            {
                return Ok(new
                {
                    Status = "Assinatura Ativa"
                });
            }
            else
            {
                return NotFound("Nenhuma assinatura encontrada.");
            }
        }
    }
}
