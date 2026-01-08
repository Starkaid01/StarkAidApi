using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StarkAid.Api.Entities;
using StarkAid.Api.Services.V1.Lembretes;
using System.Security.Claims;
using System.Threading.Tasks;

namespace StarkAid.Api.Controllers.V1
{
    [ApiController]
    [Route("api/v1/[controller]")]
    [Authorize]
    public class LembretesController : ControllerBase
    {
        private readonly ILembreteService _lembreteService;

        public LembretesController(ILembreteService lembreteService)
        {
            _lembreteService = lembreteService;
        }

        [HttpPost]
        public async Task<IActionResult> Criar([FromBody] CreateLembreteRequest request)
        {
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!Guid.TryParse(userIdStr, out var userId)) 
                return Unauthorized();
            
            Lembrete lembrete;
            if (request.DispararEm.HasValue)
            {
               lembrete = await _lembreteService.CriarLembreteAsync(userId, request.Texto, request.DispararEm.Value);
            }
            else
            {
               lembrete = await _lembreteService.ProcessarTextoLembreteAsync(userId, request.Texto);
               if (lembrete == null)
               {
                   return Ok(new { success = false, message = "Não entendi o horário", code = "MISSING_TIME", texto = request.Texto });
               }
            }

            return Ok(new { success = true, lembrete });
        }

        [HttpGet]
        public async Task<IActionResult> ObterTodos()
        {
             var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
             if (!Guid.TryParse(userIdStr, out var userId)) 
                return Unauthorized();
             
             var lembretes = await _lembreteService.ObterDoUsuarioAsync(userId);
             return Ok(lembretes);
        }

        [HttpPost("{id}/falado")]
        public async Task<IActionResult> MarcarFalado(Guid id)
        {
            await _lembreteService.MarcarComoFaladoAsync(id);
            return Ok();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Remover(Guid id)
        {
             await _lembreteService.RemoverAsync(id);
             return Ok();
        }
    }

    public class CreateLembreteRequest
    {
        public string Texto { get; set; }
        public DateTimeOffset? DispararEm { get; set; }
    }
}
