using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using StarkAid.Api.Services.V1.Devices;
using System.Security.Claims;

namespace StarkAid.Api.Controllers.V1;

[Authorize]
[ApiVersion("1.0")]
[ApiController]
[Route("api/v{version:apiVersion}/[controller]")]
public class StatusController : ControllerBase
{
    private readonly IMqttClientService _mqttClient;
    private readonly DeviceService _deviceService;
    private readonly ILogger<StatusController> _logger;

    public StatusController(IMqttClientService mqttClient, DeviceService deviceService, ILogger<StatusController> logger)
    {
        _mqttClient = mqttClient;
        _deviceService = deviceService;
        _logger = logger;
    }

    [HttpGet("{deviceId}/status")]
    public async Task<IActionResult> GetStatus(string deviceId)
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userIdClaim))
            return Unauthorized("Usuário não autenticado corretamente.");

        if (!Guid.TryParse(userIdClaim, out var userId))
            return Unauthorized("Identificador de usuário inválido.");

        // Validar se deviceId é um Guid válido
        if (!Guid.TryParse(deviceId, out var deviceIdGuid))
        {
            _logger.LogWarning("❌ DeviceId inválido recebido: '{DeviceId}' do usuário {UserId}", deviceId, userId);
            return BadRequest($"DeviceId inválido: '{deviceId}'. Deve ser um GUID válido.");
        }

        _logger.LogDebug("🔍 Buscando status do dispositivo {DeviceId} para usuário {UserId}", deviceIdGuid, userId);

        var device = await _deviceService.GetByIdAsync(deviceIdGuid);
        if (device == null)
        {
            _logger.LogWarning("❌ Dispositivo {DeviceId} não encontrado para usuário {UserId}", deviceIdGuid, userId);
            return NotFound($"Dispositivo {deviceId} não encontrado.");
        }

        if (device.UserId != userId)
        {
            _logger.LogWarning("🚫 Acesso negado: dispositivo {DeviceId} pertence ao usuário {DeviceUserId}, mas requisição veio de {UserId}", 
                deviceIdGuid, device.UserId, userId);
            return Forbid("Dispositivo não pertence ao usuário.");
        }

        if (!_mqttClient.IsConnected)
            return StatusCode(503, "Serviço MQTT indisponível no momento.");

        var topic = $"starkaid/{userId}/{deviceIdGuid}/commands/status";
        var status = await _mqttClient.GetStatusAsync(topic);

        if (string.IsNullOrEmpty(status))
            return StatusCode(503, "Status do dispositivo indisponível no momento.");

        return Ok(new { deviceId = deviceIdGuid, status });
    }
}