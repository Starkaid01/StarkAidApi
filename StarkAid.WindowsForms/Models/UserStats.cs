namespace StarkAid.WindowsForms.Models;

public class UserStats
{
    public int TotalDevices { get; set; }
    public int TotalComandosSociais { get; set; }
    public string ApiStatus { get; set; } = "Desconectado";
    public string MqttStatus { get; set; } = "Desconectado";
    public bool MqttConnected { get; set; }
}

