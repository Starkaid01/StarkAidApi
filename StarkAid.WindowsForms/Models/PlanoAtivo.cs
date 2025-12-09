namespace StarkAid.WindowsForms.Models;

public class PlanoAtivo
{
    public Guid Id { get; set; }
    public decimal Valor { get; set; }
    public int Nivel { get; set; }
    public string NomePlano { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTimeOffset? IniciadaEm { get; set; }
    public DateTimeOffset? ExpiraEm { get; set; }
    public DateTimeOffset DataCriacao { get; set; }
    public string? StripeSubscriptionId { get; set; }
}

