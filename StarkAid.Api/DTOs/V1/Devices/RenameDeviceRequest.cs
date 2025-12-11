namespace StarkAid.Api.DTOs.V1.Devices
{
    public class RenameDeviceRequest
    {
        public string NewName { get; set; } = string.Empty;
        public string NewComando { get; set; } = string.Empty;
    }
}