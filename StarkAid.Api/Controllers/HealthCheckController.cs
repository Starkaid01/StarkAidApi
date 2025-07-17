using Microsoft.AspNetCore.Mvc;
using StarkAid.Api.Services;

namespace StarkAid.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class HealthCheckController : ControllerBase
{
    private readonly IMqttClientService _mqttClient;

    public HealthCheckController(IMqttClientService mqttClient)
    {
        _mqttClient = mqttClient;
    }

    [HttpGet("mqtt")]
    public IActionResult CheckMqtt()
    {
        if (_mqttClient.IsConnected)
            return Ok(new { status = "OK", message = "MQTT conectado." });

        return StatusCode(503, new { status = "Unavailable", message = "MQTT desconectado." });
    }

    [HttpGet("api")]
    public IActionResult CheckApi()
    {
        return Ok(new { status = "OK", message = "API StarkAid operando." });
    }
}
