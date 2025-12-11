namespace StarkAid.Api.DTOs.V1.Manutencao;

public class AlterarSenhaRequest
{
    public Guid UserId { get; set; }
    public string NovaSenha { get; set; } = string.Empty;
}
