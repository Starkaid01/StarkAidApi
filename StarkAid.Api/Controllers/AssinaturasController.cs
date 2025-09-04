using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using StarkAid.Api.Data;
using StarkAid.Api.DTOs;
using StarkAid.Api.Entities;
using StarkAid.Api.EntityConfigurations;
using StarkAid.Api.Services;
using System.Security.Claims;
using Microsoft.Extensions.Logging;

namespace StarkAid.Api.Controllers;

[Route("api/[controller]")]
[ApiController]
public class AssinaturasController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly StripeService _stripeService;
    private readonly EntityConfigurations.StripeSettings _stripeSettings;
    private readonly ILogger<AssinaturasController> _logger; // Declare o campo

    public AssinaturasController(AppDbContext db, StripeService stripeService, IOptions<EntityConfigurations.StripeSettings> stripeOptions, ILogger<AssinaturasController> logger)
    {
        _db = db;
        _stripeService = stripeService;
        _stripeSettings = stripeOptions.Value;
        _logger = logger;
    }

    [Authorize]
    [HttpPost("assinarStarkaidNivel2")]
    public async Task<IActionResult> AssinarStarkaidNivel2()
    {
        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
        var user = await _db.Users.FindAsync(userId);
        if (user == null) return NotFound("Usuário não encontrado.");

        var host = Request.Scheme + "://" + Request.Host.Value;

        var successUrl = $"{_stripeSettings.CheckoutFrontendUrl}?status=success&session_id={{CHECKOUT_SESSION_ID}}";
        var cancelUrl = $"{_stripeSettings.CheckoutFrontendUrl}?status=canceled";

        // tenta achar a última assinatura com StripeCustomerId do usuário
        var ultimaAssinatura = await _db.Assinaturas
            .Where(a => a.UserId == userId)
            .OrderByDescending(a => a.DataCriacao)
            .FirstOrDefaultAsync();

        string? customerId = ultimaAssinatura?.StripeCustomerId;

        // Cria session reusando customerId se existir
        var (session, customer) = await _stripeService.CreateCheckoutSessionAsync(user, _stripeSettings.PriceId, successUrl, cancelUrl, customerId);

        // Se não tinha assinatura pendente vinculada ao customer, cria
        Assinatura assinaturaParaSalvar;
        if (ultimaAssinatura == null || string.IsNullOrEmpty(ultimaAssinatura.StripeCustomerId))
        {
            assinaturaParaSalvar = new Assinatura
            {
                Id = Guid.NewGuid(),
                UserId = user.Id,
                StripeCustomerId = customer.Id,
                StripeSubscriptionId = null,
                Status = "pendente",
                Valor = 1.00m,
                IniciadaEm = DateTimeOffset.UtcNow,
                DataCriacao = DateTimeOffset.UtcNow
            };
            _db.Assinaturas.Add(assinaturaParaSalvar);
        }
        else if (ultimaAssinatura.Status == "pendente")
        {
            // reusa a pendente — atualiza timestamps se quiser
            ultimaAssinatura.DataCriacao = DateTimeOffset.UtcNow;
            ultimaAssinatura.IniciadaEm = DateTimeOffset.UtcNow;
            _db.Assinaturas.Update(ultimaAssinatura);
            assinaturaParaSalvar = ultimaAssinatura;
        }
        else
        {
            // Criar nova assinatura pendente, mas reusar customer.Id
            assinaturaParaSalvar = new Assinatura
            {
                Id = Guid.NewGuid(),
                UserId = user.Id,
                StripeCustomerId = customer.Id,
                StripeSubscriptionId = null,
                Status = "pendente",
                Valor = 1.00m,
                IniciadaEm = DateTimeOffset.UtcNow,
                DataCriacao = DateTimeOffset.UtcNow
            };
            _db.Assinaturas.Add(assinaturaParaSalvar);
        }

        await _db.SaveChangesAsync();

        var jwt = Request.Headers["Authorization"].ToString().Replace("Bearer ", "");
        var redirect = $"{_stripeSettings.CheckoutFrontendUrl}?session_id={session.Id}&token={jwt}";
        return Ok(new { redirectUrl = redirect, sessionId = session.Id });
    }

    [HttpGet("checkout-page")]
    public IActionResult ServeCheckoutPage()
    {
        var filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "checkout-page.html");

        if (!System.IO.File.Exists(filePath))
        {
            return NotFound("Página de checkout não encontrada");
        }

        return PhysicalFile(filePath, "text/html");
    }


    // Endpoint que o checkout.html chama para obter session.url (redirecionamento seguro)
    [HttpGet("session/{sessionId}")]
    public async Task<IActionResult> GetSessionUrl(string sessionId)
    {
        if (!Request.Headers.TryGetValue("Authorization", out var auth) || string.IsNullOrWhiteSpace(auth))
            return Unauthorized();

        var session = await _stripeService.GetSessionAsync(sessionId);
        if (session == null) return NotFound();

        return Ok(new { sessionUrl = session.Url });
    }

    // POST: api/assinaturas/cancelar
    [Authorize]
    [HttpPost("cancelar")]
    public async Task<IActionResult> Cancelar()
    {
        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
        _logger.LogInformation($"usuário id encontrado: {userId}");

        // Buscar todas as assinaturas ativas do usuário
        var assinaturasAtivas = await _db.Assinaturas
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
            _db.Assinaturas.Update(assinatura);

            results.Add(new SubscriptionCancelResult(
                assinatura.Id,
                stripeResult != null ? "Cancelada" : "Falha no cancelamento",
                stripeResult?.Status ?? "Erro"
                
            ));
            _logger.LogInformation($"Resultado do cancelamento: {stripeResult?.Status}");
        }

        // Rebaixar usuário para nível 1
        var user = await _db.Users.FindAsync(userId);
        if (user != null)
        {
            user.Role = "UserNivel1";
            _db.Users.Update(user);
        }

        await _db.SaveChangesAsync();

        _logger.LogInformation($"Total de assinaturas canceladas: {results.Count}");
        return Ok(new
        {
            Message = "Solicitação de cancelamento processada",
            Results = results
        });
    }

    // GET: api/assinaturas/status
    [Authorize]
    [HttpGet("status")]
    public async Task<IActionResult> Status()
    {
        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
        var assinatura = await _db.Assinaturas.OrderByDescending(a => a.DataCriacao).FirstOrDefaultAsync(a => a.UserId == userId);
        if (assinatura == null) return NotFound("Nenhuma assinatura encontrada.");

        return Ok(new
        {
            assinatura.Status,
            assinatura.IniciadaEm,
            assinatura.CanceladaEm,
            assinatura.ExpiraEm,
            assinatura.PagamentoConfirmadoEm,
            assinatura.StripeCustomerId,
            assinatura.StripeSubscriptionId
        });
    }
    [Authorize]
    [HttpGet("has-active-subscriptions")]
    public async Task<IActionResult> HasActiveSubscriptions()
    {
        var userIdFromToken = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(userIdFromToken, out Guid userId))
            return BadRequest("Token inválido");

        var hasActiveSubscriptions = await _db.Assinaturas
            .AnyAsync(a => a.UserId == userId &&
                          a.Status == "Ativa");

        return Ok(new { HasActiveSubscriptions = hasActiveSubscriptions });
    }

}