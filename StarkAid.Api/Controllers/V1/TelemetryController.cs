using Microsoft.AspNetCore.Mvc;
using StarkAid.Api.DTOs.Telemetry;
using StarkAid.Api.Entities;
using StarkAid.Api.Services.Telemetry;
using System.Threading.Tasks;

namespace StarkAid.Api.Controllers.V1
{
    [ApiController]
    [Route("api/v1/telemetry")]
    public sealed class TelemetryController : ControllerBase
    {
        private readonly ITelemetryService _service;

        public TelemetryController(ITelemetryService service)
        {
            _service = service;
        }

        [HttpPost]
        public async Task<IActionResult> Post([FromBody] TelemetryEventDto dto)
        {
            await _service.RegistrarAsync(dto);
            return Ok();
        }

        [HttpPost("ia")]
        public async Task<IActionResult> PostIa([FromBody] AiInteractionEvent evento)
        {
            await _service.RegistrarInteracaoIaAsync(evento);
            return Ok();
        }
    }
}
