using Microsoft.AspNetCore.SignalR;
using MQTTnet;
using MQTTnet.Client;
using MQTTnet.Formatter;
using MQTTnet.Protocol;
using StarkAid.Api.Hubs;
using System.Text;

namespace StarkAid.Api.Services;

public class MqttClientService : IMqttClientService, IAsyncDisposable
{
    private readonly IMqttClient _mqttClient;
    private readonly Dictionary<string, string> _deviceStatuses = new();
    private readonly MqttClientOptions _options;
    private bool _disposed;

    public bool IsConnected => _mqttClient.IsConnected;
    private readonly IHubContext<DeviceHub> _hubContext;

    [Obsolete]
    public MqttClientService(IHubContext<DeviceHub> hubContext)
    {
        _hubContext = hubContext;

        var factory = new MqttFactory();
        _mqttClient = factory.CreateMqttClient();

        _options = new MqttClientOptionsBuilder()
            .WithTcpServer("ec67254fbc124ef49fc159a2fec12d4d.s1.eu.hivemq.cloud", 8883)
            .WithCredentials("starkaid", "Deusmaior1424#")
            .WithTls(new MqttClientOptionsBuilderTlsParameters
            {
                UseTls = true,
                CertificateValidationHandler = context => true,
                SslProtocol = System.Security.Authentication.SslProtocols.Tls13 |
                              System.Security.Authentication.SslProtocols.Tls12
            })
            .WithProtocolVersion(MqttProtocolVersion.V311)
            .Build();

        // ✅ Registro do handler correto
        _mqttClient.ApplicationMessageReceivedAsync += MqttMessageReceived;

        _mqttClient.DisconnectedAsync += async e =>
        {
            Console.WriteLine($"🔴 MQTT disconnected: {e.Reason}");
            await Task.Delay(TimeSpan.FromSeconds(5));
            await TryConnectAsync();
        };
    }

    private async Task MqttMessageReceived(MqttApplicationMessageReceivedEventArgs e)
    {
        var topic = e.ApplicationMessage.Topic; // ex: starkaid/{userId}/{deviceId}/status
        var payload = Encoding.UTF8.GetString(e.ApplicationMessage.PayloadSegment);

        Console.WriteLine($"📡 Status recebido: {topic} => {payload}");
        _deviceStatuses[topic] = payload;

        // Extrair userId e deviceId do tópico
        var parts = topic.Split('/');
        if (parts.Length < 4) return;

        var userId = parts[1];
        var deviceId = parts[2];
        var status = payload;

        // Envia via SignalR para o app do usuário
        if (!string.IsNullOrEmpty(userId))
        {
            await _hubContext.Clients.Group(userId)
                .SendAsync("ReceiveStatus", deviceId, status);
        }
    }

    public async Task StartAsync()
    {
        _mqttClient.ConnectedAsync += async e =>
        {
            Console.WriteLine("🟢 MQTT conectado no backend.");
            await SubscribeDefaultTopicsAsync();
        };

        await TryConnectAsync();
    }

    private async Task TryConnectAsync()
    {
        if (_disposed) return;

        try
        {
            if (!_mqttClient.IsConnected)
            {
                Console.WriteLine("🔄 Tentando conectar ao MQTT...");
                await _mqttClient.ConnectAsync(_options, CancellationToken.None);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Falha ao conectar MQTT: {ex.Message}");
            await Task.Delay(10000);
            await TryConnectAsync();
        }
    }

    private async Task SubscribeDefaultTopicsAsync()
    {
        try
        {
            await _mqttClient.SubscribeAsync(new MqttTopicFilterBuilder()
                .WithTopic("starkaid/+/+/+/status")
                .WithQualityOfServiceLevel(MqttQualityOfServiceLevel.AtLeastOnce)
                .Build());

            Console.WriteLine("✅ Inscrito no tópico de status");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Falha ao inscrever: {ex.Message}");
        }
    }

    public async Task PublishAsync(string topic, string payload)
    {
        if (!_mqttClient.IsConnected)
        {
            Console.WriteLine("⚠️ MQTT desconectado. Tentando reconectar...");
            await TryConnectAsync();
        }

        try
        {
            var message = new MqttApplicationMessageBuilder()
                .WithTopic(topic)
                .WithPayload(payload)
                .WithQualityOfServiceLevel(MqttQualityOfServiceLevel.AtLeastOnce)
                .Build();

            await _mqttClient.PublishAsync(message);
            Console.WriteLine($"📤 Publicado no tópico {topic}: {payload}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Falha ao publicar: {ex.Message}");
        }
    }

    public async Task SubscribeAsync(string topic)
    {
        await _mqttClient.SubscribeAsync(new MqttTopicFilterBuilder()
            .WithTopic(topic)
            .WithQualityOfServiceLevel(MqttQualityOfServiceLevel.AtLeastOnce)
            .Build());

        Console.WriteLine($"✅ Inscrito no tópico: {topic}");
    }

    public Task<string?> GetStatusAsync(string topic)
    {
        _deviceStatuses.TryGetValue(topic, out var status);
        return Task.FromResult(status);
    }

    public async ValueTask DisposeAsync()
    {
        if (_disposed) return;

        try
        {
            if (_mqttClient.IsConnected)
            {
                Console.WriteLine("🔻 Desconectando MQTT client...");
                await _mqttClient.DisconnectAsync();
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"⚠️ Erro ao desconectar MQTT: {ex.Message}");
        }

        _disposed = true;
    }
}
