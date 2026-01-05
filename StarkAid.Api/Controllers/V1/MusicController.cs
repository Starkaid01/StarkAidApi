using Microsoft.AspNetCore.Mvc;
using StarkAid.Api.Services.V1.Music;
using StarkAid.Api.DTOs.V1.Music;

namespace StarkAid.Api.Controllers.V1
{
    [ApiController]
    [Route("api/v1/[controller]")]
    public class MusicController : ControllerBase
    {
        private readonly IMusicIntentService _musicIntentService;
        private readonly ILogger<MusicController> _logger;

        public MusicController(IMusicIntentService musicIntentService, ILogger<MusicController> logger)
        {
            _musicIntentService = musicIntentService;
            _logger = logger;
        }

        [HttpPost("resolve")]
        public async Task<IActionResult> Resolve([FromBody] MusicResolveRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Text))
                return BadRequest("Text is required");

            _logger.LogInformation("Music resolve request: {Text}", request.Text);

            var response = await _musicIntentService.ResolveIntentAsync(request.Text);

            return Ok(response);
        }

        [HttpGet("online/stream/{externalId}")]
        public async Task<IActionResult> GetAudioStream([FromRoute] string externalId, [FromServices] IExternalAudioResolver audioResolver)
        {
            if (string.IsNullOrWhiteSpace(externalId))
                return BadRequest("External ID is required");

            // Feature Flag (Hardcoded for now as requested: Music.EnableOnlineFallback check simulated)
            bool enableOnlineFallback = true; 
            if (!enableOnlineFallback)
            {
                return NotFound("Online fallback disabled.");
            }

            try 
            {
                var result = await audioResolver.GetAudioStreamUrlAsync(externalId);
                
                if (result == null)
                {
                    _logger.LogWarning("Resolver returned null for ID: {Id}", externalId);
                    return NotFound("Stream not found or unavailable.");
                }

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Controller error resolving stream for ID: {Id}", externalId);
                return StatusCode(500, $"Internal error resolving stream: {ex.Message}");
            }
        }
    }
}
