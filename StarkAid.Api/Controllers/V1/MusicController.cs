using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using StarkAid.Api.Services;
using StarkAid.Api.Services.V1.Music;
using StarkAid.Api.DTOs;
using StarkAid.Api.DTOs.V1.Music;
using StarkAid.Api.Data;

namespace StarkAid.Api.Controllers.V1
{
    [ApiController]
    [Route("api/v1/[controller]")]
    public class MusicController : ControllerBase
    {
        private readonly IMusicIntentService _musicIntentService;
        private readonly ITokenUsageService _tokenUsage;
        private readonly AppDbContext _context;
        private readonly ILogger<MusicController> _logger;
        private readonly Services.PlanoLimitesService _planoLimites;

        public MusicController(IMusicIntentService musicIntentService, ITokenUsageService tokenUsage, AppDbContext context, Services.PlanoLimitesService planoLimites, ILogger<MusicController> logger)
        {
            _musicIntentService = musicIntentService;
            _tokenUsage = tokenUsage;
            _context = context;
            _planoLimites = planoLimites;
            _logger = logger;
        }

        [Authorize]
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
