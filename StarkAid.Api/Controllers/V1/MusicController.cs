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
    }
}
