using MQTTnet;
using MQTTnet.Client;
using MQTTnet.Formatter;
using MQTTnet.Protocol;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace StarkAid.Api.Services;

public class MqttClientService : IMqttClientService
{
    private readonly IMqttClient _mqttClient;
    private readonly Dictionary<string, string> _deviceStatuses = new();
    private readonly MqttClientOptions _options;

    public bool IsConnected => _mqttClient.IsConnected;

    public MqttClientService()
    {
        var factory = new MqttFactory();
        _mqttClient = factory.CreateMqttClient();

        _options = new MqttClientOptionsBuilder()
            .WithTcpServer("ec67254fbc124ef49fc159a2fec12d4d.s1.eu.hivemq.cloud", 8883)
            .WithCredentials("starkaid", "Deusmaior1424#")
            .WithTlsOptions(new MqttClientTlsOptions
            {
                UseTls = true,
                AllowUntrustedCertificates = true,
                IgnoreCertificateChainErrors = true,
                IgnoreCertificateRevocationErrors = true
            })
            .WithProtocolVersion(MqttProtocolVersion.V311)
            .Build();

        _mqttClient.ApplicationMessageReceivedAsync += e =>
        {
            var topic = e.ApplicationMessage.Topic;
            var payload = Encoding.UTF8.GetString(e.ApplicationMessage.Payload.ToArray());
            Console.WriteLine($"📡 Status recebido: {topic} => {payload}");
            _deviceStatuses[topic] = payload;
            return Task.CompletedTask;
        };
    }

    public async Task StartAsync()
    {
        _mqttClient.ConnectedAsync += async e =>
        {
            Console.WriteLine("🟢 MQTT conectado no backend.");
            await _mqttClient.SubscribeAsync(new MqttTopicFilterBuilder()
                .WithTopic("starkaid/+/+/+/status")
                .WithQualityOfServiceLevel(MqttQualityOfServiceLevel.AtLeastOnce)
                .Build());

            Console.WriteLine("✅ Inscrito no tópico de status");
        };

        await _mqttClient.ConnectAsync(_options);
    }

    public async Task PublishAsync(string topic, string payload)
    {
        var message = new MqttApplicationMessageBuilder()
            .WithTopic(topic)
            .WithPayload(payload)
            .WithQualityOfServiceLevel(MqttQualityOfServiceLevel.AtLeastOnce)
            .Build();

        await _mqttClient.PublishAsync(message);
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
}