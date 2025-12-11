using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StarkAid.Api.Services.V1.Devices;
using System.Security.Claims;

    [ApiVersion("1.0")]
    [ApiController]
[Route("api/v{version:apiVersion}/[controller]")]
public class NotificacoesController : ControllerBase
{
    private readonly FcmNotificationService _fcm;

    public NotificacoesController(FcmNotificationService fcm)
    {
        _fcm = fcm;
    }

    [HttpPost("enviar")]
    public async Task<IActionResult> EnviarNotificacao([FromBody] NotificacaoRequest request)
    {
        await _fcm.EnviarNotificacaoAsync(request.Token, request.Titulo, request.Corpo, request.DisparoId);
        return Ok("Notificação enviada.");
    }

    [Authorize]
    [HttpPost("registrar-token")]
    public async Task<IActionResult> RegistrarToken([FromBody] RegistrarTokenRequest request)
    {
        if (string.IsNullOrEmpty(request.FcmToken))
            return BadRequest("Token inválido");

        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out Guid userId))
            return Unauthorized();

        await _fcm.SalvarTokenFcmAsync(userId, request.FcmToken);

        return Ok("Token salvo.");
    }

    public class RegistrarTokenRequest
    {
        public string FcmToken { get; set; } = string.Empty;
    }
}

public class NotificacaoRequest
{
    public string Token { get; set; } = string.Empty;
    public string Titulo { get; set; } = string.Empty;
    public string Corpo { get; set; } = string.Empty;
    public Guid DisparoId { get; set; } // 🔥 adiciona esse
}
