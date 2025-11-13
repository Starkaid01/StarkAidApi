using StarkAid.Api.Entities;
using StarkAid.Api.Services.Devices;

public class AgendamentoWorker : BackgroundService
{
    private readonly IServiceProvider _services;
    private readonly ILogger<AgendamentoWorker> _logger;
    private const int IntervaloVerificacaoMs = 60000;

    public AgendamentoWorker(IServiceProvider services, ILogger<AgendamentoWorker> logger)
    {
        _services = services;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("AgendamentoWorker iniciado.");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _services.CreateScope();
                var agendamentoService = scope.ServiceProvider.GetRequiredService<AgendamentoService>();
                var mqttClient = scope.ServiceProvider.GetRequiredService<IMqttClientService>();
                var deviceService = scope.ServiceProvider.GetRequiredService<DeviceService>();

                var pendentes = await agendamentoService.BuscarAgendamentosPendentesAsync();

                foreach (var agendamento in pendentes)
                {
                    try
                    {
                        var device = await deviceService.GetByIdAsync(agendamento.DeviceId);
                        if (device == null) continue;

                        await mqttClient.PublishAsync(device.MqttTopic, agendamento.Comando);

                        if (string.IsNullOrEmpty(agendamento.Recorrencia) || agendamento.Recorrencia == "Nenhum")
                        {
                            await agendamentoService.MarcarComoExecutadoAsync(agendamento.Id);
                        }
                        else
                        {
                            var novoAgendamento = CalcularProximoAgendamento(agendamento);
                            agendamento.AgendadoPara = novoAgendamento;
                            await agendamentoService.AtualizarAgendamentoAsync(agendamento);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Erro agendamento {Id}", agendamento.Id);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogCritical(ex, "Erro no AgendamentoWorker");
            }

            try
            {
                await Task.Delay(IntervaloVerificacaoMs, stoppingToken);
            }
            catch (TaskCanceledException) { break; }
        }

        _logger.LogInformation("AgendamentoWorker finalizado.");
    }

    private DateTimeOffset CalcularProximoAgendamento(Agendamento agendamento) => agendamento.Recorrencia switch
    {
        "Diario" => agendamento.AgendadoPara.AddDays(1),
        "Semanal" => agendamento.AgendadoPara.AddDays(7),
        "Mensal" => agendamento.AgendadoPara.AddMonths(1),
        "Anual" => agendamento.AgendadoPara.AddYears(1),
        _ => agendamento.AgendadoPara.AddDays(1)
    };

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("AgendamentoWorker está sendo finalizado...");
        await base.StopAsync(cancellationToken);
    }
}
