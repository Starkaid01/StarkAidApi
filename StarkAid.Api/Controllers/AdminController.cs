using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace StarkAid.Api.Controllers
{

    [Authorize]
    [Authorize(Policy = "AdministradorOnly")]
    [ApiController]
    [Route("api/[controller]")]
    public class AdminController : ControllerBase
    {
        
        [HttpGet("admin-only")]
        public IActionResult AdminOnly()
        {
            return Ok("Acesso exclusivo para administradores.");
        }
    }
}
