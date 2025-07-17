using StarkAid.Api.Services;

public class AgendamentoWorker : BackgroundService
{
    private readonly IServiceProvider _services;

    public AgendamentoWorker(IServiceProvider services)
    {
        _services = services;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            using var scope = _services.CreateScope();
            var agendamentoService = scope.ServiceProvider.GetRequiredService<AgendamentoService>();
            var mqttClient = scope.ServiceProvider.GetRequiredService<IMqttClientService>(); // <-- Interface aqui
            var deviceService = scope.ServiceProvider.GetRequiredService<DeviceService>();

            var pendentes = await agendamentoService.BuscarAgendamentosPendentesAsync();

            foreach (var agendamento in pendentes)
            {
                var device = await deviceService.GetByIdAsync(agendamento.DeviceId);
                if (device != null)
                {
                    await mqttClient.PublishAsync(device.MqttTopic, agendamento.Comando);

                    if (string.IsNullOrEmpty(agendamento.Recorrencia) || agendamento.Recorrencia == "Nenhum")
                    {
                        await agendamentoService.MarcarComoExecutadoAsync(agendamento.Id);
                    }
                    else
                    {
                        // Reagendar
                        var novoAgendamento = agendamento.AgendadoPara;
                        switch (agendamento.Recorrencia)
                        {
                            case "Diario":
                                novoAgendamento = novoAgendamento.AddDays(1);
                                break;
                            case "Semanal":
                                novoAgendamento = novoAgendamento.AddDays(7);
                                break;
                                // outros casos se quiser
                        }

                        agendamento.AgendadoPara = novoAgendamento;
                        await agendamentoService.AtualizarAgendamentoAsync(agendamento);
                    }
                }
            }

            await Task.Delay(1000, stoppingToken); // verifica a cada 1 segundo
        }
    }
}