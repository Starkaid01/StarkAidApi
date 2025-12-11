namespace StarkAid.Api.DTOs.V1.Devices;

public class CreateDeviceRequest
{
    public string Name { get; set; } = string.Empty;
    public string Comando { get; set; } = string.Empty;
}