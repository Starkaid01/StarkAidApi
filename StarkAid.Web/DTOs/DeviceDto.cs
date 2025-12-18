namespace StarkAid.Web.DTOs
{
    public class DeviceDto
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public bool Online { get; set; }
        public bool IsOn { get; set; }
        public string FamilyId { get; set; } = string.Empty;
        public string RoomId { get; set; } = string.Empty;
        // Para StarkSwitch
        public string ApiKey { get; set; } = string.Empty;
        public string UserId { get; set; } = string.Empty;
        public string MqttTopic { get; set; } = string.Empty;
        public string Comando { get; set; } = string.Empty;
    }
}
