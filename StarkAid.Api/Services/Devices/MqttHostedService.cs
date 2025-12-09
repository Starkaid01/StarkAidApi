using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;

namespace StarkAid.Api.Services.Devices;

/// <summary>
/// Serviço hospedado que inicia o cliente MQTT quando a aplicação sobe.
/// </summary>
public class MqttHostedService : IHostedService
{
    private readonly IMqttClientService _mqttService;

    public MqttHostedService(IMqttClientService mqttService) => _mqttService = mqttService;

    public Task StartAsync(CancellationToken cancellationToken) =>
        _mqttService.StartAsync();

    public Task StopAsync(CancellationToken cancellationToken) =>
        _mqttService.DisposeAsync().AsTask();
}
