using System;
using System.ComponentModel.DataAnnotations;

namespace StarkAid.Api.Entities;

public enum TipoAgendamento
{
    Starkswitch = 1,
    ESP = 2,
    Ewelink = 3
}

public class Agendamento
{
    [Key] public Guid Id { get; set; }

    [Required] public Guid UserId { get; set; }

    public Guid? DeviceId { get; set; } // Opcional - usado apenas para Starkswitch

    public Guid? DispositivoEspId { get; set; } // Opcional - usado apenas para ESP

    public string? EwelinkDeviceId { get; set; } // Opcional - usado apenas para Ewelink (string porque é o deviceId do Ewelink)

    [Required] public TipoAgendamento TipoAgendamento { get; set; }

    [Required] public DateTimeOffset AgendadoPara { get; set; }

    [Required] public string Comando { get; set; } = string.Empty;

    [Required] public bool Executado { get; set; }

    public string? Recorrencia { get; set; } // Valores: "NaoRepetir", "TodosOsDias", "TodaSemana", "TodoMes", "TodoAno"

    public User User { get; set; } = null!;
    public Device? Device { get; set; }
    public DispositivoEsp? DispositivoEsp { get; set; }
}
