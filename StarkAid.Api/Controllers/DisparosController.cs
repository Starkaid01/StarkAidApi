using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StarkAid.Api.Services.Devices;
using System.Security.Claims;

namespace StarkAid.Api.Controllers
{
    [Authorize]        
    [ApiController]
    [Route("api/[controller]")]
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

            // Registrar disparo no banco (agora retorna DTO)
            var disparoResponse = await _disparoService.RegistrarDisparoAsync(userId, request.DispositivoId, request.Mensagem);

            // Disparar notificação FCM
            await _fcmService.EnviarParaUsuarioAsync(userId, "Alerta de Disparo", request.Mensagem, disparoResponse.Id);

            return Created("", disparoResponse); // Retorna o DTO
        }

        [HttpGet]
        public async Task<IActionResult> Listar()
        {
            var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
            var disparos = await _disparoService.ListarDisparosComNomePorUsuarioAsync(userId);
            return Ok(disparos); // Já retorna List<DisparoResponse>
        }

        [HttpPut("{id}/confirmar")]
        public async Task<IActionResult> Confirmar(Guid id)
        {
            var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
            var sucesso = await _disparoService.ConfirmarDisparoAsync(id, userId);
            if (!sucesso) return NotFound("Disparo não encontrado ou não pertence a você.");
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
