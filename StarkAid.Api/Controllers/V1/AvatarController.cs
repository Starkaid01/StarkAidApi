using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using StarkAid.Api.Hubs;
using System.Security.Claims;

namespace StarkAid.Api.Controllers.V1;

[ApiVersion("1.0")]
[ApiController]
[Route("api/v{version:apiVersion}/[controller]")]
public class AvatarController : ControllerBase
{
    private readonly IHubContext<AvatarHub> _avatarHub;

    public AvatarController(IHubContext<AvatarHub> avatarHub)
    {
        _avatarHub = avatarHub;
    }

    private string ResolveSessionId(string? requestedSessionId)
    {
        var sessionId = string.IsNullOrWhiteSpace(requestedSessionId) ? null : requestedSessionId.Trim();
        if (!string.IsNullOrWhiteSpace(sessionId))
        {
            return sessionId;
        }

        var userId = User?.FindFirstValue(ClaimTypes.NameIdentifier) ??
                     User?.FindFirstValue("sub");

        if (!string.IsNullOrWhiteSpace(userId))
        {
            return userId;
        }

        return "default";
    }

    [AllowAnonymous]
    [HttpPost("animate-from-text")]
    public async Task<IActionResult> AnimateAvatarFromText([FromBody] AnimateAvatarFromTextRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Text))
        {
            return BadRequest(new { error = "Texto não informado." });
        }

        var sessionId = ResolveSessionId(request.SessionId);

        await _avatarHub.Clients.Group(sessionId).SendAsync("AnimateFromText", request.Text, cancellationToken);

        return Ok(new { status = "ok", sessionId });
    }

    [AllowAnonymous]
    [HttpPost("stop-speaking")]
    public async Task<IActionResult> StopSpeaking([FromBody] StopSpeakingRequest? request, CancellationToken cancellationToken)
    {
        var sessionId = ResolveSessionId(request?.SessionId);

        await _avatarHub.Clients.Group(sessionId).SendAsync("StopSpeaking", Array.Empty<object?>(), cancellationToken);

        return Ok(new { status = "ok", sessionId });
    }

    [HttpGet("avatar/base64")]
    [AllowAnonymous]
    public async Task<IActionResult> GetAvatarBase64()
    {
        var path = Path.Combine(
            Directory.GetCurrentDirectory(),
            "wwwroot/base64avatar/base64.txt");

        var base64 = await System.IO.File.ReadAllTextAsync(path);
        return Content(base64, "text/plain");
    }
}

public sealed class AnimateAvatarFromTextRequest
{
    public string Text { get; set; } = string.Empty;
    public string? SessionId { get; set; }
}

public sealed class StopSpeakingRequest
{
    public string? SessionId { get; set; }
}
