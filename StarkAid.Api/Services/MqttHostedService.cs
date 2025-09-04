using Microsoft.Extensions.Hosting;
using StarkAid.Api.Services;
using System.Threading;
using System.Threading.Tasks;

public class MqttHostedService : IHostedService
{
    private readonly IMqttClientService _mqttClientService;

    public MqttHostedService(IMqttClientService mqttClientService)
    {
        _mqttClientService = mqttClientService;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        await _mqttClientService.StartAsync();
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        await _mqttClientService.DisposeAsync();
    }
}
