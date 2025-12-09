namespace StarkAid.Api.DTOs.Suporte;

public class ChatMessageDto
{
    public string Message { get; set; } = string.Empty;
    public string Sender { get; set; } = string.Empty; // "user", "support", "ia"
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public Guid? UserId { get; set; }
    public string? Origem { get; set; } // "software", "app"
}
