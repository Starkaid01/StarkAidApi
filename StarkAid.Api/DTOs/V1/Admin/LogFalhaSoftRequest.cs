namespace StarkAid.Api.DTOs.V1.Admin;

public class LogFalhaSoftRequest
{
    public string TipoFalha { get; set; } = string.Empty;
    public string? Descricao { get; set; }
    public string? ComandoTentado { get; set; }
    public string? DispositivoNome { get; set; }
    public string? ErroDetalhado { get; set; }
}

