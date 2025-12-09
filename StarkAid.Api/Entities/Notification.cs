using System.ComponentModel.DataAnnotations;

namespace StarkAid.Api.Entities;

public class Notification
{
    [Key]
    public Guid Id { get; set; }

    [Required]
    [MaxLength(100)]
    public string Tipo { get; set; } = string.Empty; // "pagamento_avulso", "assinatura", "licenca"

    [Required]
    [MaxLength(500)]
    public string Titulo { get; set; } = string.Empty;

    [Required]
    public string Mensagem { get; set; } = string.Empty;

    public Guid? UserId { get; set; } // Usuário que fez a compra (opcional)

    [MaxLength(200)]
    public string? UserEmail { get; set; } // Email do usuário (opcional)

    [MaxLength(200)]
    public string? UserName { get; set; } // Nome do usuário (opcional)

    public decimal? Valor { get; set; } // Valor da compra

    [MaxLength(100)]
    public string? ReferenciaId { get; set; } // ID da assinatura, licença, etc.

    [Required]
    public bool Lida { get; set; } = false;

    [Required]
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    public DateTimeOffset? LidaEm { get; set; }
}
