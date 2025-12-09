namespace StarkAid.WindowsForms.Models;

public class ComandoSocial
{
    public Guid Id { get; set; }
    public string Comando { get; set; } = string.Empty;
    public string Resposta { get; set; } = string.Empty;
    public string? RespostasAleatorias { get; set; }
}

