namespace StarkAid.Api.DTOs.V1.Devices;

public class CriarAgendamentoEspRequest
{
    public Guid DispositivoEspId { get; set; }
    public DateTime Data { get; set; }
    public int Hora { get; set; }
    public int Minuto { get; set; }
    public string Recorrencia { get; set; } = "NaoRepetir"; // NaoRepetir, TodosOsDias, TodaSemana, TodoMes, TodoAno
}

