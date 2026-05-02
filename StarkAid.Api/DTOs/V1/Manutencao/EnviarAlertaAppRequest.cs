namespace StarkAid.Api.DTOs.V1.Manutencao;

public class EnviarAlertaAppRequest
{
    public Guid UserId { get; set; }
    public string Message { get; set; } = string.Empty;
}
