using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StarkAid.Api.DTOs.Commands;
using StarkAid.Api.Services.CommandRouter;
using System;
using System.Security.Claims;
using System.Threading.Tasks;

namespace StarkAid.Api.Controllers.V1
{
    [Authorize]
    [ApiController]
    [Route("api/v1/commands")]
    public sealed class CommandController : ControllerBase
    {
        private readonly ICommandRouter _router;

        public CommandController(ICommandRouter router)
        {
            _router = router;
        }

        [HttpPost("route")]
        public async Task<IActionResult> Route([FromBody] CommandRequestDto request)
        {
            // Garantir que o UserId do request corresponde ao usuário autenticado, 
            // a menos que seja uma origem interna/confiável.
            // Para segurança básica, injetamos o UserId do Token.
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (Guid.TryParse(userIdClaim, out var userId))
            {
                request.UserId = userId;
            }

            var result = await _router.RouteAsync(request);

            if (result.IsSuccess)
                return Ok(result);

            return BadRequest(result);
        }
    }
}
