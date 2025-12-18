namespace StarkAid.Web.DTOs
{
    public class HealthCheckResponse
    {
        public bool ApiStatus { get; set; }
        public bool MqttStatus { get; set; }
        public string ApiMessage { get; set; } = string.Empty;
        public string MqttMessage { get; set; } = string.Empty;
    }
}
