namespace StarkAid.Api.DTOs.Manutencao;

public class AlterarSenhaRequest
{
    public Guid UserId { get; set; }
    public string NovaSenha { get; set; } = string.Empty;
}
