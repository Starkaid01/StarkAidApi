using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using StarkAid.Api.Services.V1.Rotinas;

namespace StarkAid.Api.Services.V1.Rotinas
{
    public class RoutineSchedulerService : BackgroundService
    {
        private readonly ILogger<RoutineSchedulerService> _logger;
        private readonly IServiceProvider _serviceProvider;
        private DateTime _lastProcessedMinute = DateTime.MinValue;

        public RoutineSchedulerService(
            IServiceProvider serviceProvider,
            ILogger<RoutineSchedulerService> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("RoutineSchedulerService iniciado.");

            while (!stoppingToken.IsCancellationRequested)
            {
                var agora = DateTimeOffset.UtcNow; // Ajustar para o fuso do usuário se necessário, mas por enquanto UTC ou local do servidor.
                // Na especificação, Expressao é HH:mm. Vamos usar o horário local do servidor (que geralmente é configurado pelo usuário).
                // Mas DateTimeOffset.Now é mais seguro para fuso local.
                var agoraLocal = DateTime.Now;

                // Evita processar o mesmo minuto mais de uma vez
                if (agoraLocal.Minute != _lastProcessedMinute.Minute || agoraLocal.Hour != _lastProcessedMinute.Hour || agoraLocal.Date != _lastProcessedMinute.Date)
                {
                    try
                    {
                        using var scope = _serviceProvider.CreateScope();
                        var rotinaService = scope.ServiceProvider.GetRequiredService<IRotinaService>();

                        await rotinaService.ProcessarGatilhosTempoAsync(agoraLocal);
                        _lastProcessedMinute = agoraLocal;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Erro ao processar gatilhos de tempo de rotina.");
                    }
                }

                try
                {
                    // Aguarda 30 segundos
                    await Task.Delay(30000, stoppingToken);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
            }

            _logger.LogInformation("RoutineSchedulerService finalizado.");
        }
    }
}
