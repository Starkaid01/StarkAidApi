namespace StarkAid.Api.DTOs.V1.DispositivoEsp;

public class DispositivoEspDto
{
    public Guid Id { get; set; }
    public string Nome { get; set; } = string.Empty;
    public string Ip { get; set; } = string.Empty;
    public int Porta { get; set; }
    public string? Comando { get; set; }
    public string? ComandToEsp { get; set; }
    public string Status { get; set; } = string.Empty;
    public bool LigadoDesligado { get; set; }
    public Guid? UserId { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? LastPingAt { get; set; }
    public DateTimeOffset? LastUpdatedAt { get; set; }
}

