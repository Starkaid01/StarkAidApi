namespace StarkAid.WindowsForms.Models;

public class LogToSuporte
{
    public int Id { get; set; }
    public string? UltimoComando { get; set; }
    public string? UltimaResposta { get; set; }
    public string? UltimoDispositivoAcionado { get; set; }
    public string? ErroCompleto { get; set; }
    public string? CodigoDeErro { get; set; }
    public string DataErro { get; set; } = string.Empty;
    public string HoraErro { get; set; } = string.Empty;
    public string AcaoErro { get; set; } = string.Empty;
}

