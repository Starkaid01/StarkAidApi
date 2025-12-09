using System;
using System.Collections.Concurrent;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using MQTTnet;
using MQTTnet.Client;
using MQTTnet.Formatter;
using MQTTnet.Protocol;
using StarkAid.Api.Hubs;

namespace StarkAid.Api.Services.Devices;

/// <summary>
/// Cliente MQTT singleton que gerencia publicação/subscrição e integração com SignalR.
/// </summary>
public class MqttClientService : IMqttClientService
{
    private readonly IMqttClient _client;
    private readonly IHubContext<DeviceHub> _hubContext;
    private readonly ILogger<MqttClientService> _logger;
    private readonly ConcurrentDictionary<string, string> _statusCache = new();
    private readonly MqttClientOptions _options;
    private bool _disposed;

    public bool IsConnected => _client.IsConnected;

    public MqttClientService(
        IHubContext<DeviceHub> hubContext,
        IConfiguration configuration,
        ILogger<MqttClientService> logger)
    {
        _hubContext = hubContext;
        _logger = logger;

        var factory = new MqttFactory();
        _client = factory.CreateMqttClient();

        // Configurações obtidas de appsettings (Mqtt:*)
        var mqttSection = configuration.GetSection("Mqtt");
        var broker = mqttSection["Broker"];
        var port = int.Parse(mqttSection["Port"]);
        var username = mqttSection["Username"];
        var password = mqttSection["Password"];

        _options = new MqttClientOptionsBuilder()
            .WithTcpServer(broker, port)
            .WithCredentials(username, password)
            .WithTls()
            .WithProtocolVersion(MqttProtocolVersion.V311)
            .Build();

        // Handlers
        _client.ApplicationMessageReceivedAsync += OnMessageReceivedAsync;
        _client.DisconnectedAsync += async e =>
        {
            _logger.LogWarning("MQTT desconectado. Tentando reconectar em 5s...");
            await Task.Delay(TimeSpan.FromSeconds(5));
            await TryConnectAsync();
        };
    }

    private async Task OnMessageReceivedAsync(MqttApplicationMessageReceivedEventArgs e)
    {
        var topic = e.ApplicationMessage.Topic;
        var payload = Encoding.UTF8.GetString(e.ApplicationMessage.PayloadSegment);
        _statusCache[topic] = payload;

        var parts = topic.Split('/');
        if (parts.Length < 4) return;

        var userId = parts[1];
        var deviceId = parts[2];
        await _hubContext.Clients.Group(userId).SendAsync("ReceiveStatus", deviceId, payload);
        _logger.LogInformation("Status recebido: {Topic} => {Payload}", topic, payload);
    }

    public async Task StartAsync()
    {
        _client.ConnectedAsync += async _ =>
        {
            _logger.LogInformation("MQTT conectado com sucesso.");
            await SubscribeAsync("starkaid/+/+/+/status");
        };

        await TryConnectAsync();
    }

    private async Task TryConnectAsync()
    {
        if (_disposed) return;

        try
        {
            if (!_client.IsConnected)
            {
                _logger.LogInformation("Tentando conectar ao broker MQTT...");
                await _client.ConnectAsync(_options, CancellationToken.None);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Falha ao conectar ao broker MQTT.");
            await Task.Delay(TimeSpan.FromSeconds(10));
            await TryConnectAsync();
        }
    }

    public async Task PublishAsync(string topic, string payload)
    {
        if (!IsConnected) await TryConnectAsync();

        var message = new MqttApplicationMessageBuilder()
            .WithTopic(topic)
            .WithPayload(payload)
            .WithQualityOfServiceLevel(MqttQualityOfServiceLevel.AtLeastOnce)
            .Build();

        await _client.PublishAsync(message);
        _logger.LogInformation("Publicação MQTT: {Topic} => {Payload}", topic, payload);
    }

    public async Task SubscribeAsync(string topic)
    {
        await _client.SubscribeAsync(new MqttTopicFilterBuilder()
            .WithTopic(topic)
            .WithQualityOfServiceLevel(MqttQualityOfServiceLevel.AtLeastOnce)
            .Build());

        _logger.LogInformation("Subscrição ao tópico MQTT: {Topic}", topic);
    }

    public Task<string?> GetStatusAsync(string topic) =>
        Task.FromResult(_statusCache.TryGetValue(topic, out var value) ? value : null);

    public async ValueTask DisposeAsync()
    {
        if (_disposed) return;
        _disposed = true;

        if (_client.IsConnected)
        {
            await _client.DisconnectAsync();
        }

        _client.Dispose();
    }
}
