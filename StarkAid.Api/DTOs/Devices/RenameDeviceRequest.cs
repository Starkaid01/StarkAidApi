namespace StarkAid.Api.DTOs.Devices
{
    public class RenameDeviceRequest
    {
        public string NewName { get; set; } = string.Empty;
        public string NewComando { get; set; } = string.Empty;
    }
}