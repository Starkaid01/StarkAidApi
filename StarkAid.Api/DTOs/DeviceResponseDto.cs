namespace StarkAid.Api.DTOs;

public class DeviceResponseDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string ApiKey { get; set; } = string.Empty;
    public string MqttTopic { get; set; } = string.Empty;
    public string Comando { get; set; } = string.Empty;
}