using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StarkAid.Api.DTOs;
using StarkAid.Api.Services;
using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace StarkAid.Api.Controllers
{
    [ApiController]    
    [Route("api/[controller]")]
    public class DevicesController : ControllerBase
    {
        private readonly DeviceService _deviceService;

        public DevicesController(DeviceService deviceService)
        {
            _deviceService = deviceService;
        }

        // GET: /api/Devices
        [Authorize]
        [Authorize(Policy = "UserNivel2Only")]
        [HttpGet]
        public async Task<IActionResult> GetDevices()
        {
            var apiKeyFromHeader = Request.Headers["Api-Key"].FirstOrDefault();

            var authHeader = Request.Headers["Authorization"].FirstOrDefault();
            Console.WriteLine($"AuthHeader: {authHeader}, ApiKey: {apiKeyFromHeader}");


            if (string.IsNullOrEmpty(apiKeyFromHeader))
                return Unauthorized("ApiKey obrigatória.");

            var userIdStr = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrEmpty(userIdStr) || !Guid.TryParse(userIdStr, out var userId))
                return Unauthorized("Usuário inválido ou token corrompido.");

            // Pega o usuário no banco pelo Id
            var user = await _deviceService.GetUserByIdAsync(userId);
            if (user == null)
                return Unauthorized("Usuário não encontrado.");

            if (user.ApiKey != apiKeyFromHeader)
                return Unauthorized("ApiKey inválida.");

            var devices = await _deviceService.GetDevicesByUserIdAsync(userId);

            var result = devices.Select(d => new
            {
                d.Id,
                d.Name,
                d.MqttTopic
            });

            return Ok(result);
        }

        [Authorize]
        [Authorize(Policy = "UserNivel2Only")]
        [HttpPut("{deviceId}/Rename")]
        public async Task<IActionResult> RenameDevice(Guid deviceId, [FromBody] RenameDeviceRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.NewName))
                return BadRequest("Novo nome obrigatório.");

            var apiKeyFromHeader = Request.Headers["Api-Key"].FirstOrDefault();
            if (string.IsNullOrEmpty(apiKeyFromHeader))
                return Unauthorized("ApiKey obrigatória.");

            var userIdStr = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdStr) || !Guid.TryParse(userIdStr, out var userId))
                return Unauthorized("Token inválido.");

            var user = await _deviceService.GetUserByIdAsync(userId);
            if (user == null)
                return Unauthorized("Usuário não encontrado.");

            if (user.ApiKey != apiKeyFromHeader)
                return Unauthorized("ApiKey inválida.");

            var updated = await _deviceService.RenameDeviceAsync(deviceId, userId, request.NewName);
            if (!updated)
                return NotFound("Dispositivo não encontrado ou não pertence ao usuário.");

            return Ok("Nome do dispositivo atualizado com sucesso.");
        }

        [HttpPost("Pair")]
        public async Task<IActionResult> PairDevice([FromQuery] string apiKey, [FromBody] PairDeviceRequest request)
        {
            if (string.IsNullOrEmpty(apiKey))
                return Unauthorized("API Key não fornecida.");

            var (device, exists) = await _deviceService.PairDeviceAsync(apiKey, request.Name);

            if (device == null)
                return Unauthorized("ApiKey inválida.");

            return exists
                ? Ok(new
                {
                    deviceId = device.Id,
                    userId = device.UserId,
                    mqttTopic = device.MqttTopic
                })
                : Created("", new
                {
                    deviceId = device.Id,
                    userId = device.UserId,
                    mqttTopic = device.MqttTopic
                });
        }

        [Authorize]
        [Authorize(Policy = "UserNivel2Only")]
        [HttpPost]
        public async Task<IActionResult> CreateDevice([FromBody] CreateDeviceRequest request)
        {
            var userIdStr = User.Claims.FirstOrDefault(c => c.Type == System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrEmpty(userIdStr) || !Guid.TryParse(userIdStr, out var userId))
            {
                return Unauthorized("Claim nameidentifier não encontrada ou inválida no token.");
            }

            var device = await _deviceService.CreateDeviceAsync(request.Name, userId);

            var response = new DeviceResponseDto
            {
                Id = device.Id,
                Name = device.Name,
                ApiKey = device.ApiKey,
                MqttTopic = device.MqttTopic
            };

            return Ok(response);
        }

        [Authorize]
        [Authorize(Policy = "UserNivel2Only")]
        [HttpDelete("{deviceId}")]
        public async Task<IActionResult> DeleteDevice(Guid deviceId)
        {
            var apiKeyFromHeader = Request.Headers["Api-Key"].FirstOrDefault();
            if (string.IsNullOrEmpty(apiKeyFromHeader))
                return Unauthorized("ApiKey obrigatória.");

            var userIdStr = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdStr) || !Guid.TryParse(userIdStr, out var userId))
                return Unauthorized("Token inválido.");

            var user = await _deviceService.GetUserByIdAsync(userId);
            if (user == null)
                return Unauthorized("Usuário não encontrado.");

            if (user.ApiKey != apiKeyFromHeader)
                return Unauthorized("ApiKey inválida.");

            var deleted = await _deviceService.DeleteDeviceAsync(deviceId, userId);
            if (!deleted)
                return NotFound("Dispositivo não encontrado ou não pertence ao usuário.");

            return Ok("Dispositivo removido com sucesso.");
        }
    }
}
