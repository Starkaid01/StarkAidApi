using Microsoft.AspNetCore.Mvc;
using StarkAid.Api.Services.V1.Devices;

namespace StarkAid.Api.Controllers.V1;

[ApiVersion("1.0")]
[ApiController]
[Route("api/v{version:apiVersion}/[controller]")]
public class HealthCheckController : ControllerBase
{
    private readonly IMqttClientService _mqtt;

    public HealthCheckController(IMqttClientService mqtt) => _mqtt = mqtt;

    [HttpGet("mqtt")]
    public IActionResult Mqtt() =>
        _mqtt.IsConnected
            ? Ok(new { status = "OK", message = "MQTT conectado." })
            : StatusCode(503, new { status = "Unavailable", message = "MQTT desconectado." });

    [HttpGet("api")]
    public IActionResult Api() => Ok(new { status = "OK", message = "API operacional." });
}
