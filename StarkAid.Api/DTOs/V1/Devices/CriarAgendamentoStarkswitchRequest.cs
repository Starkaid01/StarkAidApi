namespace StarkAid.Api.DTOs.V1.Devices;

public class CriarAgendamentoStarkswitchRequest
{
    public Guid DeviceId { get; set; }
    public string Acao { get; set; } = string.Empty; // ligar ou desligar
    public DateTime Data { get; set; }
    public int Hora { get; set; }
    public int Minuto { get; set; }
    public string Recorrencia { get; set; } = "NaoRepetir"; // NaoRepetir, TodosOsDias, TodaSemana, TodoMes, TodoAno
}

