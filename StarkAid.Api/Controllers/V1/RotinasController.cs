using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StarkAid.Api.DTOs.Rotinas;
using StarkAid.Api.Services.V1.Rotinas;
using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;

namespace StarkAid.Api.Controllers.V1
{
    [ApiVersion("1.0")]
    [Authorize]
    [ApiController]
    [Route("api/[controller]")] // Matching user request /api/rotinas
    public class RotinasController : ControllerBase
    {
        private readonly IRotinaService _rotinaService;
        private readonly Microsoft.Extensions.DependencyInjection.IServiceScopeFactory _scopeFactory;

        public RotinasController(IRotinaService rotinaService, Microsoft.Extensions.DependencyInjection.IServiceScopeFactory scopeFactory)
        {
            _rotinaService = rotinaService;
            _scopeFactory = scopeFactory;
        }

        private Guid GetUserId()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null) return Guid.Empty;
            return Guid.Parse(userIdClaim.Value);
        }

        [HttpGet]
        public async Task<ActionResult<List<RotinaDto>>> GetAll()
        {
            var userId = GetUserId();
            var result = await _rotinaService.GetAllAsync(userId);
            
            // Se o usuário não tem nenhuma rotina, cria as padrão para ele não ver a tela vazia
            if (result.Count == 0)
            {
                await _rotinaService.SeedDefaultRotinasAsync(userId);
                result = await _rotinaService.GetAllAsync(userId);
            }
            
            return Ok(result);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<RotinaDto>> GetById(Guid id)
        {
            var result = await _rotinaService.GetByIdAsync(id, GetUserId());
            if (result == null) return NotFound();
            return Ok(result);
        }

        [HttpPost]
        public async Task<ActionResult<RotinaDto>> Create([FromBody] CreateRotinaRequest request)
        {
            var result = await _rotinaService.CreateAsync(GetUserId(), request);
            return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
        }

        [HttpPut("{id}")]
        public async Task<ActionResult<RotinaDto>> Update(Guid id, [FromBody] UpdateRotinaRequest request)
        {
            var result = await _rotinaService.UpdateAsync(id, GetUserId(), request);
            if (result == null) return NotFound();
            return Ok(result);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(Guid id)
        {
            var success = await _rotinaService.DeleteAsync(id, GetUserId());
            if (!success) return NotFound();
            return NoContent();
        }

        [HttpPost("{id}/ativar")]
        public async Task<IActionResult> Ativar(Guid id)
        {
            var success = await _rotinaService.SetAtivaAsync(id, GetUserId(), true);
            if (!success) return NotFound();
            return Ok();
        }

        [HttpPost("{id}/desativar")]
        public async Task<IActionResult> Desativar(Guid id)
        {
            var success = await _rotinaService.SetAtivaAsync(id, GetUserId(), false);
            if (!success) return NotFound();
            return Ok();
        }

        [HttpPost("{id}/executar")]
        public async Task<IActionResult> Executar(Guid id)
        {
            // Executa em uma task separada se for longa, ou aguarda. 
            // Como rotinas podem ter delays, melhor executar em background.
            var userId = GetUserId();
            _ = Task.Run(async () => 
            {
                using (var scope = _scopeFactory.CreateScope())
                {
                    var scopedService = scope.ServiceProvider.GetRequiredService<IRotinaService>();
                    await scopedService.ExecutarRotinaAsync(id, userId);
                }
            });
            return Accepted(new { message = "Rotina iniciada com sucesso." });
        }
    }
}
