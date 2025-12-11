using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using StarkAid.Api.Services.V1.Firebase;

namespace StarkAid.Api.Controllers.V1
{
    [Authorize]
        [ApiVersion("1.0")]
    [ApiController]
    [Route("api/v{version:apiVersion}/[controller]")]
    public class FirebaseTokenController : ControllerBase
    {
        private readonly FirebaseTokenService _service;

        public FirebaseTokenController(FirebaseTokenService service)
        {
            _service = service;
        }

        [HttpPost("registrar-token")]
        public async Task<IActionResult> RegistrarToken([FromBody] TokenRequest request)
        {
            var userIdClaim = User.Claims.FirstOrDefault(x => x.Type == "nameid")?.Value;
            if (userIdClaim is null)
                return Unauthorized();

            await _service.CadastrarOuAtualizarAsync(Guid.Parse(userIdClaim), request.Token);
            return Ok(new { message = "Token salvo com sucesso." });
        }
    }

    public class TokenRequest
    {
        public string Token { get; set; } = string.Empty;
    }
}
