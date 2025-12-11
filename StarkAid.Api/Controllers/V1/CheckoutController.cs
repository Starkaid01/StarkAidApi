using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using StarkAid.Api.Options;

namespace StarkAid.Api.Controllers.V1
{
    [ApiVersion("1.0")]
    [ApiController]
    [Route("api/v{version:apiVersion}/assinaturas/checkout")]
    public class CheckoutController : ControllerBase
    {
        private readonly IOptions<StripeSettings> _stripeSettings;

        public CheckoutController(IOptions<StripeSettings> stripeSettings)
        {
            _stripeSettings = stripeSettings;
        }



        [HttpGet("success")] // Rota: /api/assinaturas/checkout/success
        public IActionResult Success(string session_id)
        {
            Console.WriteLine($"CheckoutSuccess chamado com session_id: {session_id}");
            Console.WriteLine($"Redirecionando para deep link: {_stripeSettings.Value.AppDeepLink}?status=success&session_id={session_id}");
            return Redirect($"{_stripeSettings.Value.AppDeepLink}?status=success&session_id={session_id}");
        }

        [HttpGet("cancel")] // Rota: /api/assinaturas/checkout/cancel
        public IActionResult Cancel()
        {
            Console.WriteLine("Redirecionando para deep link de cancelamento");
            return Redirect($"{_stripeSettings.Value.AppDeepLink}?status=canceled");
        }
    }
}