namespace StarkAid.Web.Dtos
{
    public class UserStatsDto
    {
        public int TotalDevices { get; set; }
        public int TotalComandosSociais { get; set; }
        public int TotalAgendamentos { get; set; }

        public string ApiStatus { get; set; } = string.Empty;
        public string MqttStatus { get; set; } = string.Empty;

        public bool MqttConnected { get; set; }
    }
}