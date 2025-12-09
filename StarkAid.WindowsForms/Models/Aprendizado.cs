namespace StarkAid.WindowsForms.Models;

public class Aprendizado
{
    public int Id { get; set; }
    public string ComandoUser { get; set; } = string.Empty;
    public string RespostaIa { get; set; } = string.Empty;
    public DateTime DataCriacao { get; set; }
}
