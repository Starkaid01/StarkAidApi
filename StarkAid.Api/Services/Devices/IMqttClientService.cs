using System;
using System.Threading.Tasks;

namespace StarkAid.Api.Services.Devices;

/// <summary>
/// Interface responsável pela comunicação MQTT.
/// </summary>
public interface IMqttClientService : IAsyncDisposable
{
    Task PublishAsync(string topic, string payload);
    Task SubscribeAsync(string topic);
    Task<string?> GetStatusAsync(string topic);
    bool IsConnected { get; }
    Task StartAsync();
}
