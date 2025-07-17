namespace StarkAid.Api.Entities;

public class User
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string ApiKey { get; set; } = string.Empty;
    public decimal StarkCoins { get; set; }
    public DateTime CreatedAt { get; set; }
    public bool IsActive { get; set; }
    public string Role { get; set; } = "UserNivel1"; // valor padrão
    public string? PreapprovalId { get; set; }
    public DateTime? UltimoPagamentoConfirmadoEm { get; set; }

}