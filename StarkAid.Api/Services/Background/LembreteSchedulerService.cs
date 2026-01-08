using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using StarkAid.Api.Services.V1.Lembretes;

namespace StarkAid.Api.Services.Background
{
    public class LembreteSchedulerService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<LembreteSchedulerService> _logger;

        public LembreteSchedulerService(IServiceProvider serviceProvider, ILogger<LembreteSchedulerService> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("LembreteSchedulerService iniciado.");
            
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    using (var scope = _serviceProvider.CreateScope())
                    {
                        var service = scope.ServiceProvider.GetRequiredService<ILembreteService>();
                        await service.ProcessarLembretesPendentesAsync();
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Erro ao processar lembretes no scheduler.");
                }

                // Aguarda 30 segundos
                await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);
            }
        }
    }
}
