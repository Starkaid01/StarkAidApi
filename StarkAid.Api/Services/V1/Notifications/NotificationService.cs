using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using StarkAid.Api.Data;
using StarkAid.Api.Entities;
using StarkAid.Api.Services.V1.Email;

namespace StarkAid.Api.Services.V1.Notifications;

public class NotificationService
{
    private readonly AppDbContext _context;
    private readonly IEmailService _emailService;
    private readonly ILogger<NotificationService> _logger;
    private const string ADMIN_EMAIL = "starkaid24@gmail.com";

    public NotificationService(AppDbContext context, IEmailService emailService, ILogger<NotificationService> logger)
    {
        _context = context;
        _emailService = emailService;
        _logger = logger;
    }

    public async Task<Notification> CriarNotificacaoAsync(
        string tipo,
        string titulo,
        string mensagem,
        Guid? userId = null,
        string? userEmail = null,
        string? userName = null,
        decimal? valor = null,
        string? referenciaId = null)
    {
        var notification = new Notification
        {
            Id = Guid.NewGuid(),
            Tipo = tipo,
            Titulo = titulo,
            Mensagem = mensagem,
            UserId = userId,
            UserEmail = userEmail,
            UserName = userName,
            Valor = valor,
            ReferenciaId = referenciaId,
            Lida = false,
            CreatedAt = DateTimeOffset.UtcNow
        };

        _context.Notifications.Add(notification);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Notificação criada: {Tipo} - {Titulo}", tipo, titulo);

        // Enviar email para administrador
        try
        {
            var emailBody = $"Nova notificação: {titulo}\n\n";
            emailBody += $"Tipo: {tipo}\n";
            if (!string.IsNullOrEmpty(userName))
                emailBody += $"Usuário: {userName}\n";
            if (!string.IsNullOrEmpty(userEmail))
                emailBody += $"Email: {userEmail}\n";
            if (valor.HasValue)
                emailBody += $"Valor: R$ {valor.Value:F2}\n";
            emailBody += $"\n{mensagem}";

            await _emailService.SendAsync(ADMIN_EMAIL, $"StarkAid - {titulo}", emailBody);
            _logger.LogInformation("Email de notificação enviado para {Email}", ADMIN_EMAIL);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao enviar email de notificação");
        }

        return notification;
    }

    public async Task<List<Notification>> ObterNotificacoesNaoLidasAsync()
    {
        return await _context.Notifications
            .Where(n => !n.Lida)
            .OrderByDescending(n => n.CreatedAt)
            .ToListAsync();
    }

    public async Task<List<Notification>> ObterTodasNotificacoesAsync(int limit = 50)
    {
        return await _context.Notifications
            .OrderByDescending(n => n.CreatedAt)
            .Take(limit)
            .ToListAsync();
    }

    public async Task<int> ObterContadorNaoLidasAsync()
    {
        return await _context.Notifications
            .CountAsync(n => !n.Lida);
    }

    public async Task MarcarComoLidaAsync(Guid notificationId)
    {
        var notification = await _context.Notifications.FindAsync(notificationId);
        if (notification != null && !notification.Lida)
        {
            notification.Lida = true;
            notification.LidaEm = DateTimeOffset.UtcNow;
            await _context.SaveChangesAsync();
        }
    }

    public async Task MarcarTodasComoLidasAsync()
    {
        var naoLidas = await _context.Notifications
            .Where(n => !n.Lida)
            .ToListAsync();

        foreach (var notification in naoLidas)
        {
            notification.Lida = true;
            notification.LidaEm = DateTimeOffset.UtcNow;
        }

        await _context.SaveChangesAsync();
    }

    public async Task<bool> RemoverNotificacaoAsync(Guid notificationId)
    {
        var notification = await _context.Notifications.FindAsync(notificationId);
        if (notification == null)
        {
            return false;
        }

        _context.Notifications.Remove(notification);
        await _context.SaveChangesAsync();
        
        _logger.LogInformation("Notificação removida: {Id}", notificationId);
        return true;
    }
}
