using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace StarkAid.Api.Controllers.V1
{
    [ApiVersion("1.0")]
    [ApiController]
    [Route("api/v{version:apiVersion}/[controller]")]
    public class ProtectedController : ControllerBase
    {
        [HttpGet("secreto")]
        [Authorize]
        public IActionResult GetSecreto()
        {
            return Ok("Acesso permitido para usuário autenticado.");
        }
    }
}