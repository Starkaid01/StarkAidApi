using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StarkAid.Api.DTOs;
using StarkAid.Api.Services;
using System.Security.Claims;

namespace StarkAid.Api.Controllers
{
    [Authorize]
    [Authorize(Policy = "UserNivel2Only")]
    [ApiController]    
    [Route("api/[controller]")]
    public class CommandsController : ControllerBase
    {
        private readonly IMqttClientService _mqttClient;
        private readonly DeviceService _deviceService;

        public CommandsController(IMqttClientService mqttClient, DeviceService deviceService)
        {
            _mqttClient = mqttClient;
            _deviceService = deviceService;
        }

        [HttpPost("publish")]
        public async Task<IActionResult> PublishCommand([FromBody] PublishCommandRequest request)
        {
            if (request.DeviceId == Guid.Empty)
                return BadRequest("DeviceId obrigatório.");

            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!Guid.TryParse(userIdStr, out var userId))
                return Unauthorized("Token inválido.");

            var device = await _deviceService.GetByIdAsync(request.DeviceId);
            if (device == null)
                return NotFound("Dispositivo não encontrado.");

            if (device.UserId != userId)
                return Forbid("Este dispositivo não pertence a você.");

            if (!_mqttClient.IsConnected)
                return StatusCode(503, "Serviço MQTT indisponível.");

            // Publica o comando usando o enum diretamente
            var payload = request.Command.ToString().ToLower();
            await _mqttClient.PublishAsync(device.MqttTopic, payload);

            return Ok(new { message = $"Comando '{payload}' enviado via MQTT.", topic = device.MqttTopic });
        }
    }
}