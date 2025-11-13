using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StarkAid.Api.DTOs.Devices;
using StarkAid.Api.Services.Devices;
using System.Security.Claims;
using System.Text.Json;

namespace StarkAid.Api.Controllers
{
    [Authorize]
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
            // Validação manual das regras de negócio
            bool hasEnumCommand = request.Command.HasValue;
            bool hasCustomCommand = !string.IsNullOrWhiteSpace(request.CustomCommand);

            if (!hasEnumCommand && !hasCustomCommand)
            {
                return BadRequest("Nenhum comando especificado.");
            }

            if (hasEnumCommand && hasCustomCommand)
            {
                return BadRequest("Use apenas um tipo de comando por vez (enum ou customizado).");
            }
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

            // Prioridade: Comando personalizado do banco > Comando personalizado da requisição > Comando enum
            string payload;

            if (!string.IsNullOrWhiteSpace(device.Comando))
            {
                // Usa o comando personalizado salvo no banco
                payload = device.Comando.Trim();
            }
            else if (hasCustomCommand)
            {
                // Usa o comando personalizado da requisição
                payload = request.CustomCommand.Trim();
            }
            else
            {
                // Usa o comando enum
                payload = request.Command.Value.ToString().ToLower();
            }

            await _mqttClient.PublishAsync(device.MqttTopic, payload);

            await WebsocketController.SendToUser(device.UserId.ToString(),
            JsonSerializer.Serialize(new
            {
                deviceId = device.Id,
                status = payload // ou traduzir "ligar" -> "ligado", "desligar" -> "desligado"
            }));

            return Ok(new
            {
                message = $"Comando '{payload}' enviado via MQTT.",
                topic = device.MqttTopic,
                source = !string.IsNullOrWhiteSpace(device.Comando) ? "database" :
                         hasCustomCommand ? "request" : "enum"
            });
        }
    }
}