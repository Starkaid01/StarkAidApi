namespace StarkAid.WindowsForms.Models;

public class AgendamentoArquivo
{
    public int Id { get; set; }
    public string CaminhoArquivo { get; set; } = string.Empty;
    public DateTime DataHora { get; set; }
    public FrequenciaAgendamento Frequencia { get; set; }
    public bool Ativo { get; set; } = true;
    public DateTime? UltimaExecucao { get; set; }
}

public enum FrequenciaAgendamento
{
    Nenhum = 0,
    PorHora = 1,
    PorMinuto = 2,
    Diariamente = 3,
    Semanalmente = 4,
    Mensalmente = 5
}

