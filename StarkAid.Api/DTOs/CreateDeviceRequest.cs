namespace StarkAid.Api.DTOs;

public class CreateDeviceRequest
{
    public string Name { get; set; } = string.Empty;
    public string Comando { get; set; } = string.Empty;
}