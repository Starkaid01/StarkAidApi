using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StarkAid.Api.Services.Devices;
using System.Security.Claims;

namespace StarkAid.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class StatusController : ControllerBase
{
    private readonly IMqttClientService _mqttClient;
    private readonly DeviceService _deviceService;

    public StatusController(IMqttClientService mqttClient, DeviceService deviceService)
    {
        _mqttClient = mqttClient;
        _deviceService = deviceService;
    }

    [HttpGet("{deviceId}/status")]
    public async Task<IActionResult> GetStatus(Guid deviceId)
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userIdClaim))
            return Unauthorized("Usuário não autenticado corretamente.");

        if (!Guid.TryParse(userIdClaim, out var userId))
            return Unauthorized("Identificador de usuário inválido.");

        var device = await _deviceService.GetByIdAsync(deviceId);
        if (device == null || device.UserId != userId)
            return NotFound("Dispositivo não encontrado ou não pertence ao usuário.");

        if (!_mqttClient.IsConnected)
            return StatusCode(503, "Serviço MQTT indisponível no momento.");

        var topic = $"starkaid/{userId}/{deviceId}/commands/status";
        var status = await _mqttClient.GetStatusAsync(topic);

        if (string.IsNullOrEmpty(status))
            return StatusCode(503, "Status do dispositivo indisponível no momento.");

        return Ok(new { deviceId, status });
    }
}