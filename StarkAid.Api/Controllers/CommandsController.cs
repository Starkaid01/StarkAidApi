using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using StarkAid.Api.DTOs.Devices;
using StarkAid.Api.Services.Devices;

namespace StarkAid.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class CommandsController : ControllerBase
{
    private readonly IMqttClientService _mqtt;
    private readonly IDeviceService _deviceService;
    private readonly ILogger<CommandsController> _logger; // Adicione o campo _logger

    public CommandsController(IMqttClientService mqtt, IDeviceService deviceService, ILogger<CommandsController> logger)
    {
        _mqtt = mqtt;
        _deviceService = deviceService;
        _logger = logger; // Inicialize o _logger no construtor
    }

    [HttpPost("publish")]
    public async Task<IActionResult> Publish([FromBody] PublishCommandRequest request)
    {
        // Validação de parâmetros
        var hasEnum = request.Command.HasValue;
        var hasCustom = !string.IsNullOrWhiteSpace(request.CustomCommand);

        if (!hasEnum && !hasCustom)
            return BadRequest("Nenhum comando especificado.");

        if (hasEnum && hasCustom)
            return BadRequest("Informe apenas um tipo de comando (enum ou custom).");

        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
        var device = await _deviceService.GetByIdAsync(request.DeviceId);
        if (device == null) return NotFound("Dispositivo não encontrado.");
        if (device.UserId != userId) return Forbid();

        if (!_mqtt.IsConnected) return StatusCode(503, "Serviço MQTT indisponível.");

        // 🔥 CORREÇÃO: Lógica simplificada e robusta para determinar payload
        string payload;

        if (hasCustom && !string.IsNullOrWhiteSpace(request.CustomCommand))
        {
            payload = request.CustomCommand.Trim();
        }
        else if (hasEnum && request.Command.HasValue)
        {
            payload = request.Command.Value.ToString().ToLower();
        }
        else if (!string.IsNullOrWhiteSpace(device.Comando))
        {
            payload = device.Comando.Trim();
        }
        else
        {
            return BadRequest("Não foi possível determinar o comando a ser enviado.");
        }

        // Log para debug
        _logger.LogInformation($"Enviando comando: '{payload}' para tópico: {device.MqttTopic}");

        await _mqtt.PublishAsync(device.MqttTopic, payload);
        return Ok(new { message = $"Comando '{payload}' enviado.", topic = device.MqttTopic });
    }
}
