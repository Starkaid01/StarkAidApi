using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Newtonsoft.Json.Linq;
using StarkAid.Api.Data;
using StarkAid.Api.Entities;
using StarkAid.Api.EntityConfigurations;
using StarkAid.Api.Services.Assinatura;
using Stripe;

namespace StarkAid.Api.Controllers;

[ApiController]
[Route("api/webhook/stripe")]
public class StripeWebhookController : ControllerBase
{
    private readonly StripeWebhookService _webhookService;

    public StripeWebhookController(StripeWebhookService webhookService)
    {
        _webhookService = webhookService;
    }

    [HttpPost]
    public async Task<IActionResult> Post()
    {
        var resultado = await _webhookService.ProcessWebhookAsync(Request);
        return Ok(new { status = resultado });
    }
}