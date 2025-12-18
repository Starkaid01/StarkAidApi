namespace StarkAid.Web.DTOs
{
    public class EwelinkStatusResponse
    {
        public bool IsConnected { get; set; }
        public string Message { get; set; } = string.Empty;
        public List<DeviceDto> Devices { get; set; } = new();
    }
}
