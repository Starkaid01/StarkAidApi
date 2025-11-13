using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using StarkAid.Api.DTOs.Devices;
using StarkAid.Api.Services.Devices;
using System.Security.Claims;

namespace StarkAid.Api.Controllers
{
    [Authorize]
    [Authorize(Policy = "UserNivel2Only")]
    [ApiController]
    [Route("api/[controller]")]
    public class AgendamentosController : ControllerBase
    {
        private readonly AgendamentoService _service;

        public AgendamentosController(AgendamentoService service)
        {
            _service = service;
        }

        [HttpGet]
        public async Task<IActionResult> Listar()
        {
            var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
            var agendamentos = await _service.BuscarPorUsuarioAsync(userId);
            return Ok(agendamentos);
        }

        [HttpPost]
        public async Task<IActionResult> Criar([FromBody] CriarAgendamentoRequest request)
        {
            var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
            var agendamento = await _service.CriarAsync(
                userId, 
                request.DeviceId, 
                request.AgendadoPara, 
                request.Comando,
                request.Recorrencia);
            return Created("", agendamento);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Editar(Guid id, [FromBody] EditarAgendamentoRequest request)
        {
            var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
            var atualizado = await _service.EditarAsync(
                id, 
                userId, 
                request.AgendadoPara, 
                request.Comando, 
                request.Recorrencia);
            if (!atualizado)
                return NotFound("Agendamento não encontrado ou não pertence a você.");

            return Ok("Agendamento atualizado.");
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Excluir(Guid id)
        {
            var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
            var removido = await _service.ExcluirAsync(id, userId);
            if (!removido)
                return NotFound("Agendamento não encontrado ou não pertence a você.");

            return Ok("Agendamento removido.");
        }
    }
}
