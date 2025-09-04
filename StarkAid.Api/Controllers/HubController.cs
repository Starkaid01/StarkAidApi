using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using StarkAid.Api.Hubs;

namespace StarkAid.Api.Controllers
{    
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class HubController : ControllerBase
    {
        private readonly IHubContext<DeviceHub> _hubContext;
        public HubController(IHubContext<DeviceHub> hubContext)
        {
            _hubContext = hubContext;
        }

        [HttpPost("send-to-user")]
        public async Task<IActionResult> SendToUser([FromBody] CommandRequest request)
        {
            // Validação básica
            if (string.IsNullOrWhiteSpace(request.Command))
                return BadRequest("Comando não informado.");

            var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim))
                return Unauthorized();

            // Aqui você poderia salvar o comando no banco, ou validar dispositivo
            // Exemplo: verificar se o device pertence ao user
            // var device = await _deviceService.GetByIdAsync(request.DeviceId);

            // Envia comando via SignalR para o usuário
            await _hubContext.Clients.Group(userIdClaim)
                .SendAsync("ReceiveCommand", request.DeviceId, request.Command);

            return Ok(new { status = "Comando enviado via SignalR", deviceId = request.DeviceId, command = request.Command });
        }

    }

    public class CommandRequest
    {
        public Guid DeviceId { get; set; }
        public string Command { get; set; }
    }
}
