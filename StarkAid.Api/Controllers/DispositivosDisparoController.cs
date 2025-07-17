using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using StarkAid.Api.DTOs;
using StarkAid.Api.Services;
using System.Security.Claims;

namespace StarkAid.Api.Controllers
{
    [Authorize]
    [Authorize(Policy = "UserNivel2Only")]
    [Route("api/[controller]")]
    [ApiController]
    public class DispositivosDisparoController : ControllerBase
    {
        private readonly DispositivoDisparoService _service;

        public DispositivosDisparoController(DispositivoDisparoService service)
        {
            _service = service;
        }

        [HttpPost]
        public async Task<IActionResult> Criar([FromBody] CriarDispositivoDisparoRequest request)
        {
            var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
            var dispositivo = await _service.CriarAsync(userId, request.Nome);
            return Created("", dispositivo);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Editar(Guid id, [FromBody] EditarDispositivoDisparoRequest request)
        {
            var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
            var atualizado = await _service.EditarAsync(id, userId, request.Nome);
            if (!atualizado) return NotFound();
            return Ok("Dispositivo atualizado.");
        }

        [HttpGet]
        public async Task<IActionResult> Listar()
        {
            var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
            var dispositivos = await _service.ListarPorUsuarioAsync(userId);
            return Ok(dispositivos);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> Obter(Guid id)
        {
            var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
            var dispositivo = await _service.ObterPorIdAsync(id, userId);
            if (dispositivo == null) return NotFound();
            return Ok(dispositivo);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Excluir(Guid id)
        {
            var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
            var removido = await _service.ExcluirAsync(id, userId);
            if (!removido) return NotFound();
            return Ok("Dispositivo removido.");
        }
    }

}
