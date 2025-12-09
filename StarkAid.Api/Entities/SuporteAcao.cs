using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace StarkAid.Api.Entities;

public class SuporteAcao
{
    [Key]
    public int Id { get; set; }

    [Required]
    public Guid UserId { get; set; }

    [ForeignKey(nameof(UserId))]
    public User User { get; set; } = null!;

    [Required]
    [MaxLength(20)]
    public string Origem { get; set; } = "software"; // "software" ou "app"

    [Required]
    [MaxLength(100)]
    public string Acao { get; set; } = string.Empty; // "limparcache", "limpardados", "logout", etc.

    [MaxLength(500)]
    public string? Resposta { get; set; } // Resposta do cliente após executar ação

    public bool Sucesso { get; set; } = false;

    [Required]
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}
