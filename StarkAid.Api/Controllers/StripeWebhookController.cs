using Microsoft.AspNetCore.Mvc;
using StarkAid.Api.Services.Assinatura;
using StarkAid.Api.Services.Payment;
using StarkAid.Api.Services.Payment.Stripe;

namespace StarkAid.Api.Controllers;

[ApiController]
[Route("api/webhook/stripe")]
public class StripeWebhookController : ControllerBase
{
    private readonly StripeWebhookService _service;

    public StripeWebhookController(StripeWebhookService service) => _service = service;

    [HttpPost]
    public async Task<IActionResult> Post()
    {
        var result = await _service.ProcessWebhookAsync(Request);
        return Ok(new { status = result });
    }
}
