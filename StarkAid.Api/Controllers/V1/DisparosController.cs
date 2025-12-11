using System;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StarkAid.Api.DTOs.V1.Devices;
using StarkAid.Api.Services.V1.Devices;

namespace StarkAid.Api.Controllers.V1
{
    [Authorize]
        [ApiVersion("1.0")]
    [ApiController]
    [Route("api/v{version:apiVersion}/[controller]")]
    public class DisparosController : ControllerBase
    {
        private readonly DisparoService _disparoService;
        private readonly FcmNotificationService _fcmService;

        public DisparosController(DisparoService disparoService, FcmNotificationService fcmService)
        {
            _disparoService = disparoService;
            _fcmService = fcmService;
        }

        [HttpPost]
        public async Task<IActionResult> Registrar([FromBody] DisparoRequest request)
        {
            var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));

            var disparoResponse = await _disparoService
                .RegistrarDisparoAsync(userId, request.DispositivoId, request.Mensagem);

            await _fcmService
                .EnviarParaUsuarioAsync(userId, "Alerta de Disparo", request.Mensagem, disparoResponse.Id);

            return Created(string.Empty, disparoResponse);
        }

        [HttpGet]
        public async Task<IActionResult> Listar()
        {
            var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
            var disparos = await _disparoService.ListarDisparosComNomePorUsuarioAsync(userId);
            return Ok(disparos);
        }

        [HttpPut("{id}/confirmar")]
        public async Task<IActionResult> Confirmar(Guid id)
        {
            var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
            var sucesso = await _disparoService.ConfirmarDisparoAsync(id, userId);

            if (!sucesso)
                return NotFound("Disparo não encontrado ou não pertence a você.");

            return Ok("Disparo confirmado.");
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Excluir(Guid id)
        {
            var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
            var removido = await _disparoService.ExcluirAsync(id, userId);

            if (!removido)
                return NotFound("Disparo não encontrado ou não pertence a você.");

            return Ok("Disparo removido.");
        }
    }

    public class DisparoRequest
    {
        public Guid DispositivoId { get; set; }
        public string Mensagem { get; set; } = string.Empty;
    }
}
