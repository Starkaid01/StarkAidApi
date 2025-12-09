namespace StarkAid.WindowsForms.Models;

public class DispositivoEsp
{
    public Guid Id { get; set; }
    public string Nome { get; set; } = string.Empty;
    public string Ip { get; set; } = string.Empty;
    public int Porta { get; set; }
    public string? Comando { get; set; }
    public string? ComandToEsp { get; set; }
    public string Status { get; set; } = "Desconectado";
    public bool LigadoDesligado { get; set; }
}

