using Microsoft.AspNetCore.Mvc;
using StarkAid.Api.Services.V1.Payment.Stripe;

namespace StarkAid.Api.Controllers.V1;

[ApiVersion("1.0")]
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
