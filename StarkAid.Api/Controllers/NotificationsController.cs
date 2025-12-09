using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StarkAid.Api.Data;
using StarkAid.Api.Services.Notifications;
using System.Security.Claims;

namespace StarkAid.Api.Controllers;

[ApiController]
[Route("api/notifications")]
[Authorize]
public class NotificationsController : ControllerBase
{
    private readonly NotificationService _notificationService;
    private readonly AppDbContext _context;
    private readonly ILogger<NotificationsController> _logger;

    public NotificationsController(
        NotificationService notificationService,
        AppDbContext context,
        ILogger<NotificationsController> logger)
    {
        _notificationService = notificationService;
        _context = context;
        _logger = logger;
    }

    private Guid? GetUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
        {
            return null;
        }
        return userId;
    }

    private bool IsAdministrator()
    {
        var role = User.FindFirst(ClaimTypes.Role)?.Value;
        return role == "Administrador";
    }

    [HttpGet]
    public async Task<IActionResult> GetNotifications()
    {
        if (!IsAdministrator())
        {
            return Forbid();
        }

        var notifications = await _notificationService.ObterTodasNotificacoesAsync();
        return Ok(notifications);
    }

    [HttpGet("unread-count")]
    public async Task<IActionResult> GetUnreadCount()
    {
        if (!IsAdministrator())
        {
            return Forbid();
        }

        var count = await _notificationService.ObterContadorNaoLidasAsync();
        return Ok(new { count });
    }

    [HttpGet("unread")]
    public async Task<IActionResult> GetUnreadNotifications()
    {
        if (!IsAdministrator())
        {
            return Forbid();
        }

        var notifications = await _notificationService.ObterNotificacoesNaoLidasAsync();
        return Ok(notifications);
    }

    [HttpPost("{id}/mark-as-read")]
    public async Task<IActionResult> MarkAsRead(Guid id)
    {
        if (!IsAdministrator())
        {
            return Forbid();
        }

        await _notificationService.MarcarComoLidaAsync(id);
        return Ok(new { message = "Notificação marcada como lida" });
    }

    [HttpPost("mark-all-as-read")]
    public async Task<IActionResult> MarkAllAsRead()
    {
        if (!IsAdministrator())
        {
            return Forbid();
        }

        await _notificationService.MarcarTodasComoLidasAsync();
        return Ok(new { message = "Todas as notificações foram marcadas como lidas" });
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteNotification(Guid id)
    {
        if (!IsAdministrator())
        {
            return Forbid();
        }

        var removed = await _notificationService.RemoverNotificacaoAsync(id);
        if (!removed)
        {
            return NotFound(new { message = "Notificação não encontrada" });
        }

        return Ok(new { message = "Notificação removida com sucesso" });
    }
}
