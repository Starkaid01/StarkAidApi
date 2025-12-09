namespace StarkAid.WindowsForms.Models;

public enum TipoAgendamento
{
    Starkswitch = 1,
    ESP = 2,
    Ewelink = 3
}

public class Agendamento
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public Guid? DeviceId { get; set; } // Opcional - usado apenas para Starkswitch
    public Guid? DispositivoEspId { get; set; } // Opcional - usado apenas para ESP
    public string? EwelinkDeviceId { get; set; } // Opcional - usado apenas para Ewelink
    public TipoAgendamento TipoAgendamento { get; set; }
    public DateTimeOffset AgendadoPara { get; set; }
    public string Comando { get; set; } = string.Empty;
    public bool Executado { get; set; }
    public string? Recorrencia { get; set; } // Valores: "NaoRepetir", "TodosOsDias", "TodaSemana", "TodoMes", "TodoAno"
}
