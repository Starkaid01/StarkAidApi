using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StarkAid.Api.DTOs.Devices;
using StarkAid.Api.Services.Devices;
using System.Security.Claims;

namespace StarkAid.Api.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class DevicesController : ControllerBase
    {
        private readonly IDeviceService _deviceService;

        public DevicesController(IDeviceService deviceService)
        {
            _deviceService = deviceService;
        }

        private Guid GetUserId()
            => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));

        [HttpGet]
        public async Task<IActionResult> GetDevices()
        {
            var devices = await _deviceService.GetByUserAsync(GetUserId());
            return Ok(devices);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetDeviceById(Guid id)
        {
            var device = await _deviceService.GetByIdAsync(id);

            if (device == null || device.UserId != GetUserId())
                return NotFound();

            return Ok(device);
        }

        [HttpPost]
        public async Task<IActionResult> CreateDevice([FromBody] CreateDeviceRequest request)
        {
            var device = await _deviceService.CreateAsync(request.Name, GetUserId(), request.Comando);
            return Created("", device);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> RenameDevice(Guid id, [FromBody] RenameDeviceRequest request)
        {
            var success = await _deviceService.RenameAsync(id, GetUserId(), request.NewName, request.NewComando);

            if (!success)
                return NotFound();

            return Ok("Device updated.");
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteDevice(Guid id)
        {
            var success = await _deviceService.DeleteAsync(id, GetUserId());

            if (!success)
                return NotFound();

            return Ok("Device deleted.");
        }

        [HttpPost("pair")]
        [AllowAnonymous]
        public async Task<IActionResult> PairDevice([FromBody] PairDeviceRequest request, [FromHeader] string apiKey)
        {
            if (string.IsNullOrWhiteSpace(apiKey))
                return BadRequest("API Key is required.");

            var (device, exists) = await _deviceService.PairAsync(apiKey, request.Name);

            if (device == null)
                return Unauthorized("Invalid API Key.");

            if (exists)
                return Ok(new { message = "Device already paired.", device });

            return Created("", new { message = "Device paired successfully.", device });
        }
    }
}
