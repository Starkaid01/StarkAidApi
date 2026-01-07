using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StarkAid.Api.DTOs.V1.Comodos;
using StarkAid.Api.Services.V1.Comodos;
using System.Security.Claims;

namespace StarkAid.Api.Controllers.V1
{
    [ApiVersion("1.0")]
    [Authorize]
    [ApiController]
    [Route("api/v{version:apiVersion}/[controller]")]
    public class ComodosController : ControllerBase
    {
        private readonly IComodoService _comodoService;
        private readonly IEscopoConversacionalService _escopoService;

        public ComodosController(IComodoService comodoService, IEscopoConversacionalService escopoService)
        {
            _comodoService = comodoService;
            _escopoService = escopoService;
        }

        private Guid GetUserId()
            => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var result = await _comodoService.GetAllAsync(GetUserId());
            return Ok(result);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(Guid id)
        {
            var result = await _comodoService.GetByIdAsync(id, GetUserId());
            if (result == null) return NotFound();
            return Ok(result);
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateComodoRequest request)
        {
            var result = await _comodoService.CreateAsync(GetUserId(), request);
            return CreatedAtAction(nameof(GetById), new { id = result.Id, version = "1" }, result);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(Guid id, [FromBody] UpdateComodoRequest request)
        {
            var result = await _comodoService.UpdateAsync(id, GetUserId(), request);
            if (result == null) return NotFound();
            return Ok(result);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(Guid id)
        {
            var success = await _comodoService.DeleteAsync(id, GetUserId());
            if (!success) return NotFound();
            return NoContent();
        }

        [HttpPost("{id}/dispositivos")]
        public async Task<IActionResult> AddDevice(Guid id, [FromBody] AssociateDeviceRequest request)
        {
            var success = await _comodoService.AddDeviceAsync(id, GetUserId(), request);
            if (!success) return BadRequest("Não foi possível adicionar o dispositivo (Cômodo não encontrado ou erro).");
            return Ok();
        }

        [HttpDelete("{id}/dispositivos/{dispositivoId}")]
        public async Task<IActionResult> RemoveDevice(Guid id, string dispositivoId)
        {
            var success = await _comodoService.RemoveDeviceAsync(id, dispositivoId, GetUserId());
            if (!success) return NotFound();
            return NoContent();
        }

        [HttpGet("devices/available")]
        public async Task<IActionResult> GetAvailableDevices()
        {
            var result = await _comodoService.GetAvailableDevicesAsync(GetUserId());
            return Ok(result);
        }

        [HttpPost("resolver-dispositivo")]
        public async Task<IActionResult> ResolverDispositivo([FromQuery] string tipo, [FromQuery] string? comando, [FromQuery] string? comodoConfirmado)
        {
            var result = await _comodoService.ResolverComandoAmbienteAsync(GetUserId(), tipo, comando, comodoConfirmado);
            return Ok(result);
        }

        [HttpPost("toggle-device")]
        public async Task<IActionResult> ToggleDevice([FromQuery] string dispositivoId, [FromQuery] string tipo)
        {
            var success = await _comodoService.ToggleDeviceAsync(GetUserId(), dispositivoId, tipo);
            if (!success) return BadRequest("Não foi possível alternar o estado do dispositivo.");
            return Ok();
        }

        // Endpoint para debug do escopo
        [HttpGet("escopo")]
        public async Task<IActionResult> GetEscopo()
        {
             var escopo = await _escopoService.GetEscopoAtivoAsync(GetUserId());
             return Ok(escopo);
        }
    }
}
