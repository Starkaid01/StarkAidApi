namespace StarkAid.Api.DTOs.V1.Suporte;

public class ChatMessageDto
{
    public string Message { get; set; } = string.Empty;
    public string Sender { get; set; } = string.Empty; // "user", "support", "ia"
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public Guid? UserId { get; set; }
    public string? Origem { get; set; } // "software", "app"

    // Pacote econômico padronizado
    public string? PlanType { get; set; }
    public int StarkCoinBalance { get; set; }
    public int TokensConsumidosSemana { get; set; }
    public int TokensSemanaMax { get; set; }
    public int TokensRestantes { get; set; }
    public bool AdsEnabled { get; set; }
    public int AgendamentosMax { get; set; }
    public int AgendamentosRestantes { get; set; }
    public int Rate { get; set; } = 100;
}
