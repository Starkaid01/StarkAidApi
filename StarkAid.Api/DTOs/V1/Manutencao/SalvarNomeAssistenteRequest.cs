namespace StarkAid.Api.DTOs.V1.Manutencao;

public class SalvarNomeAssistenteRequest
{
    public Guid UserId { get; set; }
    public string NomeAssistente { get; set; } = string.Empty;
}
