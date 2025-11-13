namespace StarkAid.Api.Services.Devices;

public interface IMqttClientService : IAsyncDisposable
{
    Task PublishAsync(string topic, string payload);
    Task SubscribeAsync(string topic);
    Task<string?> GetStatusAsync(string topic);
    bool IsConnected { get; }

    Task StartAsync();
}