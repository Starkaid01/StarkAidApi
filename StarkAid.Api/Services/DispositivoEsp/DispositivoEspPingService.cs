using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using StarkAid.Api.Data;
using StarkAid.Api.Hubs;
using System.Net.NetworkInformation;

namespace StarkAid.Api.Services.DispositivoEsp;

public class DispositivoEspPingService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<DispositivoEspPingService> _logger;
    private readonly TimeSpan _pingInterval = TimeSpan.FromMinutes(5); // Ping a cada 5 minutos

    public DispositivoEspPingService(
        IServiceProvider serviceProvider,
        ILogger<DispositivoEspPingService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Serviço de ping de DispositivosESP iniciado");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await PingAllDispositivos();
                await Task.Delay(_pingInterval, stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao executar ping de dispositivos");
                await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken); // Espera 1 minuto em caso de erro
            }
        }
    }

    private async Task PingAllDispositivos()
    {
        using var scope = _serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var hubContext = scope.ServiceProvider.GetRequiredService<IHubContext<DispositivoEspHub>>();

        var dispositivos = await context.DispositivosEsp.ToListAsync();

        _logger.LogInformation("Fazendo ping em {Count} dispositivos", dispositivos.Count);

        foreach (var dispositivo in dispositivos)
        {
            try
            {
                var isOnline = await PingDispositivo(dispositivo.Ip);
                var novoStatus = isOnline ? "Conectado" : "Desconectado";

                if (dispositivo.Status != novoStatus)
                {
                    dispositivo.Status = novoStatus;
                    dispositivo.LastPingAt = DateTimeOffset.UtcNow;
                    await context.SaveChangesAsync();

                    // Notifica via WebSocket usando o mesmo formato do Hub
                    await hubContext.Clients.All.SendAsync("StatusDispositivoAtualizado", new
                    {
                        nome = dispositivo.Nome,
                        ip = dispositivo.Ip,
                        status = dispositivo.Status,
                        ligadoDesligado = dispositivo.LigadoDesligado
                    });

                    _logger.LogInformation("Status do dispositivo {Nome} atualizado para {Status}", dispositivo.Nome, novoStatus);
                }
                else
                {
                    dispositivo.LastPingAt = DateTimeOffset.UtcNow;
                    await context.SaveChangesAsync();
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Erro ao fazer ping no dispositivo {Nome} ({Ip})", dispositivo.Nome, dispositivo.Ip);
            }
        }
    }

    private async Task<bool> PingDispositivo(string ip)
    {
        try
        {
            using var ping = new System.Net.NetworkInformation.Ping();
            var reply = await ping.SendPingAsync(ip, 3000);
            return reply.Status == System.Net.NetworkInformation.IPStatus.Success;
        }
        catch
        {
            return false;
        }
    }
}

