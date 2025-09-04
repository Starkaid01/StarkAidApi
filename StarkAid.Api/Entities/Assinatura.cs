using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace StarkAid.Api.Entities;

public class Assinatura
{
    [Key]
    public Guid Id { get; set; }

    [Required]
    public Guid UserId { get; set; }

    [ForeignKey(nameof(UserId))]
    public User User { get; set; } = null!;

    [MaxLength(100)]
    public string? StripeCustomerId { get; set; }

    [MaxLength(100)]
    public string? StripeSubscriptionId { get; set; }

    [MaxLength(50)]
    public string Status { get; set; } = "pendente"; // ativa, cancelada, trial, vencida, etc.

    [Column(TypeName = "decimal(18,2)")]
    public decimal Valor { get; set; } // Valor da assinatura (ex: 1.00)

    public DateTimeOffset? IniciadaEm { get; set; }

    public DateTimeOffset? CanceladaEm { get; set; }

    public DateTimeOffset? ExpiraEm { get; set; }

    public DateTimeOffset? PagamentoConfirmadoEm { get; set; }

    [Required]
    public DateTimeOffset DataCriacao { get; set; } = DateTimeOffset.UtcNow;
}
