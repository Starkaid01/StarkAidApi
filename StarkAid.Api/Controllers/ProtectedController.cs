using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace StarkAid.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
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